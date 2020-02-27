using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Soundbox.Util
{
    public class IdentityHashProvider<T> : IEqualityComparer<T>
    {
        public bool Equals([AllowNull] T x, [AllowNull] T y)
        {
            return object.ReferenceEquals(x, y);
        }

        public int GetHashCode([DisallowNull] T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
