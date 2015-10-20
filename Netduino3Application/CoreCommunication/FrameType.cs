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

    public class Frame
    {
        public FrameType type;
    }

    public class DIOADCRx16IndicatorFrame : Frame
    {
        public byte FrameID;
        public UInt16 SourceAddress;
        public byte RSSI;
        public PacketOption Options;
        public byte[] DigitalChannels;
        public byte[] AnalogChannels;
        public byte[] DigitalSampleData;
        public UInt16[] AnalogSampleData;
    }
}
