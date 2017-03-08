using System;
using System.Threading;
using System.Web;

namespace SenseNet.Diagnostics
{
    public class TimeboxedActivity
    {
        private bool _aborted = false;
        public Exception ExecutionException { get; private set; }

        public bool Aborted { get { return _aborted; } }
        public object InArgument;
        public object OutArgument;

        public HttpContext Context;

        public AutoResetEvent WaitHandle = new AutoResetEvent(false);

        public Action<TimeboxedActivity> Activity;

        private Thread t;

        public void Execute()
        {
            _aborted = false;
            ExecutionException = null;
            t = new Thread(new ThreadStart(InternalExecute));
            t.Start();
        }

        public bool ExecuteAndWait(int millisecondWait)
        {
            Execute();
            return WaitHandle.WaitOne(millisecondWait);
        }

        public void Abort()
        {
            t.Abort();
            _aborted = true;
        }
        private void InternalExecute()
        {
            try
            {
                // we need the http context here mainly to
                // have the sql transaction object if it is
                // needed by the action
                if (HttpContext.Current == null && Context != null)
                    HttpContext.Current = Context;

                Activity(this);
            }
            catch (ThreadAbortException)
            {
                _aborted = true;
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
