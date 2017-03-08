using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Storage
{
    public class Retrier
    {
        public static void Retry(int count, int waitMilliseconds, Type caughtExceptionType, Action callback)
        {
            var retryCount = count;
            Exception lastException = null;
            while (retryCount > 0)
            {
                try
                {
                    callback();
                    return;
                }
                catch (Exception e)
                {
                    if (!caughtExceptionType.IsInstanceOfType(e))
                        throw;
                    lastException = e;
                    retryCount--;
                    System.Threading.Thread.Sleep(waitMilliseconds);
                }
            }
            throw lastException;
        }

        public static T Retry<T>(int count, int waitMilliseconds, Type caughtExceptionType, Func<T> callback)
        {
            var retryCount = count;
            Exception lastException = null;
            while (retryCount > 0)
            {
                try
                {
                    return callback();
                }
                catch (Exception e)
                {
                    if (!caughtExceptionType.IsInstanceOfType(e))
                        throw;
                    lastException = e;
                    retryCount--;
                    System.Threading.Thread.Sleep(waitMilliseconds);
                }
            }
            throw lastException;
        }

        public static T Retry<T>(int count, int waitMilliseconds, Func<T> callback, Func<T, int, Exception, bool> expectation)
        {
            var retryCount = count;
            T result = default(T);
            Exception error;
            while (retryCount > 0)
            {
                error = null;
                try
                {
                    result = callback();
                }
                catch (Exception e)
                {
                    error = e;
                }

                if (expectation(result, retryCount, error))
                    break;
                retryCount--;
                System.Threading.Thread.Sleep(waitMilliseconds);
            }
            return result;
        }

    }
}
