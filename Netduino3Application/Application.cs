using System;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Net.NetworkInformation;

using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

using CloudLib;
using NetduinoCore;
using CoreCommunication;
using XBee;

namespace Netduino3Application
{
    class Application : IApplication
    {
        private XBeeCoordinator xbeeCoordinator;
        private NDMQTT upstreamMQTT;
        private OutputPort onboardLED;
        private InterruptPort onboardButton;
        private XBeeDiscoveryService discoveryService;
        private RemoteXBee[] knownDevices;

        public void applicationWillStart()
        {
            // Logging
            NDLogger.RemoveLoggers();
            NDLogger.AddLogger(new NDTTYLogger());
            NDLogger.SetLogLevel(LogLevel.Verbose);

            NDLogger.Log("Program started!");
            NDLogger.Log("Waiting for DHCP to set up.", LogLevel.Verbose);

            if (onboardLED == null)
            {
                onboardLED = new OutputPort(Pins.ONBOARD_LED, false);
            }
            onboardLED.Write(true);
        }

        public void didFinishLaunching()
        {
            // Ehernet didSetup
            onboardLED.Write(false);
            NetworkInterface NI = NetworkInterface.GetAllNetworkInterfaces()[0];
            NDLogger.Log("Network IP " + NI.IPAddress.ToString(), LogLevel.Verbose);

            xbeeCoordinator = new XBeeCoordinator(createSerialPortWithName(SerialPorts.COM1));

            xbeeCoordinator.BytesReadFromSerial += new BytesReadFromSerialEventHandler(BytesReadFromSerialHandler);
            xbeeCoordinator.FrameDroppedByChecksum += new FrameDroppedByChecksumEventHandler(FrameDroppedByChecksumHandler);
            xbeeCoordinator.StartListen();

            upstreamMQTT = new NDMQTT();

            // setup our interrupt port (on-board button)
            onboardButton = new InterruptPort((Cpu.Pin)0x15, false, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeHigh);

            // assign our interrupt handler
            onboardButton.OnInterrupt += new NativeEventHandler(button_OnInterrupt);

            discoveryService = new XBeeDiscoveryService(xbeeCoordinator);
            discoveryService.Discover(delegate(RemoteXBee[] knownDevices)
            {
                this.knownDevices = knownDevices;
                xbeeCoordinator.ReceivedRemoteFrame += new ReceivedRemoteFrameEventHandler(ReceivedRemoteFrameHandler);
            });
        }

        private SerialPort createSerialPortWithName(string name)
        {
            SerialPort port = new SerialPort(name, 9600, Parity.None, 8, StopBits.One);
            port.Handshake = Handshake.None;

            return port;
        }

        // the interrupt handler for the button
        void button_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            if (upstreamMQTT.IsConnected)
            {
                try
                {
                    upstreamMQTT.UnsubscribeFromEvents(new String[] { Configuration.MQTT.SensorDataTopic });
                }
                catch
                {
                    NDLogger.Log("MQTT unsubscribe exception!", LogLevel.Verbose);
                }

                upstreamMQTT.Disconnect();
                upstreamMQTT = null;
                NDLogger.Log("MQTT connection cancelled", LogLevel.Verbose);
            }
            else
            {
                try
                {
                    startMQTT();
                }
                catch
                {
                    NDLogger.Log("MQTT start exception!", LogLevel.Verbose);
                }
            }
        }

        void startMQTT()
        {
            int returnCode = upstreamMQTT.Connect(Configuration.MQTT.HostName, Configuration.MQTT.UserName, Configuration.MQTT.Password);

            if (returnCode == 0)
            {
                NDLogger.AddLogger(new MQTTLogger(upstreamMQTT));
                upstreamMQTT.MqttMsgPublishReceived += new MqttMsgPublishReceivedEventHandler(MqttMsgPublishReceived);
            }
            else
            {
                upstreamMQTT = null;
                return;
            }

            upstreamMQTT.SubscribeToEvents(MqttQoS.DeliverAtMostOnce, new String[] { Configuration.MQTT.SensorDataTopic });
        }

        private void MqttMsgPublishReceived(object sender, MqttMsgPublishReceivedEventArgs e)
        {

        }

        public NDConfiguration Configuration
        {
            get { return NDConfiguration.DefaultConfiguration; }
        }

        void ReceivedRemoteFrameHandler(object sender, Frame frame)
        {
            if (!(frame is DigitalAnalogSampleFrame)) { return; }

            DigitalAnalogSampleFrame sampleFrame = frame as DigitalAnalogSampleFrame;
            double analogSample = sampleFrame.AnalogSampleData[0];
            double temperatureCelsius = ((analogSample / 1023.0 * 3.3) - 0.5) * 100.0;
            NDLogger.Log("Temperature " + temperatureCelsius + " Celsius" + " sample " + analogSample, LogLevel.Info);

            analogSample = 1023.0 - (frame as DigitalAnalogSampleFrame).AnalogSampleData[1];
            double ambientLightPercent = (analogSample / 1023.0) * 100.0;
            double lux = (analogSample / 1023.0) * 1200.0;
            NDLogger.Log("Ambient light percent " + ambientLightPercent + "% Lux: " + lux, LogLevel.Info);

            if (upstreamMQTT != null)
            {
                RemoteXBee sourceXBee = null;
                foreach (RemoteXBee xbee in knownDevices)
                {
                    if (Frame.isEqualAddress(xbee.SourceAddress64Bit, sampleFrame.SourceAddress64Bit))
                    {
                        sourceXBee = xbee;
                    }
                }

                if (sourceXBee == null)
                {
                    sourceXBee = new RemoteXBee(sampleFrame.SourceAddress16Bit, sampleFrame.SourceAddress64Bit, "");
                    discoveryService.AddXBee(sourceXBee);
                    knownDevices = discoveryService.KnownDevices;
                }

                CLEvent e = new CLEvent((int)CLEventType.TemperatureReading, temperatureCelsius);
                e.SourceIdentifier = sourceXBee.SerialNumber;
                upstreamMQTT.PostEvent(e);
            }
        }

        void FrameDroppedByChecksumHandler(object sender, FrameDroppedByChecksumEventArgs e)
        {
            NDLogger.Log("Frame dropped because of checksum:", LogLevel.Error);
            logBytesRead(e.RawBytes);
        }

        void BytesReadFromSerialHandler(object sender, BytesReadFromSerialEventArgs e)
        {
            NDLogger.Log("Bytes read from serial:", LogLevel.Verbose);
            logBytesRead(e.RawBytes);
        }


        void logBytesRead(byte[] bytes)
        {
            string log = "";
            for (int i = 0; i < bytes.Length; ++i)
            {
                log += ByteOperations.ByteToHex(bytes[i]) + " ";
            }
            NDLogger.Log(log, LogLevel.Verbose);
        }
    }
}
