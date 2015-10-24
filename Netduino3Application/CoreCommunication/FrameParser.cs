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
                    Debug.Print("Received unknown frame type: " + frameType);
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

                UInt16[] analogSamples = nAnalogSamplesFromBytes(FRAME_ANALOG_SAMPLE_BYTE_INDEX, frame.AnalogChannels.Length, bytes);
                frame.AnalogSampleData = analogSamples;
            }
            else if (frame.AnalogChannels.Length > 0)
            {
                // If no digital samples are present, analog sample data is at the digital sample index
                UInt16[] analogSamples = nAnalogSamplesFromBytes(FRAME_DIGITAL_SAMPLE_BYTE_INDEX, frame.AnalogChannels.Length, bytes);
                frame.AnalogSampleData = analogSamples;
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
            int lastIndex = 0;
            for (int i = 0; i < digitalChannels.Length; ++i)
            {
                if (digitalChannels[i] > 0)
                {
                    channels[lastIndex++] = digitalChannels[i];
                }
            }
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
            int lastIndex = 0;
            for (int i = 0; i < analogChannels.Length; ++i)
            {
                if (analogChannels[i] > 0) {
                    channels[lastIndex++] = analogChannels[i];
                }
            }
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

        private static UInt16[] nAnalogSamplesFromBytes(int startIndex, int numberOfAnalogSamples, byte[] bytes)
        {
            UInt16[] analogSampleData = new UInt16[numberOfAnalogSamples];

            for (int i = 0; i < numberOfAnalogSamples; i++)
            {
                byte msb = bytes[startIndex + (i * 2)];
                byte lsb = bytes[startIndex + (i * 2) + 1];
                UInt16 sample = ByteOperations.littleEndianWordFromBytes(msb, lsb);
                analogSampleData[i] = sample;
            }

            return analogSampleData;
        }
    }
}
