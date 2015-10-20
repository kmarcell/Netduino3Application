using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;

namespace NetduinoCore
{
    class NDSockets
    {
        public static bool TryConnect(Socket s, EndPoint ep)
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
            while (checks-- > 0 && connected == false) Thread.Sleep(100);

            return connected;
        }
    }
}
