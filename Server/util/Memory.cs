using System;

namespace Soundbox.Util
{
    /// <summary>
    /// Extension and helper methods for memory related functions.
    /// </summary>
    public static class Memory
    {
        /// <summary>
        /// Returns the given segment's array content.
        /// If the segment's <see cref="ArraySegment{T}.Offset"/> is 0, then <see cref="ArraySegment{T}.Array"/> is returned.
        /// Otherwise, copies the elements into the given array and resizes it as needed.<br/>
        /// This can be used for APIs that provide an array and size parameter, but not an offset parameter.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="segment"></param>
        /// <param name="array"></param>
        /// <returns></returns>
        public static T[] GetArray<T>(this ArraySegment<T> segment, ref T[] array)
        {
            if (segment.Offset == 0)
                return segment.Array;
            if (array == null || array.Length < segment.Count)
                array = new T[segment.Count];
            Array.Copy(segment.Array, segment.Offset, array, 0, segment.Count);
            return array;
        }

        /// <summary>
        /// Like <see cref="GetArray{T}(ArraySegment{T}, ref T[])"/> but allocates a new array every time if <see cref="ArraySegment{T}.Offset"/> is non-zero.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="segment"></param>
        /// <param name="array"></param>
        /// <returns></returns>
        public static T[] GetArray<T>(this ArraySegment<T> segment)
        {
            T[] result = null;
            return GetArray(segment, ref result);
        }
    }
}
