using System;
using Microsoft.SPOT;
using System.IO.Ports;
using CoreCommunication;

namespace Xbee
{
    public class ReceivedRemoteFrameEventArgs : EventArgs
    {
        private Frame frame;

        public ReceivedRemoteFrameEventArgs(Frame frame)
        {
            this.frame = frame;
        }

        public Frame Frame
        {
            get { return this.frame; }
        }
    }

    public delegate void ReceivedRemoteFrameEventHandler(object sender, ReceivedRemoteFrameEventArgs e);

    public class FrameDroppedByChecksumEventArgs : EventArgs
    {
        private byte[] rawBytes;

        public FrameDroppedByChecksumEventArgs(byte[] bytes)
        {
            this.rawBytes = bytes;
        }

        public byte[] RawBytes
        {
            get { return this.rawBytes; }
        }
    }

    public delegate void FrameDroppedByChecksumEventHandler(object sender, FrameDroppedByChecksumEventArgs e);

    public class BytesReadFromSerialEventArgs
    {
        private byte[] rawBytes;

        public BytesReadFromSerialEventArgs(byte[] bytes)
        {
            this.rawBytes = bytes;
        }

        public byte[] RawBytes
        {
            get { return this.rawBytes; }
        }
    }

    public delegate void BytesReadFromSerialEventHandler(object sender, BytesReadFromSerialEventArgs e);

    class XbeeDevice
    {
        public event ReceivedRemoteFrameEventHandler ReceivedRemoteFrame;
        public event FrameDroppedByChecksumEventHandler FrameDroppedByChecksum;
        public event BytesReadFromSerialEventHandler BytesReadFromSerial;

        private SerialPort serialPort;
        private static byte XBEE_START_BYTE = 0x7E;
        private static int XBEE_LENGTH_BYTE_INDEX = 1;
        private byte[] rx_buffer;

        public XbeeDevice(SerialPort serialPort)
        {
            this.serialPort = serialPort;
            this.serialPort.Open();
            this.serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            this.rx_buffer = new byte[0];
        }

        protected struct NextFrame
        {
            public bool HasValue;
            public int EndIndex;
            public byte[] RawBytes;

            public NextFrame(byte[] bytes, int startIndex)
            {
                this.HasValue = true;
                this.EndIndex = startIndex + bytes.Length;
                this.RawBytes = bytes;
            }
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort serialPort = (SerialPort)sender;
            if (serialPort != this.serialPort) { return; }

            int nBytes = serialPort.BytesToRead;
            if (nBytes > 0)
            {
                // Merge RxBuffer and incoming bytes to buffer
                byte[] bytes = readBytesFromSerial(this.serialPort, nBytes);
                byte[] buffer = new byte[this.rx_buffer.Length + bytes.Length];
                System.Array.Copy(this.rx_buffer, buffer, this.rx_buffer.Length);
                System.Array.Copy(bytes, 0, buffer, this.rx_buffer.Length, bytes.Length);
                this.rx_buffer = new byte[0];

                // Slice and Parse frames
                int index = 0;
                NextFrame rawFrame = nextFrameFromBuffer(buffer, index);
                while (rawFrame.HasValue)
                {
                    handleRawFrameRead(rawFrame);

                    index = rawFrame.EndIndex;
                    rawFrame = nextFrameFromBuffer(buffer, index);
                }

                // Save partial last Frame
                int remainingBytes = buffer.Length - index;
                if (remainingBytes > 0)
                {
                    this.rx_buffer = new byte[remainingBytes];
                    Array.Copy(buffer, index, this.rx_buffer, 0, remainingBytes);
                }
            }
        }

        private void handleRawFrameRead(NextFrame rawFrame)
        {
            if (isValidChecksum(rawFrame.RawBytes))
            {
                Frame frame = FrameParser.FrameFromRawBytes(rawFrame.RawBytes);
                if (frame != null)
                {
                    OnRecievedFrame(new ReceivedRemoteFrameEventArgs(frame));
                }
            }
            else
            {
                OnFrameDropped(new FrameDroppedByChecksumEventArgs(rawFrame.RawBytes));
            }

        }

        private void OnRecievedFrame(ReceivedRemoteFrameEventArgs e)
        {
            ReceivedRemoteFrameEventHandler handler = ReceivedRemoteFrame;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void OnFrameDropped(FrameDroppedByChecksumEventArgs e)
        {
            FrameDroppedByChecksumEventHandler handler = FrameDroppedByChecksum;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void OnBytesReadFromSerial(BytesReadFromSerialEventArgs e)
        {
            BytesReadFromSerialEventHandler handler = BytesReadFromSerial;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private byte[] readBytesFromSerial(SerialPort port, int nBytes)
        {
            byte[] buff = new byte[nBytes];
            int nRead = serialPort.Read(buff, 0, buff.Length);

            OnBytesReadFromSerial(new BytesReadFromSerialEventArgs(buff));
            
            return buff;
        }

        private bool isValidChecksum(byte[] rawFrame)
        {
            int sum = 0;
            int checksumIndex = rawFrame.Length - 1;
            for (int i = 3; i < checksumIndex; ++i)
            {
                sum += rawFrame[i];
            }

            return rawFrame[checksumIndex] == 0xFF - (sum & 0xFF);
        }

        private NextFrame nextFrameFromBuffer(byte[] buffer, int startIndex)
        {
            if (startIndex < 0 || startIndex >= buffer.Length || buffer.Length < 3) { return new NextFrame(); }

            UInt16 length = ByteOperations.littleEndianWordFromBytes(buffer[startIndex + XBEE_LENGTH_BYTE_INDEX], buffer[startIndex + XBEE_LENGTH_BYTE_INDEX + 1]);
            int frameLength = 3 + length + 1;
            if (buffer.Length < 3 + length + 1) { return new NextFrame(); }

            byte[] bytes = new byte[frameLength];
            Array.Copy(buffer, startIndex, bytes, 0, frameLength);
            return new NextFrame(bytes, startIndex);
        }
    }
}
