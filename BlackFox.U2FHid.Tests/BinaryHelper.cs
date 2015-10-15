using System;
using System.IO;
using BlackFox.Binary;
using NFluent;
using NUnit.Framework;

namespace BlackFox.U2FHid.Tests
{
    internal static class BinaryHelper
    {
        public class BytesHolder
        {
            public int ByteCount { get; }
            public byte[] Bytes { get; }

            public BytesHolder(int byteCount)
            {
                ByteCount = byteCount;
                Bytes = new byte[byteCount];
            }
        }

        public static int GetSize(params object[] args)
        {
            var size = 0;
            foreach (var arg in args)
            {
                var type = arg.GetType();
                if (type == typeof(uint) || type == typeof(int))
                {
                    size += 4;
                }
                else if (type == typeof(ushort) || type == typeof(short))
                {
                    size += 2;
                }
                else if (type == typeof(byte) || type == typeof(sbyte))
                {
                    size += 1;
                }
                else if (type == typeof (BytesHolder))
                {
                    size += ((BytesHolder)arg).ByteCount;
                }
                else if (type == typeof (ArraySegment<byte>))
                {
                    size += ((ArraySegment<byte>)arg).Count;
                }
                else if (type == typeof (byte[]))
                {
                    size += ((byte[])arg).Length;
                }
                else
                {
                    throw new ArgumentException($"Unsupported type: {type.Name}", nameof(args));
                }
            }
            return size;
        }

        public static byte[] BuildBinary(params object[] args)
        {
            using (var stream = new MemoryStream(64))
            {
                var writer = new BinaryWriter(stream);

                foreach (var arg in args)
                {
                    var type = arg.GetType();
                    if (type == typeof(uint))
                    {
                        writer.Write((uint)arg);
                    }
                    else if (type == typeof(ushort))
                    {
                        writer.Write((ushort)arg);
                    }
                    else if (type == typeof(byte))
                    {
                        writer.Write((byte)arg);
                    }
                    else if (type == typeof(BytesHolder))
                    {
                        var holder = ((BytesHolder)arg);
                        writer.Write(holder.Bytes);
                    }
                    else if (type == typeof(ArraySegment<byte>))
                    {
                        var segment = (ArraySegment<byte>)arg;
                        writer.Write(segment.Array, segment.Offset, segment.Count);
                    }
                    else if (type == typeof(byte[]))
                    {
                        var array = (byte[])arg;
                        writer.Write(array);
                    }
                }

                return stream.GetBuffer();
            }
        }

        public static void AssertBinary(ArraySegment<byte> actualBytes, params object[] expectedContent)
        {
            var str = SegmentToString(actualBytes);

            using (var stream = actualBytes.AsStream())
            {
                var reader = new BinaryReader(stream);

                foreach (var expectedObject in expectedContent)
                {
                    var index = stream.Position;
                    var type = expectedObject.GetType();
                    if (type == typeof (uint))
                    {
                        var expected = (uint)expectedObject;
                        var actual = reader.ReadUInt32();
                        Check.That(actual).IsEqualTo(expected);
                    }
                    else if (type == typeof(ushort))
                    {
                        var expected = (ushort)expectedObject;
                        var actual = reader.ReadUInt16();
                        Check.That(actual).IsEqualTo(expected);
                    }
                    else if (type == typeof(byte))
                    {
                        var expected = (byte)expectedObject;
                        var actual = reader.ReadByte();
                        if (actual != expected)
                        {
                            throw new AssertionException(
                                $"Byte at index {index} is 0x{actual:X2} but 0x{expected:X2} was expected.\r\n\r\n{str}");
                        }
                    } else if (type == typeof (BytesHolder))
                    {
                        var holder = (BytesHolder)expectedObject;
                        reader.Read(holder.Bytes, 0, holder.ByteCount);
                    }
                    else if (type == typeof (ArraySegment<byte>))
                    {
                        var expected = (ArraySegment<byte>)expectedObject;
                        var actual = new byte[expected.Count];
                        reader.Read(actual, 0, actual.Length);
                        Check.That(expected.ContentEquals(actual.Segment())).IsTrue();
                    }
                    else if (type == typeof (byte[]))
                    {
                        var expected = (byte[])expectedObject;
                        var actual = new byte[expected.Length];
                        reader.Read(actual, 0, actual.Length);
                        if (!expected.Segment().ContentEquals(actual.Segment()))
                        {
                            var actualStr = SegmentToString(actual.Segment());
                            var expectedStr = SegmentToString(expected.Segment());
                            throw new AssertionException($"The 2 binary data differ, expected: \r\n{expectedStr}\r\nRead:\r\n{actualStr}\r\nFull actual data:\r\n{str}");
                        }
                    }
                }
            }
        }

        static string SegmentToString(ArraySegment<byte> segment)
        {
            var w = new StringWriter();
            segment.WriteAsHexTo(w);
            var str = w.ToString();
            return str;
        }
    }
}