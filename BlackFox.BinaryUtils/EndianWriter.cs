// Copyright (c) Microsoft. All rights reserved.
// Copyright (c) Julien Roncaglia.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;

namespace BlackFox.Binary
{
    // This abstract base class represents a writer that can write
    // primitives to an arbitrary stream. A subclass can override methods to
    // give unique encodings.
    //
    internal class EndianWriter : IDisposable
    {
        public static readonly EndianWriter Null = new EndianWriter();

        protected Stream OutStream;
        public Endianness Endianess { get; }
        readonly Endianness resolvedEndianess;
        private readonly byte[] buffer;    // temp space for writing primitives to.
        public Encoding Encoding { get; }
        private readonly Encoder encoder;

        private readonly bool leaveOpen;

        // Perf optimization stuff
        private byte[] largeByteBuffer;  // temp space for writing chars.
        private int maxChars;   // max # of chars we can put in _largeByteBuffer
        
        // Size should be around the max number of chars/string * Encoding's max bytes/char
        private const int LargeByteBufferSize = 256;

        // Protected default constructor that sets the output stream
        // to a null stream (a bit bucket).
        protected EndianWriter()
        {
            OutStream = Stream.Null;
            buffer = new byte[16];
            Encoding = new UTF8Encoding(false, true);
            encoder = Encoding.GetEncoder();
        }

        public EndianWriter(Stream output) : this(output, Endianness.Native, new UTF8Encoding(false, true), true)
        {
        }

        public EndianWriter(Stream output, Encoding encoding)
            : this(output, Endianness.Native, encoding, true)
        {
        }

        public EndianWriter(Stream output, Endianness endianess)
            : this(output, endianess, new UTF8Encoding(false, true), true)
        {
        }

        public EndianWriter(Stream output, Endianness endianess, Encoding encoding, bool leaveOpen)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (!output.CanWrite)
                throw new ArgumentException("Can't write to the output stream", nameof(output));
            Contract.EndContractBlock();

            OutStream = output;
            buffer = new byte[16];
            Encoding = encoding;
            encoder = Encoding.GetEncoder();
            this.leaveOpen = leaveOpen;

            Endianess = endianess;
            resolvedEndianess = EndianessHelper.Resolve(endianess);
        }

        // Closes this writer and releases any system resources associated with the
        // writer. Following a call to Close, any operations on the writer
        // may raise exceptions. 
        public void Close()
        {
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (leaveOpen)
                    OutStream.Flush();
                else
                    OutStream.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        /*
         * Returns the stream associate with the writer. It flushes all pending
         * writes before returning. All subclasses should override Flush to
         * ensure that all buffered data is sent to the stream.
         */
        public Stream BaseStream
        {
            get
            {
                Flush();
                return OutStream;
            }
        }

        // Clears all buffers for this writer and causes any buffered data to be
        // written to the underlying device. 
        public void Flush()
        {
            OutStream.Flush();
        }

        public long Seek(int offset, SeekOrigin origin)
        {
            return OutStream.Seek(offset, origin);
        }

        // Writes a boolean to this stream. A single byte is written to the stream
        // with the value 0 representing false or the value 1 representing true.
        // 
        public void Write(bool value)
        {
            buffer[0] = (byte)(value ? 1 : 0);
            OutStream.Write(buffer, 0, 1);
        }

        // Writes a byte to this stream. The current position of the stream is
        // advanced by one.
        // 
        public void Write(byte value)
        {
            OutStream.WriteByte(value);
        }

        // Writes a signed byte to this stream. The current position of the stream 
        // is advanced by one.
        // 
        public void Write(sbyte value)
        {
            OutStream.WriteByte((byte)value);
        }

        // Writes a byte array to this stream.
        // 
        // This default implementation calls the Write(Object, int, int)
        // method to write the byte array.
        // 
        public void Write(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            Contract.EndContractBlock();
            OutStream.Write(buffer, 0, buffer.Length);
        }

        // Writes a section of a byte array to this stream.
        //
        // This default implementation calls the Write(Object, int, int)
        // method to write the byte array.
        // 
        public void Write(byte[] buffer, int index, int count)
        {
            OutStream.Write(buffer, index, count);
        }

        char[] oneChar = new char[1];

        // Writes a character to this stream. The current position of the stream is
        // advanced by two.
        // Note this method cannot handle surrogates properly in UTF-8.
        // 
        public void Write(char ch)
        {
            if (char.IsSurrogate(ch))
                throw new ArgumentException("Surrogates not allowed as single char", nameof(ch));
            Contract.EndContractBlock();

            Contract.Assert(Encoding.GetMaxByteCount(1) <= 16, "_encoding.GetMaxByteCount(1) <= 16)");
            oneChar[0] = ch;
            var numBytes = encoder.GetBytes(oneChar, 0, 1, buffer, 0, true);
            OutStream.Write(buffer, 0, numBytes);
        }

        // Writes a character array to this stream.
        // 
        // This default implementation calls the Write(Object, int, int)
        // method to write the character array.
        // 
        public void Write(char[] chars)
        {
            if (chars == null)
                throw new ArgumentNullException(nameof(chars));
            Contract.EndContractBlock();

            var bytes = Encoding.GetBytes(chars, 0, chars.Length);
            OutStream.Write(bytes, 0, bytes.Length);
        }

        // Writes a section of a character array to this stream.
        //
        // This default implementation calls the Write(Object, int, int)
        // method to write the character array.
        // 
        public void Write(char[] chars, int index, int count)
        {
            var bytes = Encoding.GetBytes(chars, index, count);
            OutStream.Write(bytes, 0, bytes.Length);
        }

        public unsafe void WriteLittleEndian(double value)
        {
            var tmpValue = *(ulong*)&value;
            buffer[0] = (byte)tmpValue;
            buffer[1] = (byte)(tmpValue >> 8);
            buffer[2] = (byte)(tmpValue >> 16);
            buffer[3] = (byte)(tmpValue >> 24);
            buffer[4] = (byte)(tmpValue >> 32);
            buffer[5] = (byte)(tmpValue >> 40);
            buffer[6] = (byte)(tmpValue >> 48);
            buffer[7] = (byte)(tmpValue >> 56);
            OutStream.Write(buffer, 0, 8);
        }

        public unsafe void WriteBigEndian(double value)
        {
            var tmpValue = *(ulong*)&value;
            buffer[0] = (byte)(tmpValue >> 56);
            buffer[1] = (byte)(tmpValue >> 48);
            buffer[2] = (byte)(tmpValue >> 40);
            buffer[3] = (byte)(tmpValue >> 32);
            buffer[4] = (byte)(tmpValue >> 24);
            buffer[5] = (byte)(tmpValue >> 16);
            buffer[6] = (byte)(tmpValue >> 8);
            buffer[7] = (byte)tmpValue;
            OutStream.Write(buffer, 0, 8);
        }

        // Writes a double to this stream. The current position of the stream is
        // advanced by eight.
        // 
        public void Write(double value)
        {
            if (resolvedEndianess == Endianness.BigEndian)
            {
                WriteBigEndian(value);
            }
            else
            {
                WriteLittleEndian(value);
            }
        }

        public void WriteLittleEndian(short value)
        {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            OutStream.Write(buffer, 0, 2);
        }

        public void WriteBigEndian(short value)
        {
            buffer[0] = (byte)(value >> 8);
            buffer[1] = (byte)value;
            OutStream.Write(buffer, 0, 2);
        }

        // Writes a two-byte signed integer to this stream. The current position of
        // the stream is advanced by two.
        // 
        public void Write(short value)
        {
            if (resolvedEndianess == Endianness.BigEndian)
            {
                WriteBigEndian(value);
            }
            else
            {
                WriteLittleEndian(value);
            }
        }

        public void WriteLittleEndian(ushort value)
        {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            OutStream.Write(buffer, 0, 2);
        }

        public void WriteBigEndian(ushort value)
        {
            buffer[0] = (byte)(value >> 8);
            buffer[1] = (byte)value;
            OutStream.Write(buffer, 0, 2);
        }

        // Writes a two-byte unsigned integer to this stream. The current position
        // of the stream is advanced by two.
        // 
        public void Write(ushort value)
        {
            if (resolvedEndianess == Endianness.BigEndian)
            {
                WriteBigEndian(value);
            }
            else
            {
                WriteLittleEndian(value);
            }
        }

        // Writes a four-byte signed integer to this stream. The current position
        // of the stream is advanced by four.
        // 
        public void Write(int value)
        {
            if (resolvedEndianess == Endianness.BigEndian)
            {
                WriteBigEndian(value);
            }
            else
            {
                WriteLittleEndian(value);
            }
        }

        public void WriteLittleEndian(int value)
        {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);
            OutStream.Write(buffer, 0, 4);
        }

        public void WriteBigEndian(int value)
        {
            buffer[0] = (byte)(value >> 24);
            buffer[1] = (byte)(value >> 16);
            buffer[2] = (byte)(value >> 8);
            buffer[3] = (byte)value;
            OutStream.Write(buffer, 0, 4);
        }

        // Writes a four-byte unsigned integer to this stream. The current position
        // of the stream is advanced by four.
        // 
        public void Write(uint value)
        {
            if (resolvedEndianess == Endianness.BigEndian)
            {
                WriteBigEndian(value);
            }
            else
            {
                WriteLittleEndian(value);
            }
        }

        public void WriteLittleEndian(uint value)
        {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);
            OutStream.Write(buffer, 0, 4);
        }

        public void WriteBigEndian(uint value)
        {
            buffer[0] = (byte)(value >> 24);
            buffer[1] = (byte)(value >> 16);
            buffer[2] = (byte)(value >> 8);
            buffer[3] = (byte)value;
            OutStream.Write(buffer, 0, 4);
        }

        // Writes an eight-byte signed integer to this stream. The current position
        // of the stream is advanced by eight.
        // 
        public void Write(long value)
        {
            if (resolvedEndianess == Endianness.BigEndian)
            {
                WriteBigEndian(value);
            }
            else
            {
                WriteLittleEndian(value);
            }
        }

        public void WriteLittleEndian(long value)
        {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);
            buffer[4] = (byte)(value >> 32);
            buffer[5] = (byte)(value >> 40);
            buffer[6] = (byte)(value >> 48);
            buffer[7] = (byte)(value >> 56);
            OutStream.Write(buffer, 0, 8);
        }

        public void WriteBigEndian(long value)
        {
            buffer[0] = (byte)(value >> 56);
            buffer[1] = (byte)(value >> 48);
            buffer[2] = (byte)(value >> 40);
            buffer[3] = (byte)(value >> 32);
            buffer[4] = (byte)(value >> 24);
            buffer[5] = (byte)(value >> 16);
            buffer[6] = (byte)(value >> 8);
            buffer[7] = (byte)value;
            OutStream.Write(buffer, 0, 8);
        }

        // Writes an eight-byte unsigned integer to this stream. The current 
        // position of the stream is advanced by eight.
        // 
        public void Write(ulong value)
        {
            if (resolvedEndianess == Endianness.BigEndian)
            {
                WriteBigEndian(value);
            }
            else
            {
                WriteLittleEndian(value);
            }
        }


        public void WriteLittleEndian(ulong value)
        {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);
            buffer[4] = (byte)(value >> 32);
            buffer[5] = (byte)(value >> 40);
            buffer[6] = (byte)(value >> 48);
            buffer[7] = (byte)(value >> 56);
            OutStream.Write(buffer, 0, 8);
        }

        public void WriteBigEndian(ulong value)
        {
            buffer[0] = (byte)(value >> 56);
            buffer[1] = (byte)(value >> 48);
            buffer[2] = (byte)(value >> 40);
            buffer[3] = (byte)(value >> 32);
            buffer[4] = (byte)(value >> 24);
            buffer[5] = (byte)(value >> 16);
            buffer[6] = (byte)(value >> 8);
            buffer[7] = (byte)value;
            OutStream.Write(buffer, 0, 8);
        }

        // Writes a float to this stream. The current position of the stream is
        // advanced by four.
        // 
        public void Write(float value)
        {
            if (resolvedEndianess == Endianness.BigEndian)
            {
                WriteBigEndian(value);
            }
            else
            {
                WriteLittleEndian(value);
            }
        }


        public unsafe void WriteLittleEndian(float value)
        {
            var tmpValue = *(uint*)&value;
            buffer[0] = (byte)tmpValue;
            buffer[1] = (byte)(tmpValue >> 8);
            buffer[2] = (byte)(tmpValue >> 16);
            buffer[3] = (byte)(tmpValue >> 24);
            OutStream.Write(buffer, 0, 4);
        }

        public unsafe void WriteBigEndian(float value)
        {
            var tmpValue = *(uint*)&value;
            buffer[0] = (byte)(tmpValue >> 24);
            buffer[1] = (byte)(tmpValue >> 16);
            buffer[2] = (byte)(tmpValue >> 8);
            buffer[3] = (byte)tmpValue;
            OutStream.Write(buffer, 0, 4);
        }

        // Writes a length-prefixed string to this stream in the EndianWriter's
        // current Encoding. This method first writes the length of the string as 
        // a four-byte unsigned integer, and then writes that many characters 
        // to the stream.
        // 
        public void Write(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            Contract.EndContractBlock();

            var len = Encoding.GetByteCount(value);
            Write7BitEncodedInt(len);

            if (largeByteBuffer == null)
            {
                largeByteBuffer = new byte[LargeByteBufferSize];
                maxChars = LargeByteBufferSize/Encoding.GetMaxByteCount(1);
            }

            if (len <= LargeByteBufferSize)
            {
                Encoding.GetBytes(value, 0, value.Length, largeByteBuffer, 0);
                OutStream.Write(largeByteBuffer, 0, len);
            }
            else
            {
                // Aggressively try to not allocate memory in this loop for
                // runtime performance reasons.  Use an Encoder to write out 
                // the string correctly (handling surrogates crossing buffer
                // boundaries properly).  
                var charStart = 0;
                var numLeft = value.Length;
                var valueChars = value.ToCharArray();
                while (numLeft > 0)
                {
                    // Figure out how many chars to process this round.
                    var charCount = (numLeft > maxChars) ? maxChars : numLeft;
                    var byteLen = encoder.GetBytes(valueChars, charStart, charCount, largeByteBuffer, 0, charCount == numLeft);
                    OutStream.Write(largeByteBuffer, 0, byteLen);
                    charStart += charCount;
                    numLeft -= charCount;
                }
            }
        }

        protected void Write7BitEncodedInt(int value)
        {
            // Write out an int 7 bits at a time.  The high bit of the byte,
            // when on, tells reader to continue reading more bytes.
            var v = (uint)value; // support negative numbers
            while (v >= 0x80)
            {
                Write((byte)(v | 0x80));
                v >>= 7;
            }
            Write((byte)v);
        }
    }
}