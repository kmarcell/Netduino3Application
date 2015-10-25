using System;
using Microsoft.SPOT;

namespace CoreCommunication
{
    class FrameBuilder
    {
        // Private variables
        private FrameType frameType;
        private byte[] destinationAddress16Bit;
        private byte[] destinationAddress64Bit;
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
        public FrameBuilder(FrameType frameType)
        {
            this.frameType = frameType;
            setDefaultValues();
        }

        // Builder methods
        public Frame Build()
        {
            Frame frame = null;
            switch (frameType)
            {
                case FrameType.RemoteATCommand:
                {
                    RemoteATCommandRequestFrame _frame = new RemoteATCommandRequestFrame();
                    _frame.type = FrameType;
                    _frame.variableDataLength = ATCommandData.Length;
                    _frame.DestinationAddress16Bit = destinationAddress16Bit;
                    _frame.DestinationAddress64Bit = destinationAddress64Bit;
                    _frame.CommandOptions = CmdOptions;
                    _frame.ATCommandName = ATCommandName;
                    _frame.ATCommandData = ATCommandData;

                    frame = _frame;
                } break;

                default:
                    break;
            }

            return frame;
        }

        public FrameBuilder setDestinationAddress16Bit(UInt16 destinationAddress)
        {
            destinationAddress16Bit = ByteOperations.littleEndianBytesFromWord(destinationAddress);
            return this;
        }
        public FrameBuilder setDestinationAddress16Bit(byte[] destinationAddress)
        {
            destinationAddress16Bit = destinationAddress;
            return this;
        }

        public FrameBuilder setATCommandName(string commandName)
        {
            this.commandName = commandName;
            return this;
        }

        public FrameBuilder setATCommandData(byte[] commmandData)
        {
            this.commandData = commmandData;
            return this;
        }

        // Helpers

        public FrameBuilder setBroadcastAddress()
        {
            setDestinationAddressDefaultValues();
            return this;
        }

        //! Allows for module parameter registers on a remote device to be queried or set.
        public static FrameBuilder RemoteATCommandRequest
        {
            get { return new FrameBuilder(FrameType.RemoteATCommand); }
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
