using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Microsoft.SPOT;
using Microsoft.SPOT.Net.NetworkInformation;

namespace NetduinoCore
{
    class NDBroadcastAddress
    {
        private static NDBroadcastAddress instance;
        private const int UDP_PORT_NETBIOS_NS = 137;
        private Thread broadcastThread = null;

        private NDBroadcastAddress()
        {
        }

        public static NDBroadcastAddress sharedInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = new NDBroadcastAddress();
                }
                return instance;
            }
        }

        public Boolean isBroadcasting
        {
            get
            {
                return broadcastThread != null;
            }
        }

        public void startBroadcast(String broadcastAddress)
        {
            broadcastThread = new Thread(delegate
            {
                try
                {
                    broadcast();
                }
                catch (Exception e)
                {
                    NDLogger.Log("Broadcast exception " + e.Message, LogLevel.Error);
                    startBroadcast(broadcastAddress);
                }
            });

            broadcastThread.Start();
        }

        public void stopBroadcast()
        {
            broadcastThread.Abort();
        }

        public void broadcast()
        {

            byte[] myNbName = EncodeNetbiosName(NDConfiguration.DefaultConfiguration.NetbiosName);
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            using (Socket serverSocket = new Socket(AddressFamily.InterNetwork,
                                                    SocketType.Dgram,
                                                    ProtocolType.Udp))
            {
                serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true); // Enable broadcast
                EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, UDP_PORT_NETBIOS_NS);
                byte[] IP = IPAddress.Parse(networkInterfaces[0].IPAddress).GetAddressBytes();
                serverSocket.Bind(remoteEndPoint);

                while (true)
                {
                    if (serverSocket.Poll(1000, SelectMode.SelectRead))
                    {
                        byte[] inBuffer = new byte[serverSocket.Available];
                        int count = serverSocket.ReceiveFrom(inBuffer, ref remoteEndPoint);
                        if ((inBuffer[2] >> 3) == 0) // opcode == 0 
                        {
                            byte[] nbName = new byte[32];
                            Array.Copy(inBuffer, 13, nbName, 0, 32);
                            NDLogger.Log("NETBIOS NAME QUERY: " + DecodeNetbiosName(nbName), LogLevel.Verbose);
                            if (BytesEqual(inBuffer, 13, myNbName, 0, 32))
                            {
                                byte[] outBuffer = new byte[62];
                                outBuffer[0] = inBuffer[0]; // trnid 
                                outBuffer[1] = inBuffer[1]; // trnid 
                                outBuffer[2] = 0x85;

                                outBuffer[3] = 0x00;
                                outBuffer[4] = 0x00;
                                outBuffer[5] = 0x00;
                                outBuffer[6] = 0x00;

                                outBuffer[7] = 0x01;

                                outBuffer[8] = 0x00;
                                outBuffer[9] = 0x00;
                                outBuffer[10] = 0x00;
                                outBuffer[11] = 0x00;

                                outBuffer[12] = 0x20;
                                for (int i = 0; i < 32; i++)
                                {
                                    outBuffer[i + 13] = myNbName[i];
                                }

                                outBuffer[45] = 0x00;

                                outBuffer[46] = 0x00; outBuffer[47] = 0x20; // RR_TYPE: NB 
                                outBuffer[48] = 0x00; outBuffer[49] = 0x01; // RR_CLASS: IN 

                                outBuffer[50] = 0x00; // TTL 
                                outBuffer[51] = 0x0f;
                                outBuffer[52] = 0x0f;
                                outBuffer[53] = 0x0f;

                                outBuffer[54] = 0x00; outBuffer[55] = 0x06; // RDLENGTH 

                                outBuffer[56] = 0x60; outBuffer[57] = 0x00; // NB_FLAGS 

                                outBuffer[58] = IP[0];
                                outBuffer[59] = IP[1];
                                outBuffer[60] = IP[2];
                                outBuffer[61] = IP[3];

                                serverSocket.SendTo(outBuffer, remoteEndPoint);
                            }
                        }

                    }
                    Thread.Sleep(100);
                }
            }
        }

        public static Byte[] EncodeNetbiosName(string Name)
        {
            byte[] result = new byte[32];
            char c;
            for (int i = 0; i < 15; i++)
            {
                c = i < Name.Length ? Name[i] : ' ';
                result[i * 2] = (byte)(((byte)(c) >> 4) + 65);
                result[(i * 2) + 1] = (byte)(((byte)(c) & 0x0f) + 65);
            }
            result[30] = 0x41;
            result[31] = 0x41;
            return result;
        }

        public static string DecodeNetbiosName(byte[] NbName)
        {
            string result = "";
            for (int i = 0; i < 15; i++)
            {
                byte b1 = NbName[i * 2];
                byte b2 = NbName[(i * 2) + 1];
                char c = (char)(((b1 - 65) << 4) | (b2 - 65));
                result += c;
            }
            return result;
        }

        public static bool BytesEqual(byte[] Array1, int Start1, byte[] Array2, int Start2, int Count)
        {
            bool result = true;
            for (int i = 0; i < Count - 1; i++)
            {
                if (Array1[i + Start1] != Array2[i + Start2])
                {
                    result = false;
                    break;
                }
            }
            return result;
        }
    }
}
