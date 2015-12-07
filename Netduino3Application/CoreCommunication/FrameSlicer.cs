using System;
using Microsoft.SPOT;

namespace CoreCommunication
{
    class FrameSlicer
    {
        public static byte[] nextFrameFromBuffer(byte[] buffer, uint offset, out uint endIndex)
        {
            uint nBytesStartByte = 1;
            uint nBytesFrameLength = 2;
            uint nBytesFrameChecksum = 1;

            for (uint i = offset; i < buffer.Length; ++i)
            {
                if (buffer[i] == 0x7E)
                {
                    offset = i;
                    break;
                }
            }

            if (offset >= buffer.Length || buffer.Length < (offset + nBytesStartByte + nBytesFrameLength))
            {
                endIndex = offset;
                return new byte[] { };
            }

            UInt16 dataLength = ByteOperations.littleEndianWordFromBytes(buffer[offset + 1], buffer[offset + 2]);
            uint frameLength = nBytesStartByte + nBytesFrameLength + dataLength + nBytesFrameChecksum; // 7E + [msbLength, lsbLength] + [data] + [checksum]
            if (buffer.Length < offset + frameLength)
            {
                endIndex = offset;
                return new byte[] { };
            }

            endIndex = offset + frameLength;
            byte[] bytes = new byte[frameLength];
            Array.Copy(buffer, (int)offset, bytes, 0, (int)frameLength);
            return bytes;
        }
    }
}
