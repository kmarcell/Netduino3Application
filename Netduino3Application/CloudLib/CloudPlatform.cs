using System;
using System.Net;

namespace CloudLib
{
    public enum CLEventType : int
    {
        TemperatureReading,
        AmbientLightReading,
        SwitchStateChange,
        LogMessage,
    }

    public interface ICLSerialization
    {
        string serialize();
    }

    public class CLEvent : ICLSerialization
    {
        private int eventType;
        private double eventValue;
        private string eventMessage;
        public string SourceIdentifier;

        public CLEvent(int eventType, string eventMessage, string sourceIdentifier = "")
        {
            this.eventType = eventType;
            this.eventMessage = eventMessage;
            this.SourceIdentifier = sourceIdentifier;
        }

        public CLEvent(int eventType, double eventValue, string sourceIdentifier = "")
        {
            this.eventType = eventType;
            this.eventValue = eventValue;
            this.SourceIdentifier = sourceIdentifier;
        }

        public int EventType
        {
            get
            {
                return this.eventType;
            }
        }

        public double EventValue
        {
            get
            {
                return this.eventValue;
            }
        }

        public string EventMessage
        {
            get
            {
                return this.eventMessage;
            }
        }

        public string serialize()
        {
            if (eventMessage != null)
            {
                return this.eventMessage;
            }
            else
            {
                return "" + this.eventValue;
            }
        }
    }

    public interface ICloudPlatform
    {
        int Connect(string host, string userName, string password, int port = 1883);
        void Disconnect();
        int SubscribeToEvents(MqttQoS qualityOfService, string[] subTopics);
        int UnsubscribeFromEvents(string[] subTopics);
        int PostEvent(CLEvent e);
    }
}
