using System;
using Microsoft.SPOT;

namespace CoreCommunication
{
    class Command
    {
        public bool RequestResponse;
        private byte[] data;

        public Command(byte[] data)
        {
            this.data = data;
        }

        public byte[] ByteArrayValue
        {
            get { return data; }
        }

        public byte FrameID
        {
            get
            {
                return data.Length >= 5 ? data[4] : (byte)0;
            }
            set
            {
                if (data.Length >= 5)
                {
                    data[4] = value;
                }
            }
        }
    }

    class CommandBuilder
    {
        // Private variables
        private FrameType frameType;
        private byte[] destinationAddress16Bit;
        private byte[] destinationAddress64Bit;
        private bool requestResponse;
        private byte CmdOptions = 0x01; // Apply changes on remote device. NOTE: If this bit is not set, an AC (or WR+FR) command must be sent before changes will take effect.
        private string commandName;
        private byte[] commandData;

        // Public variables
        public FrameType FrameType
        {
            get { return frameType; }
        }

        public byte[] DestinationAddress16Bit
        {
            get { return destinationAddress16Bit; }
        }

        public byte[] DestinationAddress64Bit
        {
            get { return destinationAddress64Bit; }
        }

        public bool RequestResponse
        {
            get { return requestResponse; }
        }

        public byte CommandOptions
        {
            get { return CmdOptions; }
        }

        public string ATCommandName
        {
            get { return commandName; }
        }

        public byte[] ATCommandData
        {
            get { return commandData; }
        }

        // Constructor
        public CommandBuilder(FrameType frameType)
        {
            this.frameType = frameType;
            setDefaultValues();
        }

        // Builder methods
        public Command Build()
        {
            Command cmd = new Command(CommandSerializer.Serialize(this));
            cmd.RequestResponse = requestResponse;
            return cmd;
        }
        
        public CommandBuilder setDestinationAddress16Bit(UInt16 destinationAddress)
        {
            destinationAddress16Bit = ByteOperations.littleEndianBytesFromWord(destinationAddress);
            return this;
        }
        public CommandBuilder setDestinationAddress16Bit(byte[] destinationAddress)
        {
            destinationAddress16Bit = destinationAddress;
            return this;
        }

        public CommandBuilder setRequestResponse(bool shouldRequestResponse)
        {
            requestResponse = shouldRequestResponse;
            return this;
        }

        public CommandBuilder setATCommandName(string commandName)
        {
            this.commandName = commandName;
            return this;
        }

        public CommandBuilder setATCommandData(byte[] commmandData)
        {
            this.commandData = commmandData;
            return this;
        }

        //! Allows for module parameter registers on a remote device to be queried or set.
        public static CommandBuilder RemoteATCommandRequest()
        {
            return new CommandBuilder(FrameType.RemoteATCommand);
        }

        // Private methods

        private void setDefaultValues()
        {
            switch (frameType)
            {
                case FrameType.RemoteATCommand:
                    setDestinationAddressDefaultValues();
                    break;

                default:
                    break;
            }
        }

        private void setDestinationAddressDefaultValues()
        {
            destinationAddress16Bit = new byte[2] { 0xFF, 0xFE };
            destinationAddress64Bit = new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF };
        }
    }
}
