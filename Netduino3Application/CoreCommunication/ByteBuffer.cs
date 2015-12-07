using System;
using Microsoft.SPOT;

namespace CoreCommunication
{
    class ByteBuffer
    {
        private byte[] buffer;

        public ByteBuffer()
        {
            buffer = new byte[0];
        }

        public ByteBuffer(byte[] bytes)
        {
            buffer = bytes;
        }

        public void AddBytes(byte[] bytes)
        {
            byte[] tmp_buffer = new byte[buffer.Length + bytes.Length];
            System.Array.Copy(buffer, tmp_buffer, buffer.Length);
            System.Array.Copy(bytes, 0, tmp_buffer, buffer.Length, bytes.Length);
            buffer = tmp_buffer;
        }

        public void RemoveFirstNBytes(uint N)
        {
            if (N > buffer.Length)
            {
                N = (uint)buffer.Length;
            }

            if (N == 0)
            {
                return;
            }

            byte[] tmp_buffer = new byte[buffer.Length - N];
            Array.Copy(buffer, (int)N, tmp_buffer, 0, tmp_buffer.Length);
            buffer = tmp_buffer;
        }

        public byte[] RawBytes
        {
            get { return buffer; }
        }

        public int Length
        {
            get { return buffer.Length; }
        }
    }
}
