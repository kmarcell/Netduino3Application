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
            this.TopicFromEventType = topicFromEventType;
            this.clientID = ClientID;
            this.ListenerThreadException += ListenerExceptionHandler;
        }

        private string topicFromEventType(int type)
        {
            String topic = "";
            switch (type)
            {
                case (int)CLEventType.CLTemperatureReadingEventType:
                    topic = NDConfiguration.DefaultConfiguration.MQTT.SensorDataTopic;
                    break;

                case (int)CLEventType.CLLogMessageEventType:
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

        public override int SubscribeToEvents(int[] topicQoS, string[] subTopics)
        {
            int returnCode = base.SubscribeToEvents(topicQoS, subTopics);
            if (returnCode == 0)
            {
                NDLogger.Log("Subscribed to " + subTopics, LogLevel.Verbose);
            }
            else
            {
                NDLogger.Log("Subscription failed with errorCode: " + returnCode, LogLevel.Error);
            }
            return returnCode;
        }

        public override int Connect(IPHostEntry host, string userName, string password, int port = 1883)
        {
            int returnCode = base.Connect(host, userName, password, port);
            if (returnCode != 0)
            {
                NDLogger.Log("MQTT connection error: " + returnCode, LogLevel.Error);
            }
            else
            {
                NDLogger.Log("Connected to MQTT", LogLevel.Verbose);
            }
            return returnCode;
        }

        public void ListenerExceptionHandler(object sender, ListenerThreadExceptionEventArgs e)
        {
            NDLogger.Log("MQTT cloud platform listener error: " + e.Exception.Message, LogLevel.Error);
            NDLogger.Log(e.Exception.StackTrace, LogLevel.Verbose);
            NDLogger.Log("MQTT cloud platform restarting", LogLevel.Verbose);
        }
    }
}
