using System;
using Microsoft.SPOT;

namespace CoreCommunication
{
    class FrameParser
    {
        public static int FRAME_TYPE_BYTE_INDEX = 3;
        public static int FRAME_ID_BYTE_INDEX = 4;
        public static int FRAME_SOURCE_ADDRESS_BYTE_INDEX = 5;
        public static int FRAME_RSSI_BYTE_INDEX = 7;
        public static int FRAME_OPTIONS_BYTE_INDEX = 8;
        public static int FRAME_CHANNELS_BYTE_INDEX = 9;
        public static int FRAME_DIGITAL_SAMPLE_BYTE_INDEX = 11;
        public static int FRAME_ANALOG_SAMPLE_BYTE_INDEX = 13;

        public static int NUMBER_OF_DIGITAL_CHANNELS = 9;
        public static int NUMBER_OF_ANALOG_CHANNELS = 6;

        public static Frame FrameFromRawBytes(byte[] bytes)
        {
            if (bytes.Length <= FRAME_TYPE_BYTE_INDEX) { return null; }

            Frame frame = null;
            FrameType frameType = (FrameType)bytes[FRAME_TYPE_BYTE_INDEX];
            switch (frameType)
            {
                case FrameType.DIOADCRx16Indicator:
                    frame = DIOADCRx16IndicatorFrameFromRawBytes(bytes); 
                    break;

                default:
                    break;
            }

            return frame;
        }

        private static DIOADCRx16IndicatorFrame DIOADCRx16IndicatorFrameFromRawBytes(byte[] bytes)
        {
            if (bytes.Length <= FRAME_ANALOG_SAMPLE_BYTE_INDEX) { return null; }

            DIOADCRx16IndicatorFrame frame = new DIOADCRx16IndicatorFrame();
            frame.FrameID = bytes[FRAME_ID_BYTE_INDEX];
            frame.SourceAddress = ByteOperations.littleEndianWordFromBytes(bytes[FRAME_SOURCE_ADDRESS_BYTE_INDEX], bytes[FRAME_SOURCE_ADDRESS_BYTE_INDEX + 1]);
            frame.RSSI = bytes[FRAME_RSSI_BYTE_INDEX];
            frame.Options = (PacketOption)bytes[FRAME_OPTIONS_BYTE_INDEX];
            frame.DigitalChannels = digitalChannelsFromBytes(bytes[FRAME_CHANNELS_BYTE_INDEX], bytes[FRAME_CHANNELS_BYTE_INDEX + 1]);
            frame.AnalogChannels = analogChannelsFromByte(bytes[FRAME_CHANNELS_BYTE_INDEX]);
            
            if (frame.DigitalChannels.Length > 0)
            {
                frame.DigitalSampleData = digitalSampleDataFromBytes(bytes[FRAME_DIGITAL_SAMPLE_BYTE_INDEX], bytes[FRAME_DIGITAL_SAMPLE_BYTE_INDEX + 1]);

                UInt16 sample = ByteOperations.littleEndianWordFromBytes(bytes[FRAME_ANALOG_SAMPLE_BYTE_INDEX], bytes[FRAME_ANALOG_SAMPLE_BYTE_INDEX + 1]);
                frame.AnalogSampleData = new UInt16[] { sample };
            }
            else
            {
                UInt16 sample = ByteOperations.littleEndianWordFromBytes(bytes[FRAME_DIGITAL_SAMPLE_BYTE_INDEX], bytes[FRAME_DIGITAL_SAMPLE_BYTE_INDEX + 1]);
                frame.AnalogSampleData = new UInt16[]{ sample };
            }

            return frame;
        }

        private static byte[] digitalChannelsFromBytes(byte msb, byte lsb)
            // [na, A5, A4, A3, A2, A1, A0, D8][D7, D6, D5, D4, D3, D2, D1, D0]
        {
            byte[] digitalChannels = new byte[NUMBER_OF_DIGITAL_CHANNELS];
            int activeChannels = 0;

            int mask = ByteOperations.littleEndianWordFromBytes(msb, lsb);
            for (int i = 0; i < NUMBER_OF_DIGITAL_CHANNELS; ++i)
            {
                if ((mask & (1 << i)) > 0)
                {
                    ++activeChannels;
                    digitalChannels[i] = (byte)i;
                }
            }

            byte[] channels = new byte[activeChannels];
            Array.Copy(channels, digitalChannels, activeChannels);
            return channels;
        }

        private static byte[] analogChannelsFromByte(byte msb)
            // [na, A5, A4, A3, A2, A1, A0, D8]
        {
            byte[] analogChannels = new byte[NUMBER_OF_ANALOG_CHANNELS];
            int activeChannels = 0;

            int mask = msb >> 1;
            for (int i = 0; i < NUMBER_OF_ANALOG_CHANNELS; ++i)
            {
                if ((mask & (1 << i)) > 0)
                {
                    ++activeChannels;
                    analogChannels[i] = (byte)i;
                }
            }

            byte[] channels = new byte[activeChannels];
            Array.Copy(channels, analogChannels, activeChannels);
            return channels;
        }

        private static byte[] digitalSampleDataFromBytes(byte msb, byte lsb)
        {
            byte[] digitalSampleData = new byte[NUMBER_OF_DIGITAL_CHANNELS];

            int sampleData = ByteOperations.littleEndianWordFromBytes(msb, lsb);
            for (int i = 0; i < NUMBER_OF_DIGITAL_CHANNELS; ++i)
            {
                digitalSampleData[i] = (sampleData & (1 << i)) > 0 ? (byte)1 : (byte)0;
            }

            return digitalSampleData;
        }
    }
}
