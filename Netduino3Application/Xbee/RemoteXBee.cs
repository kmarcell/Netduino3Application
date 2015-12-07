using System;
using System.Text;
using System.Collections;
using Microsoft.SPOT;

using CoreCommunication;

namespace XBee
{
    public enum WidgetType : byte
    {
        TemperatureSensor = 0x01,
        AmbientLightSensor = 0x02,
        Switch = 0x03,
    }

    public class Widget : Object
    {
        private WidgetType type;
        public WidgetType Type { get { return type; } }

        public double RawValue;
        public string Identifier;

        public Widget(WidgetType type)
        {
            this.type = type;
        }

        public override string ToString()
        {
            switch (type)
            {
                case WidgetType.TemperatureSensor:
                    return Value.ToString("F") + " °C (Celsius)";
                case WidgetType.AmbientLightSensor:
                    return Value.ToString("F") + " % (Ambient Light Percent)";
                case WidgetType.Switch:
                    return RawValue > 0 ? "On" : "Off";
                default:
                    return RawValue.ToString();
            }
        }

        public double Value
        {
            get
            {
                switch (type)
                {
                    case WidgetType.TemperatureSensor:
                        return ((RawValue / 1023.0 * 3.3) - 0.5) * 100.0;
                    case WidgetType.AmbientLightSensor:
                        return (1.0 - (RawValue / 1023.0)) * 100.0;
                    case WidgetType.Switch:
                        return RawValue;
                }

                return RawValue;
            }
        }
    }

    public delegate void UpdateEventHandler(RemoteXBee xbee, Widget[] updateData);

    public class RemoteXBee
    {
        public byte[] SourceAddress16Bit;
        public byte[] SourceAddress64Bit;

        private string identifier;
        public string Identifier
        {
            get { return identifier != null ? SerialNumber : identifier; }
            set { identifier = value; }
        }

        public event UpdateEventHandler UpdateEvent;

        public string SerialNumber
        {
            get
            {
                string serialNumber = "";
                foreach (byte b in SourceAddress64Bit)
                {
                    serialNumber += ByteOperations.ByteToHex(b);
                }
                return serialNumber;
            }
        }

        private XBeeCoordinator coordinator;

        public XBeeCoordinator Coordinator
        {
            get
            {
                return coordinator;
            }
            set
            {
                coordinator = value;
                coordinator.ReceivedRemoteFrame += new ReceivedRemoteFrameEventHandler(ReceivedRemoteFrameHandler);
                retrieveDeviceTypeIdentifier();
            }
        }

        private byte[] deviceTypeIdentifier;
        private Hashtable pinToWidgetMapping;

        public Widget[] LastUpdateData
        {
            get
            {
                ICollection values = pinToWidgetMapping.Values;
                Widget[] widgets = new Widget[values.Count];
                int i = 0;
                foreach (Widget w in values)
                {
                    widgets[i++] = w;
                }
                return widgets;
            }
        }

        public RemoteXBee(byte[] SourceAddress16Bit, byte[] SourceAddress64Bit, string Identifier)
        {
            this.SourceAddress16Bit = SourceAddress16Bit;
            this.SourceAddress64Bit = SourceAddress64Bit;
            this.Identifier = Identifier;
        }

        public void setValueOfWidgetWithType(double value, WidgetType widgetType)
        {
            Widget widget = null;
            int xbeePin = -1;
            foreach (int pin in pinToWidgetMapping.Keys)
            {
                widget = (Widget)pinToWidgetMapping[pin];
                if (widget.Type == widgetType)
                {
                    xbeePin = pin;
                    break;
                }
            }

            if (xbeePin > -1)
            {
                string command;
                byte[] commandData;
                switch (widgetType)
                {
                    case WidgetType.Switch:
                        command = "D" + xbeePin;
                        commandData = new byte[] { value > 0 ? (byte)0x05 : (byte)0x04};
                        break;
                    default:
                        return;
                }

                RemoteATCommandRequestFrame frame = FrameBuilder.RemoteATCommandRequest
                                .setATCommandName(command)
                                .setATCommandData(commandData)
                                .setDestinationAddress64Bit(this.SourceAddress64Bit)
                                .setDestinationAddress16Bit(this.SourceAddress16Bit)
                                .Build() as RemoteATCommandRequestFrame;

                coordinator.EnqueueFrame(frame, null);
            }
        }

        private void ReceivedRemoteFrameHandler(object sender, Frame frame)
        {
            if (!(frame is DigitalAnalogSampleFrame) ||
                !Frame.isEqualAddress((frame as DigitalAnalogSampleFrame).SourceAddress64Bit, SourceAddress64Bit) ||
                deviceTypeIdentifier == null)
            {
                return;
            }

            DigitalAnalogSampleFrame sample = frame as DigitalAnalogSampleFrame;

            for (int i = 0; i < sample.AnalogChannels.Length; ++i)
            {
                int pin = sample.AnalogChannels[i];
                Widget w = (Widget)pinToWidgetMapping[pin];
                if (w != null)
                {
                    w.RawValue = sample.AnalogSampleData[i];
                }
            }

            for (int i = 0; i < sample.DigitalChannels.Length; ++i)
            {
                int pin = sample.DigitalChannels[i];
                Widget w = (Widget)pinToWidgetMapping[pin];
                if (w != null)
                {
                    w.RawValue = sample.DigitalSampleData[pin];
                }
            }

            onUpdateReceived();
        }

        private void onUpdateReceived()
        {
            UpdateEventHandler handler = UpdateEvent;
            if (handler != null)
            {
                handler(this, LastUpdateData);
            }
        }

        private void createPinMapping()
        {
            Hashtable mapping = new Hashtable();

            byte[] mappingInfo = deviceTypeIdentifier;
            for (int i = 0; i < mappingInfo.Length; ++i)
            {
                byte b = mappingInfo[mappingInfo.Length -1 - i];
                byte msb = (byte)((b & 0xF0) >> 4);
                byte lsb = (byte)(b & 0x0F);

                WidgetType type;
                if (lsb != 0)
                {
                    type = (WidgetType)lsb;
                    mapping.Add(i * 2, new Widget(type));
                }

                if (msb != 0)
                {
                    type = (WidgetType)msb;
                    mapping.Add(i * 2 + 1, new Widget(type));
                }
            }

            pinToWidgetMapping = mapping;
        }

        private void retrieveDeviceTypeIdentifier()
        {
            RemoteATCommandRequestFrame frame = FrameBuilder.RemoteATCommandRequest
                .setDestinationAddress64Bit(SourceAddress64Bit)
                .setATCommandName("DD")
                .Build() as RemoteATCommandRequestFrame;

            Coordinator.EnqueueFrame(frame, delegate(Frame response)
            {
                if (!(response is RemoteATCommandResponseFrame)) { return false; }

                deviceTypeIdentifier = (response as RemoteATCommandResponseFrame).ATCommandData;
                createPinMapping();
                return true;
            });
        }
    }
}
