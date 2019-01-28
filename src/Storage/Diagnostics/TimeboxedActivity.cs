using System;
using System.Threading;

namespace SenseNet.Diagnostics
{
    public class TimeboxedActivity
    {
        public Exception ExecutionException { get; private set; }

        public bool Aborted { get; private set; }
        public object InArgument;
        public object OutArgument;

        public AutoResetEvent WaitHandle = new AutoResetEvent(false);

        public Action<TimeboxedActivity> Activity;

        private Thread _t;

        public void Execute()
        {
            Aborted = false;
            ExecutionException = null;
            _t = new Thread(InternalExecute);
            _t.Start();
        }

        public bool ExecuteAndWait(int millisecondWait)
        {
            Execute();
            return WaitHandle.WaitOne(millisecondWait);
        }

        public void Abort()
        {
            _t.Abort();
            Aborted = true;
        }
        private void InternalExecute()
        {
            try
            {
                Activity(this);
            }
            catch (ThreadAbortException)
            {
                Aborted = true;
            }
            catch (Exception ex)
            {
                ExecutionException = ex;
            }
            finally
            {
                WaitHandle.Set();
            }
        }
    }
}
