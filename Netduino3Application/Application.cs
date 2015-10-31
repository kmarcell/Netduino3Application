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
using Xbee;

namespace Netduino3Application
{
    class Application : IApplication
    {
        private XbeeDevice xbeeCoordinator;
        private NDMQTT upstreamMQTT;
        private OutputPort onboardLED;
        private InterruptPort onboardButton;

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

            xbeeCoordinator = new XbeeDevice(createSerialPortWithName(SerialPorts.COM1));

            xbeeCoordinator.BytesReadFromSerial += new BytesReadFromSerialEventHandler(BytesReadFromSerialHandler);
            xbeeCoordinator.FrameDroppedByChecksum += new FrameDroppedByChecksumEventHandler(FrameDroppedByChecksumHandler);
            xbeeCoordinator.ReceivedRemoteFrame += new ReceivedRemoteFrameEventHandler(ReceivedRemoteFrameHandler);

            upstreamMQTT = new NDMQTT();
            startMQTT();

            // setup our interrupt port (on-board button)
            onboardButton = new InterruptPort((Cpu.Pin)0x15, false, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeHigh);

            // assign our interrupt handler
            onboardButton.OnInterrupt += new NativeEventHandler(button_OnInterrupt);
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
                    upstreamMQTT.UnsubscribeFromEvents();
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
            IPHostEntry hostEntry = null;
            try
            {
                hostEntry = Dns.GetHostEntry(Configuration.MQTT.HostName);
            }
            catch (Exception e)
            {
                NDLogger.Log("Unable to get host entry by DNS error: " + e, LogLevel.Error);
                return;
            }

            int returnCode = upstreamMQTT.Connect(hostEntry, Configuration.MQTT.UserName, Configuration.MQTT.Password, Configuration.MQTT.HostPort);

            if (returnCode == 0)
            {
                NDLogger.AddLogger(new MQTTLogger(upstreamMQTT));
            }
            else
            {
                upstreamMQTT = null;
                return;
            }

            upstreamMQTT.SubscribeToEvents(new int[] { 0 }, new String[] { Configuration.MQTT.SensorDataTopic });
            upstreamMQTT.StartListen();
        }

        public NDConfiguration Configuration
        {
            get { return NDConfiguration.DefaultConfiguration; }
        }

        void ReceivedRemoteFrameHandler(object sender, Frame frame)
        {
            double analogSample = (frame as DigitalAnalogSampleFrame).AnalogSampleData[0];
            double temperatureCelsius = ((analogSample / 1023.0 * 3.3) - 0.5) * 100.0;
            NDLogger.Log("Temperature " + temperatureCelsius + " Celsius" + " sample " + analogSample, LogLevel.Info);

            if (upstreamMQTT != null)
            {
                upstreamMQTT.PostEvent(new CLEvent((int)CLEventType.CLTemperatureReadingEventType, temperatureCelsius));
            }

            analogSample = 1023.0 - (frame as DigitalAnalogSampleFrame).AnalogSampleData[1];
            double ambientLightPercent = (analogSample / 1023.0) * 100.0;
            double lux = (analogSample / 1023.0) * 1200.0;
            NDLogger.Log("Ambient light percent " + ambientLightPercent + "% Lux: " + lux, LogLevel.Info);
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
                log += ByteToHex(bytes[i]) + " ";
            }
            NDLogger.Log(log, LogLevel.Verbose);
        }

        string ByteToHex(byte b)
        {
            const string hex = "0123456789ABCDEF";
            int lowNibble = b & 0x0F;
            int highNibble = (b & 0xF0) >> 4;
            string s = new string(new char[] { hex[highNibble], hex[lowNibble] });
            return s;
        }
    }
}
