using System;
using System.Text;
using Microsoft.SPOT;

namespace CoreCommunication
{
    class FrameSerializer
    {
        public static byte[] Serialize(Frame frame)
        {
            return new FrameSerializer().BytesFromBuilder(frame);
        }

        private byte[] BytesFromBuilder(Frame frame)
        {
            byte[] bytes;
            int fixedLength = fixedLengthWithFrameType(frame.type);
            UInt16 length = (UInt16)(fixedLength + frame.variableDataLength);
            bytes = new byte[length + 4];

            bytes[0] = 0x7E;
            bytes[1] = ByteOperations.littleEndianBytesFromWord(length)[0];
            bytes[2] = ByteOperations.littleEndianBytesFromWord(length)[1];
            bytes[3] = (byte)frame.type;
            bytes[4] = 0x00; // FrameID, this should be set by the host to match with a subsequent response

            switch (frame.type)
            {
                case FrameType.RemoteATCommand:
                    setRemoteATCommandRequestValues(bytes, (RemoteATCommandRequestFrame)frame);
                    break;
            }

            bytes[fixedLength + 3 + frame.variableDataLength] = checksum(bytes);

            return bytes;
        }

        private void setRemoteATCommandRequestValues(byte[] bytes, RemoteATCommandRequestFrame frame)
        {
            for (int i = 0; i < 8; ++i)
            {
                bytes[5 + i] = frame.DestinationAddress64Bit[i];
            }
            bytes[13] = frame.DestinationAddress16Bit[0];
            bytes[14] = frame.DestinationAddress16Bit[1];
            bytes[15] = frame.CommandOptions;
            byte[] commandName = Encoding.UTF8.GetBytes(frame.ATCommandName);
            bytes[16] = commandName[0];
            bytes[17] = commandName[1];
            System.Array.Copy(frame.ATCommandData, 0, bytes, 18, frame.ATCommandData.Length);
        }

        private UInt16 fixedLengthWithFrameType(FrameType type)
        {
            switch (type)
            {
                case FrameType.RemoteATCommand:
                    return 15;

                default:
                    return 4;
            }
        }
        
        private byte checksum(byte[] bytes)
        {
            int sum = 0;
            for (int i = 0; i<bytes.Length-1; ++i)
            {
                sum += bytes[i];
            }

            return (byte)(0xFF - (sum & 0xFF));
        }
    }
}
