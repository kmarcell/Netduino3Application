using System;
using Microsoft.SPOT;

namespace CoreCommunication
{
    public abstract class ByteOperations
    {
        public static UInt16 littleEndianWordFromBytes(byte msb, byte lsb)
        {
            int word = msb * 256 + lsb;
            return (UInt16)word;
        }

        public static byte[] littleEndianBytesFromWord(UInt16 word)
        {
            byte msb = (byte)((word & 0xFF00) >> 8);
            byte lsb = (byte)(word & 0x00FF);
            return new byte[] {msb, lsb};
        }

        public static byte[] littleEndianBytesFromLong(UInt64 var)
        {
            byte[] bytes = new byte[8];
            for (int i = 0; i < 8; ++i)
            {
                bytes[i] = (byte)((var >> (i * 8)) & 0xFF);
            }
            return bytes;
        }
    }
}
