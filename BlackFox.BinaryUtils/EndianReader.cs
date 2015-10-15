// Copyright (c) Microsoft. All rights reserved.
// Copyright (c) Julien Roncaglia.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Security;
using System.Text;

namespace BlackFox.Binary
{
    internal class BinaryReader : IDisposable
    {
        private const int MaxCharBytesSize = 128;

        private byte[] buffer;
        private Decoder decoder;
        private byte[] charBytes;
        private char[] singleChar;
        private char[] charBuffer;
        private readonly int maxCharsSize;  // From MaxCharBytesSize & Encoding

        // Performance optimization for Read() w/ Unicode.  Speeds us up by ~40% 
        private readonly bool use2BytesPerChar;
        private readonly bool leaveOpen;
        Endianness resolvedEndianess;
        public Endianness Endianness { get; }
        

        public BinaryReader(Stream input) : this(input, Endianness.Native, new UTF8Encoding(), true)
        {
        }

        public BinaryReader(Stream input, Encoding encoding) : this(input, Endianness.Native, encoding, true)
        {
        }

        public BinaryReader(Stream input, Endianness endianess) : this(input, endianess, new UTF8Encoding(), true)
        {
        }

        public BinaryReader(Stream input, Endianness endianess, Encoding encoding, bool leaveOpen)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }
            if (!input.CanRead)
                throw new ArgumentException("Can't read from the output stream", nameof(input));
            Contract.EndContractBlock();

            BaseStream = input;
            decoder = encoding.GetDecoder();
            maxCharsSize = encoding.GetMaxCharCount(MaxCharBytesSize);
            var minBufferSize = encoding.GetMaxByteCount(1);  // max bytes per one char
            if (minBufferSize < 16)
                minBufferSize = 16;
            buffer = new byte[minBufferSize];
            // m_charBuffer and m_charBytes will be left null.

            // For Encodings that always use 2 bytes per char (or more), 
            // special case them here to make Read() & Peek() faster.
            use2BytesPerChar = encoding is UnicodeEncoding;
            this.leaveOpen = leaveOpen;

            Endianness = endianess;
            resolvedEndianess = EndianessHelper.Resolve(endianess);

            Contract.Assert(decoder != null, "[BinaryReader.ctor]m_decoder!=null");
        }

        public Stream BaseStream { get; private set; }

        public void Close()
        {
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                var copyOfStream = BaseStream;
                BaseStream = null;
                if (copyOfStream != null && !leaveOpen)
                    copyOfStream.Dispose();
            }
            BaseStream = null;
            buffer = null;
            decoder = null;
            charBytes = null;
            singleChar = null;
            charBuffer = null;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public int PeekChar()
        {
            Contract.Ensures(Contract.Result<int>() >= -1);

            if (BaseStream == null)
            {
                throw new ObjectDisposedException(null, "The stream is closed");
            }

            if (!BaseStream.CanSeek)
                return -1;
            var origPos = BaseStream.Position;
            var ch = Read();
            BaseStream.Position = origPos;
            return ch;
        }

        public int Read()
        {
            Contract.Ensures(Contract.Result<int>() >= -1);

            if (BaseStream == null)
            {
                throw new ObjectDisposedException(null, "The stream is closed");
            }
            return InternalReadOneChar();
        }

        public bool ReadBoolean()
        {
            FillBuffer(1);
            return (buffer[0] != 0);
        }

        public byte ReadByte()
        {
            // Inlined to avoid some method call overhead with FillBuffer.
            if (BaseStream == null)
            {
                throw new ObjectDisposedException(null, "The stream is closed");
            }

            var b = BaseStream.ReadByte();
            if (b == -1)
            {
                throw new EndOfStreamException("Readed beyond EOF");
            }
            return (byte)b;
        }

        public sbyte ReadSByte()
        {
            FillBuffer(1);
            return (sbyte)(buffer[0]);
        }

        public char ReadChar()
        {
            var value = Read();
            if (value == -1)
            {
                throw new EndOfStreamException("Readed beyond EOF");
            }
            return (char)value;
        }

        public short ReadInt16()
        {
            return resolvedEndianess == Endianness.BigEndian ? ReadInt16BigEndian() : ReadInt16LittleEndian();
        }

        public short ReadInt16LittleEndian()
        {
            FillBuffer(2);
            return (short)(buffer[0] | buffer[1] << 8);
        }

        public short ReadInt16BigEndian()
        {
            FillBuffer(2);
            return (short)( buffer[0] << 8 | buffer[1]);
        }

        public ushort ReadUInt16()
        {
            return resolvedEndianess == Endianness.BigEndian ? ReadUInt16BigEndian() : ReadUInt16LittleEndian();
        }

        public ushort ReadUInt16LittleEndian()
        {
            FillBuffer(2);
            return (ushort)(buffer[0] | buffer[1] << 8);
        }

        public ushort ReadUInt16BigEndian()
        {
            FillBuffer(2);
            return (ushort)(buffer[0] << 8 | buffer[1]);
        }

        public int ReadInt32()
        {
            return resolvedEndianess == Endianness.BigEndian ? ReadInt32BigEndian() : ReadInt32LittleEndian();
        }

        public int ReadInt32LittleEndian()
        {
            FillBuffer(4);
            return buffer[0] | buffer[1] << 8 | buffer[2] << 16 | buffer[3] << 24;
        }

        public int ReadInt32BigEndian()
        {
            FillBuffer(4);
            return buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3];
        }

        public uint ReadUInt32()
        {
            return resolvedEndianess == Endianness.BigEndian ? ReadUInt32BigEndian() : ReadUInt32LittleEndian();
        }

        public uint ReadUInt32LittleEndian()
        {
            FillBuffer(4);
            return (uint)(buffer[0] | buffer[1] << 8 | buffer[2] << 16 | buffer[3] << 24);
        }

        public uint ReadUInt32BigEndian()
        {
            FillBuffer(4);
            return (uint)(buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3]);
        }

        public long ReadInt64()
        {
            return resolvedEndianess == Endianness.BigEndian ? ReadInt64BigEndian() : ReadInt64LittleEndian();
        }

        public long ReadInt64LittleEndian()
        {
            FillBuffer(8);
            var lo = (uint)(buffer[0] | buffer[1] << 8 |
                             buffer[2] << 16 | buffer[3] << 24);
            var hi = (uint)(buffer[4] | buffer[5] << 8 |
                             buffer[6] << 16 | buffer[7] << 24);
            // ReSharper disable once RedundantCast
            return (long)((ulong)hi) << 32 | lo;
        }

        public long ReadInt64BigEndian()
        {
            FillBuffer(8);
            var lo = (uint)(buffer[7] | buffer[6] << 8 |
                             buffer[5] << 16 | buffer[4] << 24);
            var hi = (uint)(buffer[3] | buffer[2] << 8 |
                             buffer[1] << 16 | buffer[0] << 24);
            // ReSharper disable once RedundantCast
            return (long)((ulong)hi) << 32 | lo;
        }

        public ulong ReadUInt64()
        {
            return resolvedEndianess == Endianness.BigEndian ? ReadUInt64BigEndian() : ReadUInt64LittleEndian();
        }

        public ulong ReadUInt64LittleEndian()
        {
            FillBuffer(8);
            var lo = (uint)(buffer[0] | buffer[1] << 8 |
                             buffer[2] << 16 | buffer[3] << 24);
            var hi = (uint)(buffer[4] | buffer[5] << 8 |
                             buffer[6] << 16 | buffer[7] << 24);
            // ReSharper disable once RedundantCast
            return (ulong)hi << 32 | lo;
        }

        public ulong ReadUInt64BigEndian()
        {
            FillBuffer(8);
            var lo = (uint)(buffer[7] | buffer[6] << 8 |
                             buffer[5] << 16 | buffer[4] << 24);
            var hi = (uint)(buffer[3] | buffer[2] << 8 |
                             buffer[1] << 16 | buffer[0] << 24);
            // ReSharper disable once RedundantCast
            return (ulong)hi << 32 | lo;
        }

        public float ReadSingle()
        {
            return resolvedEndianess == Endianness.BigEndian ? ReadSingleBigEndian() : ReadSingleLittleEndian();
        }

        public unsafe float ReadSingleLittleEndian()
        {
            FillBuffer(4);
            var tmpBuffer = (uint)(buffer[0] | buffer[1] << 8 | buffer[2] << 16 | buffer[3] << 24);
            return *((float*)&tmpBuffer);
        }

        public unsafe float ReadSingleBigEndian()
        {
            FillBuffer(4);
            var tmpBuffer = (uint)(buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3]);
            return *((float*)&tmpBuffer);
        }

        public double ReadDouble()
        {
            return resolvedEndianess == Endianness.BigEndian ? ReadDoubleBigEndian() : ReadDoubleLittleEndian();
        }

        public unsafe double ReadDoubleLittleEndian()
        {
            FillBuffer(8);
            var lo = (uint)(buffer[0] | buffer[1] << 8 |
                buffer[2] << 16 | buffer[3] << 24);
            var hi = (uint)(buffer[4] | buffer[5] << 8 |
                buffer[6] << 16 | buffer[7] << 24);

            var tmpBuffer = ((ulong)hi) << 32 | lo;
            return *((double*)&tmpBuffer);
        }

        public unsafe double ReadDoubleBigEndian()
        {
            FillBuffer(8);
            var lo = (uint)(buffer[7] | buffer[6] << 8 |
                             buffer[5] << 16 | buffer[4] << 24);
            var hi = (uint)(buffer[3] | buffer[2] << 8 |
                             buffer[1] << 16 | buffer[0] << 24);

            var tmpBuffer = ((ulong)hi) << 32 | lo;
            return *((double*)&tmpBuffer);
        }

        public string ReadString()
        {
            Contract.Ensures(Contract.Result<string>() != null);

            if (BaseStream == null)
            {
                throw new ObjectDisposedException(null, "The stream is closed");
            }

            var currPos = 0;

            // Length of the string in bytes, not chars
            var stringLength = Read7BitEncodedInt();
            if (stringLength < 0)
            {
                throw new IOException("Invalid string length in stream");
            }

            if (stringLength == 0)
            {
                return string.Empty;
            }

            if (charBytes == null)
            {
                charBytes = new byte[MaxCharBytesSize];
            }

            if (charBuffer == null)
            {
                charBuffer = new char[maxCharsSize];
            }

            StringBuilder sb = null;
            do
            {
                var readLength = ((stringLength - currPos) > MaxCharBytesSize) ? MaxCharBytesSize : (stringLength - currPos);

                var n = BaseStream.Read(charBytes, 0, readLength);
                if (n == 0)
                {
                    throw new EndOfStreamException("Readed beyond EOF");
                }

                var charsRead = decoder.GetChars(charBytes, 0, n, charBuffer, 0);

                if (currPos == 0 && n == stringLength)
                    return new string(charBuffer, 0, charsRead);

                if (sb == null)
                    sb = new StringBuilder(stringLength); // Actual string length in chars may be smaller.
                sb.Append(charBuffer, 0, charsRead);
                currPos += n;

            } while (currPos < stringLength);

            return sb.ToString();
        }

        [SecuritySafeCritical]
        public int Read(char[] buffer, int index, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Must be a positive value");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Must be a positive value");
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentException("Invalid offset");
            }
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() <= count);
            Contract.EndContractBlock();

            if (BaseStream == null)
            {
                throw new ObjectDisposedException(null, "The stream is closed");
            }

            // SafeCritical: index and count have already been verified to be a valid range for the buffer
            return InternalReadChars(buffer, index, count);
        }

        [SecurityCritical]
        private int InternalReadChars(char[] buffer, int index, int count)
        {
            Contract.Requires(buffer != null);
            Contract.Requires(index >= 0 && count >= 0);
            Contract.Assert(BaseStream != null);

            var charsRemaining = count;

            if (charBytes == null)
            {
                charBytes = new byte[MaxCharBytesSize];
            }

            while (charsRemaining > 0)
            {
                int charsRead;
                // We really want to know what the minimum number of bytes per char
                // is for our encoding.  Otherwise for UnicodeEncoding we'd have to
                // do ~1+log(n) reads to read n characters.
                var numBytes = charsRemaining;

                // special case for DecoderNLS subclasses when there is a hanging byte from the previous loop
                if (use2BytesPerChar)
                    numBytes <<= 1;
                if (numBytes > MaxCharBytesSize)
                    numBytes = MaxCharBytesSize;

                var position = 0;
                numBytes = BaseStream.Read(charBytes, 0, numBytes);
                var byteBuffer = charBytes;

                if (numBytes == 0)
                {
                    return (count - charsRemaining);
                }

                Contract.Assert(byteBuffer != null, "expected byteBuffer to be non-null");
                charsRead = decoder.GetChars(byteBuffer, position, numBytes, buffer, index, false);

                charsRemaining -= charsRead;
                index += charsRead;
            }

            // this should never fail
            Contract.Assert(charsRemaining >= 0, "We read too many characters.");

            // we may have read fewer than the number of characters requested if end of stream reached 
            // or if the encoding makes the char count too big for the buffer (e.g. fallback sequence)
            return (count - charsRemaining);
        }

        private int InternalReadOneChar()
        {
            // I know having a separate InternalReadOneChar method seems a little 
            // redundant, but this makes a scenario like the security parser code
            // 20% faster, in addition to the optimizations for UnicodeEncoding I
            // put in InternalReadChars.   
            var charsRead = 0;
            long posSav = 0;

            if (BaseStream.CanSeek)
                posSav = BaseStream.Position;

            if (charBytes == null)
            {
                charBytes = new byte[MaxCharBytesSize];
            }
            if (singleChar == null)
            {
                singleChar = new char[1];
            }

            while (charsRead == 0)
            {
                // We really want to know what the minimum number of bytes per char
                // is for our encoding.  Otherwise for UnicodeEncoding we'd have to
                // do ~1+log(n) reads to read n characters.
                // Assume 1 byte can be 1 char unless m_2BytesPerChar is true.
                var numBytes = use2BytesPerChar ? 2 : 1;

                var r = BaseStream.ReadByte();
                charBytes[0] = (byte)r;
                if (r == -1)
                    numBytes = 0;
                if (numBytes == 2)
                {
                    r = BaseStream.ReadByte();
                    charBytes[1] = (byte)r;
                    if (r == -1)
                        numBytes = 1;
                }

                if (numBytes == 0)
                {
                    // Console.WriteLine("Found no bytes.  We're outta here.");
                    return -1;
                }

                Contract.Assert(numBytes == 1 || numBytes == 2, "BinaryReader::InternalReadOneChar assumes it's reading one or 2 bytes only.");

                try
                {

                    charsRead = decoder.GetChars(charBytes, 0, numBytes, singleChar, 0);
                }
                catch
                {
                    // Handle surrogate char 

                    if (BaseStream.CanSeek)
                        BaseStream.Seek((posSav - BaseStream.Position), SeekOrigin.Current);
                    // else - we can't do much here

                    throw;
                }

                Contract.Assert(charsRead < 2, "InternalReadOneChar - assuming we only got 0 or 1 char, not 2!");
                //                Console.WriteLine("That became: " + charsRead + " characters.");
            }
            if (charsRead == 0)
                return -1;
            return singleChar[0];
        }

        [SecuritySafeCritical]
        public char[] ReadChars(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Positive or zero value required");
            }
            Contract.Ensures(Contract.Result<char[]>() != null);
            Contract.Ensures(Contract.Result<char[]>().Length <= count);
            Contract.EndContractBlock();
            if (BaseStream == null)
            {
                throw new ObjectDisposedException(null, "The stream is closed");
            }

            if (count == 0)
            {
                return EmptyArray<char>.Value;
            }

            // SafeCritical: we own the chars buffer, and therefore can guarantee that the index and count are valid
            var chars = new char[count];
            var n = InternalReadChars(chars, 0, count);
            if (n != count)
            {
                var copy = new char[n];
                Buffer.BlockCopy(chars, 0, copy, 0, 2 * n); // sizeof(char)
                chars = copy;
            }

            return chars;
        }

        public int Read(byte[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "Positive or zero value required");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Positive or zero value required");
            if (buffer.Length - index < count)
                throw new ArgumentException("Invalid offset");
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() <= count);
            Contract.EndContractBlock();

            if (BaseStream == null)
            {
                throw new ObjectDisposedException(null, "The stream is closed");
            }
            return BaseStream.Read(buffer, index, count);
        }

        public byte[] ReadBytes(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "Positive or zero value required");
            Contract.Ensures(Contract.Result<byte[]>() != null);
            Contract.Ensures(Contract.Result<byte[]>().Length <= Contract.OldValue(count));
            Contract.EndContractBlock();
            if (BaseStream == null)
            {
                throw new ObjectDisposedException(null, "The stream is closed");
            }

            if (count == 0)
            {
                return EmptyArray<byte>.Value;
            }

            var result = new byte[count];

            var numRead = 0;
            do
            {
                var n = BaseStream.Read(result, numRead, count);
                if (n == 0)
                    break;
                numRead += n;
                count -= n;
            } while (count > 0);

            if (numRead != result.Length)
            {
                // Trim array.  This should happen on EOF & possibly net streams.
                var copy = new byte[numRead];
                Buffer.BlockCopy(result, 0, copy, 0, numRead);
                result = copy;
            }

            return result;
        }

        protected void FillBuffer(int numBytes)
        {
            if ((numBytes < 0 || numBytes > buffer.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(numBytes), "Invalid number of bytes to fill in the buffer");
            }
            var bytesRead = 0;
            int n;

            if (BaseStream == null)
            {
                throw new ObjectDisposedException(null, "The stream is closed");
            }

            // Need to find a good threshold for calling ReadByte() repeatedly
            // vs. calling Read(byte[], int, int) for both buffered & unbuffered
            // streams.
            if (numBytes == 1)
            {
                n = BaseStream.ReadByte();
                if (n == -1)
                {
                    throw new EndOfStreamException("Readed beyond EOF");
                }
                buffer[0] = (byte)n;
                return;
            }

            do
            {
                n = BaseStream.Read(buffer, bytesRead, numBytes - bytesRead);
                if (n == 0)
                {
                    throw new EndOfStreamException("Readed beyond EOF");
                }
                bytesRead += n;
            } while (bytesRead < numBytes);
        }

        internal protected int Read7BitEncodedInt()
        {
            // Read out an Int32 7 bits at a time.  The high bit
            // of the byte when on means to continue reading more bytes.
            var count = 0;
            var shift = 0;
            byte b;
            do
            {
                // Check for a corrupted stream.  Read a max of 5 bytes.
                // In a future version, add a DataFormatException.
                if (shift == 5 * 7)  // 5 bytes max per Int32, shift += 7
                    throw new FormatException("Bad 7-Bit Int32");

                // ReadByte handles end of stream cases for us.
                b = ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return count;
        }
    }


}