using System;
using System.Net;
using System.Text;
using System.Threading;

using Microsoft.SPOT;
using Microsoft.SPOT.Net;

using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace CloudLib
{
    public enum MqttQoS : byte
    {
        DeliverExactlyOnce = MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE,
        DeliverAtLeastOnce = MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE,
        DeliverAtMostOnce = MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE,
        GrantedFailure = MqttMsgBase.QOS_LEVEL_GRANTED_FAILURE,
    }

    public class MqttMsgPublishReceivedEventArgs : EventArgs
    {
        public string Message;
        public MqttQoS QosLevel;
        public string Topic;

        public MqttMsgPublishReceivedEventArgs(byte[] Message, byte QosLevel, string Topic)
        {
            this.Message = new string(Encoding.UTF8.GetChars(Message));
            this.QosLevel = (MqttQoS)QosLevel;
            this.Topic = Topic;
        }
    }

    public delegate void MqttMsgPublishReceivedEventHandler(object sender, MqttMsgPublishReceivedEventArgs e);

    class MQTTCloudPlatform : ICloudPlatform
    {
        private MqttClient mqttClient;

        protected string host;
        protected string userName;
        protected string password;

        protected string clientID;

        public delegate string TopicFromEventHandler(CLEvent clEvent);
        public TopicFromEventHandler TopicFromEvent;
        public event MqttMsgPublishReceivedEventHandler MqttMsgPublishReceived;

        public MQTTCloudPlatform()
        {
        }

        public MQTTCloudPlatform(string clientID)
        {
            this.clientID = clientID;
        }

        public bool IsConnected
        {
            get
            {
                return mqttClient.IsConnected;
            }
        }

        public virtual int Connect(string host, string userName, string password, int port = 1883)
        {
            if (host == null || userName == null || password == null)
            {
                return -1;
            }
            
            this.userName = userName;
            this.password = password;
            this.host = host;

            mqttClient = new MqttClient(host, port, false, null, MqttSslProtocols.None);

            // register to message received 
            mqttClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

            string clientId = this.clientID == null ? Guid.NewGuid().ToString() : this.clientID;
            int returnCode = mqttClient.Connect(clientID, userName, password);

            return mqttClient.IsConnected ? 0 : returnCode;
        }

        void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            onMqttMsgPublishReceived(new MqttMsgPublishReceivedEventArgs(e.Message, e.QosLevel, e.Topic));
        }

        private void onMqttMsgPublishReceived(MqttMsgPublishReceivedEventArgs e)
        {
            MqttMsgPublishReceivedEventHandler handler = MqttMsgPublishReceived;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public void Disconnect()
        {
            mqttClient.Disconnect();
        }

        public virtual int SubscribeToEvents(MqttQoS qualityOfService, string[] subTopics)
        {
            int returnCode = mqttClient.Subscribe(subTopics, new byte[] { (byte)qualityOfService });
            return returnCode;
        }

        public int UnsubscribeFromEvents(string[] subTopics)
        {
            int returnCode = mqttClient.Unsubscribe(subTopics);
            return returnCode;
        }

        public virtual int PostEvent(CLEvent e)
        {
            if (!IsConnected) { return -1; }

            string topic = TopicFromEvent(e);
            string message = e.serialize();
            int returnCode = mqttClient.Publish(topic, Encoding.UTF8.GetBytes(message));
            return returnCode;
        }
    }
}
