using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Soundbox.Test
{
    /// <summary>
    /// Implements <see cref="IOptions{TOptions}"/> and returns some given default value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TestOptions<T> : IOptions<T> where T : class, new()
    {
        public TestOptions() : this(default(T)) {}

        public TestOptions(T value)
        {
            this.Value = value;
        }

        public T Value { get; }
    }
}
