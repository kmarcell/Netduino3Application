using System;
using Microsoft.SPOT;

namespace CoreCommunication
{
    class FrameSlicer
    {
        private static int XBEE_LENGTH_BYTE_INDEX = 1;

        public static byte[] nextFrameFromBuffer(byte[] buffer, int offset)
        {
            int nBytesStartByte = 1;
            int nBytesFrameLength = 2;
            int nBytesFrameChecksum = 1;

            if (offset < 0 || offset >= buffer.Length || buffer.Length < (nBytesStartByte + nBytesFrameLength))
            {
                return new byte[] { };
            }

            UInt16 dataLength = ByteOperations.littleEndianWordFromBytes(buffer[offset + XBEE_LENGTH_BYTE_INDEX], buffer[offset + XBEE_LENGTH_BYTE_INDEX + 1]);
            int frameLength = nBytesStartByte + nBytesFrameLength + dataLength + nBytesFrameChecksum; // 7E + [msbLength, lsbLength] + [data] + [checksum]
            if (buffer.Length < frameLength)
            {
                return new byte[] { };
            }

            byte[] bytes = new byte[frameLength];
            Array.Copy(buffer, offset, bytes, 0, frameLength);
            return bytes;
        }
    }
}
