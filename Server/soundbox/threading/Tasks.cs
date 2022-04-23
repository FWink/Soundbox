using System;
using System.Threading.Tasks;

namespace Soundbox.Threading
{
    /// <summary>
    /// Helper class for Task-related utilities.
    /// </summary>
    public static class Tasks
    {
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
    }
}
