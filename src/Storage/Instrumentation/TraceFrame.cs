using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Reflection;

namespace SenseNet.Services.Instrumentation
{
    public enum GenerateTraceFrameEventMessages
    {
        GenerateAll,
        ErrorsOnly,
        DontGenerate
    }

    /// <summary>
    /// TraceFrame
    /// </summary>
    public sealed class TraceFrame : IDisposable
    {
        internal string Message { get; private set; }
        internal MethodBase Caller { get; private set; }
        private GenerateTraceFrameEventMessages FrameEventMessagesGenerateMode  { get; set; }

        private TraceFrame _nextTraceFrame;

        // Used as per thread storage when HttpContext.Current is not available
        [ThreadStatic]
        private static TraceFrame _rootTraceFrame;

        private static System.Threading.ReaderWriterLockSlim _rootTraceFrameAccessorLock =
            new System.Threading.ReaderWriterLockSlim();

        private bool _isUnderDisposal;

        private static TraceFrame RootTraceFrame
        {
            get
            {
                try
                {
                    _rootTraceFrameAccessorLock.EnterReadLock();
                    HttpContext currentHttpContext = HttpContext.Current;
                    if (currentHttpContext != null)
                    {
                        return currentHttpContext.Items["RootTraceFrame"] as TraceFrame;
                    }
                    else
                    {
                        return _rootTraceFrame;
                    }
                }
                finally
                {
                    _rootTraceFrameAccessorLock.ExitReadLock();
                }
            }
            set
            {
                try
                {
                    _rootTraceFrameAccessorLock.EnterWriteLock();
                    HttpContext currentHttpContext = HttpContext.Current;
                    if (currentHttpContext != null)
                    {
                        currentHttpContext.Items["RootTraceFrame"] = value;
                    }
                    else
                    {
                        _rootTraceFrame = value;
                    }
                }
                finally
                {
                    _rootTraceFrameAccessorLock.ExitWriteLock();
                }
            }
        }

        internal TraceFrame(string message, MethodBase caller, GenerateTraceFrameEventMessages frameEventMessagesGenerateMode)
        {
            Message = message;
            Caller = caller;
            FrameEventMessagesGenerateMode = frameEventMessagesGenerateMode;

            if (RootTraceFrame == null)
            {
                RootTraceFrame = this;
            }
            else
            {
                TraceFrame lastTraceFrame = RootTraceFrame;

                while (lastTraceFrame._nextTraceFrame != null)
                {
                    lastTraceFrame = lastTraceFrame._nextTraceFrame;
                }

                lastTraceFrame._nextTraceFrame = this;
            }

            if (FrameEventMessagesGenerateMode == GenerateTraceFrameEventMessages.GenerateAll)
                TraceHelper.TraceFrameEvent(TraceFrameEventType.Started, this);
        }

        internal static TraceFrame GetLastTraceFrame()
        {
            TraceFrame tf = TraceFrame.RootTraceFrame;

            if (tf == null)
                return null;

            while (tf._nextTraceFrame != null)
            {
                tf = tf._nextTraceFrame;
            }

            return tf;
        }

        internal static string GetTraceFrameString()
        {
            TraceFrame tf = TraceFrame.RootTraceFrame;

            StringBuilder sb = new StringBuilder("[TF>>");

            while (tf != null)
            {
                sb.Append(tf.Message);
                tf = tf._nextTraceFrame;
                if (tf != null)
                    sb.Append(">>");
            }

            sb.Append("]");

            return sb.ToString();
        }


        #region IDisposable pattern

        ~TraceFrame()
        {
            Dispose(true);
        }

        public void Close()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(false);
        }

        private void Dispose(bool calledByFinalizer)
        {
            if (_isUnderDisposal == false)
            {
                _isUnderDisposal = true;

                TraceFrame tf = RootTraceFrame;

                if (tf == this)
                {
                    // This is the root
                    if (tf._nextTraceFrame != null)
                    {
                        if (FrameEventMessagesGenerateMode== GenerateTraceFrameEventMessages.GenerateAll || FrameEventMessagesGenerateMode == GenerateTraceFrameEventMessages.ErrorsOnly)
                            TraceHelper.TraceFrameEvent(TraceFrameEventType.WrongDisposeOrder, this);
                    }

                    RootTraceFrame = null;
                }
                else
                {
                    // This is not the root

                    while (tf == null || tf._nextTraceFrame != this)
                    {
                        tf = tf._nextTraceFrame;
                    }

                    // We went down the list, but cannot found ourselves -> must never happen.
                    System.Diagnostics.Debug.Assert(tf != null);

                    // Now "tf" points to my parent

                    // I have children - the developer calls the Dispose in wrong order...
                    if (_nextTraceFrame != null)
                    {
                        if (FrameEventMessagesGenerateMode == GenerateTraceFrameEventMessages.GenerateAll || FrameEventMessagesGenerateMode == GenerateTraceFrameEventMessages.ErrorsOnly)
                            TraceHelper.TraceFrameEvent(TraceFrameEventType.WrongDisposeOrder, this);
                    }

                    // Ask my parent to kill me. Not PC, but works.
                    tf._nextTraceFrame = null;
                }

                if (FrameEventMessagesGenerateMode == GenerateTraceFrameEventMessages.GenerateAll)
                    TraceHelper.TraceFrameEvent(TraceFrameEventType.Finished, this);

                if (!calledByFinalizer)
                    GC.SuppressFinalize(this);
            }
        }

        #endregion

        public static TraceFrame Begin(string name)
        {
            MethodBase caller = TraceHelper.GetCallerInfo();
            return new TraceFrame(name, caller, GenerateTraceFrameEventMessages.GenerateAll);
        }

        public static TraceFrame Begin(string name, GenerateTraceFrameEventMessages frameEventMessagesGenerateMode)
        {
            MethodBase caller = TraceHelper.GetCallerInfo();
            return new TraceFrame(name, caller, frameEventMessagesGenerateMode);
        }

    }


}
