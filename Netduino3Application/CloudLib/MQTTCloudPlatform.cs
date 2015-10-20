using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Microsoft.SPOT;
using Netduino_MQTT_Client_Library;

namespace CloudLib
{
    class MQTTCloudPlatform : ICloudPlatform
    {
        private Socket socket;
        private Thread listenerThread;

        private int[] topicQoS;
        protected string[] subTopics;

        protected IPHostEntry host;
        protected string userName;
        protected string password;
        protected int port;

        protected string clientID;

        public delegate string TopicFromEventTypeHandler(int eventType);
        public TopicFromEventTypeHandler TopicFromEventType;

        ~MQTTCloudPlatform()
        {
            if (listenerThread != null)
            {
                listenerThread.Abort();
            }
            
            if (socket != null)
            {
                socket.Close();
            }
        }

        public MQTTCloudPlatform()
        {
        }

        public MQTTCloudPlatform(string clientID)
        {
            this.clientID = clientID;
        }

        public virtual int Connect(IPHostEntry host, string userName, string password, int port = 1883)
        {

            if (host == null || userName == null || password == null)
            {
                return Constants.CONNECTION_ERROR;
            }

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            bool success = TryConnect(socket, new IPEndPoint(host.AddressList[0], port));
            this.userName = userName;
            this.password = password;
            this.host = host;
            this.port = port;

            if (!success)
            {
                socket.Close();
                socket = null;
                SocketError();
                return Constants.CONNECTION_ERROR;
            }

            int returnCode = NetduinoMQTT.ConnectMQTT(socket, clientID, 20, true, userName, password);
            if (returnCode != Constants.SUCCESS)
            {
                return returnCode;
            }

            Timer pingTimer = new Timer(new TimerCallback(PingServer), null, 1000, 10000);

            // Setup and start a new thread for the listener
            listenerThread = new Thread(mylistenerThread);
            listenerThread.Start();

            return 0;
        }

        public virtual void SocketError() { }

        bool TryConnect(Socket s, EndPoint ep)
        {
            bool connected = false;
            new Thread(delegate
            {
                try
                {
                    s.Connect(ep);
                    connected = true;
                }
                catch { }

            }).Start();

            int checks = 10;
            while (checks-- > 0 && connected == false)
            {
                Thread.Sleep(100);
            }
            
            return connected;
        }

        public int Disconnect()
        {
            int returnCode = 0;
            try
            {
                returnCode = NetduinoMQTT.DisconnectMQTT(socket);
            }
            catch { }

            socket.Close();
            socket = null;

            return returnCode;
        }

        public virtual int SubscribeToEvents(int[] topicQoS, string[] subTopics)
        {
            this.topicQoS = topicQoS;
            this.subTopics = subTopics;
            int returnCode = NetduinoMQTT.SubscribeMQTT(socket, subTopics, topicQoS, 1);

            return returnCode;
        }

        public int UnsubscribeFromEvents()
        {
            int returnCode = NetduinoMQTT.UnsubscribeMQTT(socket, this.subTopics, this.topicQoS, 1);
            return returnCode;
        }

        public int PostEvent(CLEvent e)
        {
            if (listenerThread == null) { return 1; }

            string topic = TopicFromEventType(e.EventType);
            string message = e.serialize();
            try
            {
                NetduinoMQTT.PublishMQTT(socket, topic, message);
            }
            catch
            {
                Disconnect();
                Connect(host, userName, password, port);
            }

            // do not log publish here with mqtt logger, it causes a call cycle

            return 0;
        }

        /** Private **/

        // The function that the timer calls to ping the server
        // Our keep alive is 15 seconds - we ping again every 10. 
        // So we should live forever.
        private void PingServer(object o)
        {
            NetduinoMQTT.PingMQTT(socket);
        }

        // The thread that listens for inbound messages
        private void mylistenerThread()
        {
            try
            {
                NetduinoMQTT.listen(socket);
            }
            catch (Exception e)
            {
                ListenerThreadException(e);
                Disconnect();
                Connect(host, userName, password, port);
            }
        }

        public virtual void ListenerThreadException(Exception e) { }
    }
}
