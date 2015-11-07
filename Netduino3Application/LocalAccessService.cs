using System;
using System.Text;
using Microsoft.SPOT.Net.NetworkInformation;

using HttpLibrary;
using NetduinoCore;
using System.IO;

namespace Netduino3Application
{
    interface ILocalAccessServiceDataSource
    {
        int NumberOfSensors { get; }
        string[] SensorInfoAtIndex(int index);
    }

    class LocalAccessService
    {
        private HttpServer httpServer;
        public HttpServer HttpService { get { return httpServer; } }

        public ILocalAccessServiceDataSource DataSource;

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
            if (DataSource == null) { return; }

            NDLogger.Log("HTTP Request received: " + new string (Encoding.UTF8.GetChars(e.Request)) + " File name: " + e.FileName, LogLevel.Verbose);

            try
            {
                switch (e.FileName)
                {
                    case "index.html":
                    case @"\SD\index.html":
                        RespondWithIndexPage();
                        break;
                }
            }
            catch (Exception ex)
            {
                HttpService.SendInternalServerError(ex.Message);
            }
        }

        void RespondWithIndexPage()
        {
            SendTemplate("index.template", delegate(string key)
            {
                switch (key)
                {
                    case "@SLI":
                        return SensorListInfo();
                }
                return "";
            });
        }

        private string SensorListInfo()
        {
            string sensorListHTTPAsString = "";
            for (int i = 0; i < DataSource.NumberOfSensors; ++i)
            {
                string listItem = "<li>";
                foreach (string info in DataSource.SensorInfoAtIndex(i))
                {
                    listItem += info + " ";
                }
                listItem += "</li>";
                sensorListHTTPAsString += listItem;
            }
            return sensorListHTTPAsString;
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

        public delegate string ReplacementStringForKeyCallback(string key);
        public void SendTemplate(string FileName, ReplacementStringForKeyCallback ReplacementStringForKey)
        {
            string outputFileName = @"\SD\" + FileNameWithoutExtension(FileName) + ".html";

            FileStream TemplateFile = new FileStream(@"\SD\" + FileName, FileMode.Open, FileAccess.Read);
            FileStream OutputFile = new FileStream(outputFileName, FileMode.Create, FileAccess.Write);

            int readByte = TemplateFile.ReadByte();
            string replaceBuffer;
            while (readByte != -1)
            {
                if (ByteToUTF8Char(readByte) == '@')
                {
                    replaceBuffer = "";
                    char readChar = ByteToUTF8Char(readByte);
                    while (readByte != -1 && readChar != ' ' && readChar != '\n')
                    {
                        replaceBuffer += readChar;
                        readByte = TemplateFile.ReadByte();
                        readChar = ByteToUTF8Char(readByte);
                    }

                    string replacement = ReplacementStringForKey(replaceBuffer) + readChar;
                    byte[] output = Encoding.UTF8.GetBytes(replacement);
                    OutputFile.Write(output, 0, output.Length);
                }
                else
                {
                    OutputFile.WriteByte((byte)readByte);
                }
                readByte = TemplateFile.ReadByte();
            }

            TemplateFile.Close();
            OutputFile.Close();
            HttpService.Send(outputFileName);
            File.Delete(outputFileName);
        }

        private string FileNameWithoutExtension(string FileName)
        {
            return FileName.Substring(0, FileName.LastIndexOf('.'));
        }

        private char ByteToUTF8Char(int _byte)
        {
            return Encoding.UTF8.GetChars(new byte[] { (byte)_byte })[0];
        }
    }
}
