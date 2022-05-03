using Microsoft.Extensions.Logging;

namespace Soundbox.Logging
{
    /// <summary>
    /// Used to get <see cref="ILogger"/> instances in static contexts (i.e., static utility methods).
    /// </summary>
    public static class StaticLoggerProvider
    {
        /// <summary>
        /// For framework code only: the logger factory used by our static methods.
        /// </summary>
        internal static ILoggerFactory LoggerFactory { get; set; }

        /// <summary>
        /// Creates a new logger for the given type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static ILogger<T> CreateLogger<T>()
        {
            return LoggerFactory.CreateLogger<T>();
        }

        /// <summary>
        /// Creates a new logger for the given name
        /// </summary>
        /// <param name="categoryName"></param>
        /// <returns></returns>
        public static ILogger CreateLogger(string categoryName)
        {
            return LoggerFactory.CreateLogger(categoryName);
        }
    }
}
