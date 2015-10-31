using System;
using Microsoft.SPOT;

namespace CoreCommunication
{
    public enum FrameType : byte
    {
        Tx64Request = 0,
        Tx16Request = 0x01,
        ATCommand = 0x08,
        ATCommandQueueRegisterValue = 0x09,
        RemoteATCommand = 0x17,
        Rx64Indicator = 0x80,
        Rx16Indicator = 0x81,
        DIOADCRx64Indicator = 0x82,
        DIOADCRx16Indicator = 0x83,
        ATCommandResponse = 0x88,
        TxStatus = 0x89,
        ModemStatus = 0x8a,
        RemoteCommandResponse = 0x97,
    }

    public enum PacketOption : byte
    {
        PacketAcknowledged = 0x01,
        PacketRecievedAsBroadcast = 0x02,
        PacketRecievedOnBroadcastPAN = 0x04,
    }

    public enum RemoteATCommandOptions : byte
    {
        ApplyChanges = 0x02,
    }

    public abstract class Frame : Object
    {
        public FrameType type;
        public byte FrameID;
        public int variableDataLength;

        protected byte[] Address16Bit;
        protected byte[] Address64Bit;

        public static bool isEqualAddress(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) { return false; }

            for (int i = 0; i < a.Length; ++i)
            {
                if (a[i] != b[i]) { return false; }
            }

            return true;
        }
    }

    public class DigitalAnalogSampleFrame : Frame
    {
        public byte RSSI;
        public int NumberOfAnalogSamples;
        public byte[] DigitalChannels;
        public byte[] AnalogChannels;
        public byte[] DigitalSampleData;
        public UInt16[] AnalogSampleData;

        public byte[] SourceAddress16Bit
        {
            get { return Address16Bit; }
            set { Address16Bit = value; }
        }

        public byte[] SourceAddress64Bit
        {
            get { return Address64Bit; }
            set { Address64Bit = value; }
        }
    }

    public class RemoteATCommandFrame : Frame
    {
        public string ATCommandName;
        public byte[] ATCommandData;
    }

    public class RemoteATCommandRequestFrame : RemoteATCommandFrame
    {
        public RemoteATCommandOptions CommandOptions;

        public byte[] DestinationAddress16Bit 
        {
            get { return Address16Bit; }
            set { Address16Bit = value; }
        }

        public byte[] DestinationAddress64Bit
        {
            get { return Address64Bit; }
            set { Address64Bit = value; }
        }
    }

    public class RemoteATCommandResponseFrame : RemoteATCommandFrame
    {
        public byte RemStatus;

        public byte[] SourceAddress16Bit
        {
            get { return Address16Bit; }
            set { Address16Bit = value; }
        }

        public byte[] SourceAddress64Bit
        {
            get { return Address64Bit; }
            set { Address64Bit = value; }
        }
    }
}
