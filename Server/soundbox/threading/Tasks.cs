using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Soundbox.Threading
{
    /// <summary>
    /// Helper class for Task-related utilities.
    /// </summary>
    public static class Tasks
    {
        private static readonly ILogger LoggerUncaught = Logging.StaticLoggerProvider.CreateLogger("Soundbox.Uncaught");

        #region "Taskify"

        /// <summary>
        /// Runs the given function synchronously and returns a <see cref="Task"/> object.
        /// This should be used instead of simply returning <see cref="Task.CompletedTask"/> or such in order
        /// to get proper exception handling (which matters when the caller does not immediately await your synchronous method returning a Task).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        public static Task<T> Taskify<T>(Func<T> func)
        {
            try
            {
                T result = func();
                return Task.FromResult(result);
            }
            catch (OperationCanceledException e)
            {
                return Task.FromCanceled<T>(e.CancellationToken);
            }
            catch (Exception e)
            {
                return Task.FromException<T>(e);
            }
        }

        /// <summary>
        /// See <see cref="Taskify{T}(Func{T})"/>
        /// </summary>
        /// <param name="act"></param>
        /// <returns></returns>
        public static Task Taskify(Action act)
        {
            return Taskify<object>(() =>
            {
                act();
                return null;
            });
        }

        #endregion

        #region "Fire and forget"

        /// <summary>
        /// Asynchronously executes the given function and catches and logs errors.
        /// Used when you aren't interested in the task's result or simply need to run something in a different task/thread.
        /// </summary>
        /// <param name="func"></param>
        public static void FireAndForget(Func<Task> func)
        {
            Task.Run(() =>
            {
                Task task;
                try
                {
                    task = func();
                }
                catch (Exception e)
                {
                    LoggerUncaught.LogError(e, "Uncaught error in FireAndForget");
                    return;
                }

                _ = task.ContinueWith(taskResult =>
                {
                    LoggerUncaught.LogError(taskResult.Exception, "Uncaught error in FireAndForget");
                }, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted);
            });
        }

        /// <summary>
        /// See <see cref="FireAndForget(Func{Task})"/>
        /// </summary>
        /// <param name="act"></param>
        public static void FireAndForget(Action act)
        {
            FireAndForget(() => Taskify(act));
        }

        #endregion
    }
}
