using System;
using Microsoft.SPOT;
using System.IO.Ports;
using CoreCommunication;

namespace Xbee
{
    public delegate void ReceivedRemoteFrameEventHandler(object sender, Frame frame);

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
        private ByteBuffer rx_buffer;
        private FrameQueueService RequestResponseService;

        public XbeeDevice(SerialPort serialPort)
        {
            this.serialPort = serialPort;
            this.serialPort.Open();
            this.serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            this.serialPort.ErrorReceived += new SerialErrorReceivedEventHandler(ErrorReceivedHandler);
            rx_buffer = new ByteBuffer();
            RequestResponseService = new FrameQueueService();
            ReceivedRemoteFrame += RequestResponseService.onReceivedRemoteFrame;
            RequestResponseService.SendFrame += WriteFrame;
        }

        private bool isOn = true;
        private void WriteFrame(Frame frame)
        {
            isOn = !isOn;
            byte[] rawFrame = FrameSerializer.Serialize(frame);
            if (isOn)
            {
                rawFrame = new byte[] { 0x7E, 0x00, 0x10, 0x17, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFE, 0x02, 0x44, 0x34, 0x05, 0x6B };
            }
            else
            {
                rawFrame = new byte[] { 0x7E, 0x00, 0x10, 0x17, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFE, 0x02, 0x44, 0x34, 0x04, 0x6c };
            }

            this.serialPort.Write(rawFrame, 0, rawFrame.Length);
        }

        public void EnqueueFrame(Frame frame, Callback callback)
        {
            RequestResponseService.EnqueueFrame(frame, callback);
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort serialPort = (SerialPort)sender;
            if (serialPort != this.serialPort) { return; }

            int nBytes = serialPort.BytesToRead;
            if (nBytes > 0)
            {
                // Merge RxBuffer and incoming bytes to buffer
                byte[] bytes = readBytesFromSerial(serialPort, nBytes);
                rx_buffer.AddBytes(bytes);

                // Slice and Parse frames
                int index = 0;
                byte[] rawFrame = FrameSlicer.nextFrameFromBuffer(rx_buffer.RawBytes, index);
                while (rawFrame.Length > 0)
                {
                    handleRawFrameRead(rawFrame);

                    index += rawFrame.Length;
                    rawFrame = FrameSlicer.nextFrameFromBuffer(rx_buffer.RawBytes, index);
                }

                // Save partial last Frame
                rx_buffer.RemoveFirstNBytes(index);
            }
        }

        private void ErrorReceivedHandler(object sender, SerialErrorReceivedEventArgs e)
        {
            Debug.Print("Serial error received with type: " + e.EventType);
        }

        private void handleRawFrameRead(byte[] rawFrame)
        {
            if (isValidChecksum(rawFrame))
            {
                Frame frame = FrameParser.FrameFromRawBytes(rawFrame);
                if (frame != null)
                {
                    OnRecievedFrame(frame);
                }
            }
            else
            {
                OnFrameDropped(new FrameDroppedByChecksumEventArgs(rawFrame));
            }
        }

        private void OnRecievedFrame(Frame frame)
        {
            ReceivedRemoteFrameEventHandler handler = ReceivedRemoteFrame;
            if (handler != null)
            {
                handler(this, frame);
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
    }
}
