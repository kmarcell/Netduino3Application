using System;
using System.Text;
using Microsoft.SPOT;

namespace CoreCommunication
{
    class CommandSerializer
    {
        public static byte[] Serialize(CommandBuilder builder)
        {
            return new CommandSerializer().BytesFromBuilder(builder);
        }

        private byte[] BytesFromBuilder(CommandBuilder builder)
        {
            byte[] bytes;
            UInt16 length = (UInt16)(fixedLengthWithFrameType(builder.FrameType) + builder.ATCommandData.Length);
            bytes = new byte[length + 4];

            bytes[0] = 0x7E;
            bytes[1] = ByteOperations.littleEndianBytesFromWord(length)[0];
            bytes[2] = ByteOperations.littleEndianBytesFromWord(length)[1];
            bytes[3] = (byte)builder.FrameType;
            bytes[4] = 0x00; // FrameID, this should be set by the host to match with a subsequent response
            for (int i = 0; i < 8; ++i)
            {
                bytes[5 + i] = builder.DestinationAddress64Bit[i];
            }
            bytes[13] = builder.DestinationAddress16Bit[0];
            bytes[14] = builder.DestinationAddress16Bit[1];
            bytes[15] = builder.CommandOptions;
            byte[] commandName = Encoding.UTF8.GetBytes(builder.ATCommandName);
            bytes[16] = commandName[0];
            bytes[17] = commandName[1];
            System.Array.Copy(builder.ATCommandData, 0, bytes, 18, builder.ATCommandData.Length);
            bytes[18 + builder.ATCommandData.Length] = checksum(bytes);

            return bytes;
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
