using System;
using System.IO;
using BlackFox.UsbHid;
using NFluent;

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
                }

                return stream.GetBuffer();
            }
        }

        public static void AssertBinary(ArraySegment<byte> actualBytes, params object[] expectedContent)
        {
            using (var stream = actualBytes.AsStream())
            {
                var reader = new BinaryReader(stream);

                foreach (var expectedObject in expectedContent)
                {
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
                        Check.That(actual).IsEqualTo(expected);
                    } else if (type == typeof (BytesHolder))
                    {
                        var holder = (BytesHolder)expectedObject;
                        reader.Read(holder.Bytes, 0, holder.ByteCount);
                    }
                }
            }
        }
    }
}