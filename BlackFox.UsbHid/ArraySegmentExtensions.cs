using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;

namespace BlackFox.UsbHid.Portable
{
    public static class ArraySegmentExtensions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ArraySegment{T}"/> structure that delimits the specified
        /// range of the elements in the specified array.
        /// </summary>
        public static ArraySegment<T> Segment<T>([NotNull] this T[] array, int offset, int count)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));

            return new ArraySegment<T>(array, offset, count);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArraySegment{T}"/> structure that delimits the specified
        /// number of the elements in the specified array starting from offset 0.
        /// </summary>
        public static ArraySegment<T> Segment<T>([NotNull] this T[] array, int count)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));

            return new ArraySegment<T>(array, 0, count);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArraySegment{T}"/> structure that delimits all the elements
        /// in the specified array.
        /// </summary>
        public static ArraySegment<T> Segment<T>([NotNull] this T[] array)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));

            return new ArraySegment<T>(array);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArraySegment{T}"/> structure that delimits the specified
        /// range of the elements in the specified segment.
        /// </summary>
        public static ArraySegment<T> Segment<T>(this ArraySegment<T> segment, int offset, int count)
        {
            if (segment == null) throw new ArgumentNullException(nameof(segment));
            ThrowIfNegative(offset, nameof(offset));
            ThrowIfNegative(count, nameof(count));

            if (offset + count > segment.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    $"The offset ({offset}) and count ({count}) must define a new segment that is inside the input segment (With count {segment.Count})");
            }

            return new ArraySegment<T>(segment.Array, segment.Offset + offset, count);
        }

        [AssertionMethod]
        static void ThrowIfNegative(int value, [InvokerParameterName] string name)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(name, "Offset can't be negative");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArraySegment{T}"/> structure that delimits all elements
        /// after and including the specified one.
        /// </summary>
        public static ArraySegment<T> Segment<T>(this ArraySegment<T> segment, int offset)
        {
            if (segment == null) throw new ArgumentNullException(nameof(segment));
            ThrowIfNegative(offset, nameof(offset));

            if (offset > segment.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(offset),
                    $"The offset ({offset}) must be inferior or equal to the count of the input segment ({segment.Count})");
            }

            return new ArraySegment<T>(segment.Array, segment.Offset + offset, segment.Count - offset);
        }


        /// <summary>
        /// Wrap the segment in a <see cref="Stream"/> instance.
        /// </summary>
        public static Stream AsStream(this ArraySegment<byte> segment)
        {
            return new MemoryStream(segment.Array, segment.Offset, segment.Count);
        }

        /// <summary>
        /// Wrap the segment in a <see cref="Stream"/> instance.
        /// </summary>
        public static IEnumerable<T> AsEnumerable<T>(this ArraySegment<T> segment)
        {
            for (int i = 0; i < segment.Count; i++)
            {
                var index = i + segment.Offset;
                yield return segment.Array[index];
            }
        }

        static bool SegmentsEquals(ArraySegment<byte> a, ArraySegment<byte> b)
        {


            return true;
        }

        public static bool ContentEquals<T>(this ArraySegment<T> @this, ArraySegment<T> other,
            IEqualityComparer<T> comparer = null)
        {
            if (@this.Count != other.Count)
            {
                return false;
            }

            if (comparer == null)
            {
                comparer = EqualityComparer<T>.Default;
            }

            for (int i = 0; i < @this.Count; i++)
            {
                var thisValue = @this.Array[@this.Offset + i];
                var otherValue = other.Array[other.Offset + i];

                if (comparer.Equals(thisValue, otherValue))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

