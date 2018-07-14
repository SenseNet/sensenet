using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository
{
    public class CounterManager
    {
        private static CounterManager _current;
        private static readonly object _counterLockObject = new object();
        private static readonly string PERFORMANCECOUNTER_CATEGORYNAME = "SenseNet";

        private static readonly CounterCreationData[] _defaultCounters = new[]
                                                                             {
                                                                                 new CounterCreationData { CounterType = PerformanceCounterType.NumberOfItems32, CounterName = "GapSize" },
                                                                                 new CounterCreationData { CounterType = PerformanceCounterType.NumberOfItems32, CounterName = "IncomingMessages" },
                                                                                 new CounterCreationData { CounterType = PerformanceCounterType.NumberOfItems32, CounterName = "TotalMessagesToProcess" },
                                                                                 new CounterCreationData { CounterType = PerformanceCounterType.NumberOfItems32, CounterName = "DelayingRequests" }
                                                                             };

        private Dictionary<string, bool> _invalidCounters;

        private static System.Timers.Timer _perfCounterTimer;
        private static PerformanceCounter _cpuCounter;
        private static PerformanceCounter _ramCounter;
        private static float _cpuUsage;
        private static float _availableRAM;

        // ================================================================================= Static properties

        private static CounterManager Current
        {
            get
            {
                if (_current == null)
                {
                    lock (_counterLockObject)
                    {
                        if (_current == null)
                        {
                            var current = new CounterManager();
                            current.Initialize();
                            _current = current;

                            var message = Logging.PerformanceCountersEnabled
                                              ? "Performance counters are created: " + string.Join(", ", _current.CounterNames) + "."
                                              : "Performance counters are disabled.";

                            SnLog.WriteInformation(message + ". CounterManager:" + _current);
                        }
                    }
                }
                return _current;
            }
        }

        public string[] CounterNames
        {
            get { return CounterManager.Current.Counters.Select(c => c.CounterName).ToArray(); }
        }

        // ================================================================================= Instance properties

        private PerformanceCounterCategory Category { get; set; }

        private SenseNetPerformanceCounter[] _counters = new SenseNetPerformanceCounter[0];
        private IEnumerable<SenseNetPerformanceCounter> Counters => _counters;

        // ================================================================================= Static methods

        public static void Increment(string counterName)
        {
            if (!Logging.PerformanceCountersEnabled)
                return;

            var counter = CounterManager.Current.GetCounter(counterName);
            if (counter != null)
                counter.Increment();
        }

        public static void IncrementBy(string counterName, long value)
        {
            if (!Logging.PerformanceCountersEnabled)
                return;

            var counter = CounterManager.Current.GetCounter(counterName);
            if (counter != null)
                counter.IncrementBy(value);
        }

        public static void Decrement(string counterName)
        {
            if (!Logging.PerformanceCountersEnabled)
                return;

            var counter = CounterManager.Current.GetCounter(counterName);
            if (counter != null)
                counter.Decrement();
        }

        public static void Reset(string counterName)
        {
            if (!Logging.PerformanceCountersEnabled)
                return;

            var counter = CounterManager.Current.GetCounter(counterName);
            if (counter != null)
                counter.Reset();
        }

        public static void SetRawValue(string counterName, long value)
        {
            if (!Logging.PerformanceCountersEnabled)
                return;

            var counter = CounterManager.Current.GetCounter(counterName);
            if (counter != null)
                counter.SetRawValue(value);
        }

        public static void Start()
        {
            var cm = CounterManager.Current;
        }

        public static float GetCPUUsage()
        {
            return CounterManager.Current.GetCPUUsageInternal();
        }
        public static float GetAvailableRAM()
        {
            return CounterManager.Current.GetAvailableRAMInternal();
        }

        private float GetCPUUsageInternal()
        {
            return _cpuUsage;
        }
        private float GetAvailableRAMInternal()
        {
            return _availableRAM;
        }

        // ================================================================================= Instance methods

        private void Initialize()
        {
            _cpuUsage = 0;
            _availableRAM = 0;
            _invalidCounters = new Dictionary<string, bool>();

            if (!Logging.PerformanceCountersEnabled)
            {
                _counters = new SenseNetPerformanceCounter[0];
                return;
            }

            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _ramCounter = new PerformanceCounter("Memory", "Available KBytes");

                _perfCounterTimer = new System.Timers.Timer(3000);
                _perfCounterTimer.Elapsed += PerfCounter_Timer_Elapsed;
                _perfCounterTimer.Disposed += PerfCounter_Timer_Disposed;
                _perfCounterTimer.Enabled = true;
            }
            catch (Exception ex)
            {
                SnLog.WriteWarning(
                    "Performance counters could not be initialized, the values will always be 0. Message: " + ex.Message,
                    EventId.RepositoryLifecycle);
            }

            try
            {
                Category = CreateCategory();
                _counters = Category.GetCounters().Select(pc => new SenseNetPerformanceCounter(pc)).ToArray();
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex, "Error during performance counter initialization.");
                _counters = new SenseNetPerformanceCounter[0];
            }
        }

        // ================================================================================= Event handlers

        private static void PerfCounter_Timer_Disposed(object sender, EventArgs e)
        {
            _perfCounterTimer.Elapsed -= PerfCounter_Timer_Elapsed;
            _perfCounterTimer.Disposed -= PerfCounter_Timer_Disposed;
        }

        private static void PerfCounter_Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var timerEnabled = _perfCounterTimer.Enabled;
            _perfCounterTimer.Enabled = false;

            try
            {
                _cpuUsage = _cpuCounter.NextValue();
                _availableRAM = _ramCounter.NextValue();

                _perfCounterTimer.Enabled = timerEnabled;
            }
            catch (Exception ex) // logged
            {
                SnLog.WriteException(ex, "Error when trying to get value of performance counter. ");

                _perfCounterTimer.Enabled = false;
            }
        }

        // ================================================================================= Helper methods

        private SenseNetPerformanceCounter GetCounter(string counterName)
        {
            if (string.IsNullOrEmpty(counterName))
                throw new ArgumentNullException("counterName");

            var counter = CounterManager.Current.Counters.FirstOrDefault(c => c.CounterName == counterName);
            if (counter == null)
            {
                if (_invalidCounters.ContainsKey(counterName))
                    return null;

                lock (_counterLockObject)
                {
                    if (!_invalidCounters.ContainsKey(counterName))
                    {
                        _invalidCounters.Add(counterName, true);

                        SnLog.WriteWarning("Performance counter does not exist: " + counterName);
                    }
                }
            }

            return counter;
        }

        private static PerformanceCounterCategory CreateCategory()
        {
            if (PerformanceCounterCategory.Exists(PERFORMANCECOUNTER_CATEGORYNAME))
                PerformanceCounterCategory.Delete(PERFORMANCECOUNTER_CATEGORYNAME);

            // start with the built-in counters
            var currentCounters = new List<CounterCreationData>();
            currentCounters.AddRange(_defaultCounters);

            // add the user-defined custom counters (only the ones that are different from the built-ins)
            foreach (var customPerfCounter in Logging.CustomPerformanceCounters.Cast<CounterCreationData>().
                Where(customPerfCounter => !currentCounters.Any(c => c.CounterName == customPerfCounter.CounterName)))
            {
                currentCounters.Add(customPerfCounter);
            }

            return PerformanceCounterCategory.Create(PERFORMANCECOUNTER_CATEGORYNAME, "Performance counters of Sense/Net",
                PerformanceCounterCategoryType.SingleInstance, new CounterCreationDataCollection(currentCounters.ToArray()));
        }
    }
}
