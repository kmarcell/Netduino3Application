using System;
using System.Text;
using Microsoft.SPOT.Net.NetworkInformation;

using HttpLibrary;
using NetduinoCore;

namespace Netduino3Application
{
    class LocalAccessService
    {
        private HttpServer httpServer;
        public HttpServer HttpService { get { return httpServer; } }

        private static LocalAccessService currentInstance;
        public static LocalAccessService Current
        {
            get
            {
                if (currentInstance == null)
                {
                    currentInstance = new LocalAccessService();
                }
                return currentInstance;
            }
        }

        public LocalAccessService()
        {
            NetworkInterface NI = NetworkInterface.GetAllNetworkInterfaces()[0];

            ServerCredentials credentials = new ServerCredentials("mkresz", "admin", "admin");
            ServerConfiguration config = new ServerConfiguration(NI.IPAddress, NI.SubnetMask, NI.GatewayAddress, 80);
            httpServer = new HttpServer(config, credentials, 512, 256, @"\SD");
            httpServer.OnRequestReceived += new OnRequestReceivedDelegate(server_OnRequestReceived);
            httpServer.OnServerError += new OnErrorDelegate(server_OnServerError);
        }

        void server_OnRequestReceived(object sender, OnRequestReceivedArgs e)
        {
            NDLogger.Log("HTTP Request received: " + new string (Encoding.UTF8.GetChars(e.Request)) + " File name: " + e.FileName, LogLevel.Verbose);
            HttpService.Send(@"\SD\\index.txt");
        }

        void server_OnServerError(object sender, OnErrorEventArgs e)
        {
            NDLogger.Log("HTTP Request error: " + e.EventMessage, LogLevel.Error);
            HttpService.Stop();
            HttpService.Start();
        }

        public void Start()
        {
            HttpService.Start();
        }

        public void Stop()
        {
            httpServer.Stop();
        }
    }
}
