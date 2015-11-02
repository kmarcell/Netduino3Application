using System;
using Microsoft.SPOT;
using System.Net;
using Microsoft.SPOT.Net.NetworkInformation;

using CloudLib;

namespace NetduinoCore
{
    class NDMQTT : MQTTCloudPlatform
    {
        public NDMQTT()
        {
            this.TopicFromEvent = topicFromEvent;
            this.clientID = ClientID;
        }

        private string topicFromEvent(CLEvent clEvent)
        {
            String topic = "";
            switch (clEvent.EventType)
            {
                case (int)CLEventType.TemperatureReading:
                case (int)CLEventType.AmbientLightReading:
                    topic = NDConfiguration.DefaultConfiguration.MQTT.SensorDataTopic + "/" + clEvent.SourceIdentifier;
                    break;

                case (int)CLEventType.LogMessage:
                    topic = NDConfiguration.DefaultConfiguration.MQTT.LogTopic;
                    break;

                default:
                    break;
            }

            return topic;
        }

        private string ClientID
        {
            get
            {
                NetworkInterface[] netIF = NetworkInterface.GetAllNetworkInterfaces();

                string macAddress = "";

                // Create a character array for hexidecimal conversion.
                const string hexChars = "0123456789ABCDEF";

                // Loop through the bytes.
                for (int b = 0; b < 6; b++)
                {
                    // Grab the top 4 bits and append the hex equivalent to the return string.
                    macAddress += hexChars[netIF[0].PhysicalAddress[b] >> 4];

                    // Mask off the upper 4 bits to get the rest of it.
                    macAddress += hexChars[netIF[0].PhysicalAddress[b] & 0x0F];

                    // Add the dash only if the MAC address is not finished.
                    if (b < 5) macAddress += "-";
                }

                return macAddress;
            }
        }

        public override int SubscribeToEvents(MqttQoS qualityOfService, string[] subTopics)
        {
            int returnCode = base.SubscribeToEvents(qualityOfService, subTopics);
            NDLogger.Log("Subscribed to " + subTopics, LogLevel.Verbose);
            return returnCode;
        }

        public override int Connect(string host, string userName, string password, int port = 1883)
        {
            int returnCode = base.Connect(host, userName, password);
            if (IsConnected)
            {
                NDLogger.Log("Connected to MQTT!", LogLevel.Verbose);
            }
            else
            {
                NDLogger.Log("MQTT connection error!", LogLevel.Error);
            }
            return returnCode;
        }
    }
}
