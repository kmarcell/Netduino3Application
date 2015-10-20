using System;
using System.Net;

namespace CloudLib
{
    public enum CLEventType : int
    {
        CLTemperatureReadingEventType,
        CLLogMessageEventType,
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

        public CLEvent(int eventType, string eventMessage)
        {
            this.eventType = eventType;
            this.eventMessage = eventMessage;
        }

        public CLEvent(int eventType, double eventValue)
        {
            this.eventType = eventType;
            this.eventValue = eventValue;
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
        int Connect(IPHostEntry host, string username, string password, int port);
        int Disconnect();

        int SubscribeToEvents(int[] topicQoS, string[] subTopics);
        int UnsubscribeFromEvents();

        int PostEvent(CLEvent e);
    }
}
