using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Microsoft.SPOT;
using Microsoft.SPOT.Net;

using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

using Netduino_MQTT_Client_Library;

namespace CloudLib
{
    public class ListenerThreadExceptionEventArgs : EventArgs
    {
        private Exception exception;

        public ListenerThreadExceptionEventArgs(Exception exception)
        {
            this.exception = exception;
        }

        public Exception Exception
        {
            get { return this.exception; }
        }
    }

    public delegate void ListenerThreadExceptionEventHandler(object sender, ListenerThreadExceptionEventArgs e);

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
        protected bool isConnected;

        protected string clientID;

        public delegate string TopicFromEventTypeHandler(int eventType);
        public TopicFromEventTypeHandler TopicFromEventType;
        public event ListenerThreadExceptionEventHandler ListenerThreadException;

        ~MQTTCloudPlatform()
        {
            if (listenerThread != null)
            {
                listenerThread.Abort();
            }
        }

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
                return isConnected;
            }
        }

        public virtual int Connect(IPHostEntry host, string userName, string password, int port = 1883)
        {
            if (host == null || userName == null || password == null)
            {
                return Constants.CONNECTION_ERROR;
            }
            
            this.userName = userName;
            this.password = password;
            this.host = host;
            this.port = port;

            new Thread(delegate
            {
                try
                {
                    Connect();
                }
                catch
                {
                }
            }).Start();

            int checks = 50;
            while (checks-- > 0 && !IsConnected)
            {
                Thread.Sleep(100);
            }

            return IsConnected ? Constants.SUCCESS : Constants.CONNECTION_ERROR;
        }

        public void StartListen()
        {
            try
            {
                listenerThread = new Thread(mylistenerThread);
                listenerThread.Start();
            }
            catch (Exception e)
            {
                onListenerThreadException(new ListenerThreadExceptionEventArgs(e));
            }
        }

        public void StopListen()
        {
            try
            {
                listenerThread.Abort();
            }
            catch (ThreadAbortException)
            {

            }
        }

        private void Connect()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(host.AddressList[0], port));
            NetduinoMQTT.ConnectMQTT(socket, clientID, 20, true, userName, password);
            Timer pingTimer = new Timer(new TimerCallback(PingServer), null, 1000, 10000);
            isConnected = true;
        }

        public int Disconnect()
        {
            int returnCode = 0;
            isConnected = false;

            StopListen();
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
            if (!IsConnected) { return Constants.CONNECTION_ERROR; }

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
            if (IsConnected)
            {
                NetduinoMQTT.PingMQTT(socket);
            }
        }

        private void mylistenerThread()
        {
            try
            {
                NetduinoMQTT.listen(socket);
            }
            catch (SocketException se)
            {
                onListenerThreadException(new ListenerThreadExceptionEventArgs(se));
            }
        }

        private void onListenerThreadException(ListenerThreadExceptionEventArgs e)
        {
            ListenerThreadExceptionEventHandler handler = ListenerThreadException;
            if (handler != null)
            {
                handler(this, e);
            }
        }
    }
}
