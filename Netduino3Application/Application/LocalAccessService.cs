using System;
using System.Text;
using Microsoft.SPOT;
using Microsoft.SPOT.Net.NetworkInformation;

using HttpLibrary;
using NetduinoCore;
using System.IO;

namespace Netduino3Application
{
    interface ILocalAccessServiceDataSource
    {
        int NumberOfSensors { get; }
        int NumberOfWidgetsOfSensor(int sensorIndex);
        string[] SensorInfoAtIndex(int sensorIndex, int widgetIndex);
    }

    public class MqttConfigurationEventArgs : EventArgs
    {
        public string Host;
        public string UserName;
        public string Password;

        public MqttConfigurationEventArgs(string host, string username, string password)
        {
            Host = host;
            UserName = username;
            Password = password;
        }
    }

    public delegate void MqttConfigurationReceivedHandler(MqttConfigurationEventArgs e);

    class LocalAccessService
    {
        private HttpServer httpServer;
        public HttpServer HttpService { get { return httpServer; } }

        public ILocalAccessServiceDataSource dataSource;
        public event MqttConfigurationReceivedHandler MqttConfigurationReceived;

        private static LocalAccessService currentInstance;
        public static LocalAccessService SharedInstance
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

        private LocalAccessService()
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

            if (e.FileName == "sensor_data.csv")
            {
                try
                {
                    RespondWithSensorDataFile();
                }
                catch (Exception ex)
                {
                    HttpService.SendInternalServerError(ex.Message);
                }
            }
            else if (e.FileName == "")
            {
                try
                {
                    RespondWithLocalFile(@"\SD\\index.html");
                }
                catch (Exception ex)
                {
                    HttpService.SendInternalServerError(ex.Message);
                }
            }
            else if (e.IsInMemoryCard)
            {
                try
                {
                    RespondWithLocalFile(e.FileName);
                }
                catch (Exception ex)
                {
                    HttpService.SendInternalServerError(ex.Message);
                }
            }
            else if (e.FileName.IndexOf("setMqttConfiguration.html") > -1)
            {
                string fileName, username, password, host;
                string[] split = e.FileName.Split(new char[] { '?' });
                fileName = split[0];
                string[] parameters = split[1].Split(new char[] { '&' });
                username = parameters[0].Split(new char[] { '=' })[1];
                password = parameters[1].Split(new char[] { '=' })[1];
                host     = parameters[2].Split(new char[] { '=' })[1];

                onMqttConfigurationReceived(host, username, password);
                HttpService.SendOK();
            }
            else if (e.FileName.IndexOf("mqtt_configuration.csv") > -1)
            {
                try
                {
                    RespondWithMqttConfigurationFile();
                }
                catch (Exception ex)
                {
                    HttpService.SendInternalServerError(ex.Message);
                }
            }
        }

        private void onMqttConfigurationReceived(string host, string username, string password)
        {
            MqttConfigurationReceivedHandler handler = MqttConfigurationReceived;
            if (handler != null)
            {
                handler(new MqttConfigurationEventArgs(host, username, password));
            }
        }

        private void RespondWithLocalFile(string FileName)
        {
            HttpService.Send(FileName);
        }

        private void RespondWithSensorDataFile()
        {
            WriteSensorDataToFile(@"\SD\\sensor_data.csv");
            HttpService.Send("sensor_data.csv");
        }

        private void WriteSensorDataToFile(string FileName)
        {
            FileStream OutputFile = new FileStream(FileName, FileMode.Create, FileAccess.Write);

            for (int i = 0; i < DataSource.NumberOfSensors; ++i)
            {
                for (int j = 0; j < DataSource.NumberOfWidgetsOfSensor(i); ++j )
                {
                    string[] sensorData = DataSource.SensorInfoAtIndex(i, j);
                    string listItem = sensorData[0];
                    for (int k = 1; k < sensorData.Length; ++k)
                    {
                        listItem += "," + sensorData[k];
                    }
                    listItem += "\n";

                    byte[] bytes = Encoding.UTF8.GetBytes(listItem);
                    OutputFile.Write(bytes, 0, bytes.Length);
                }
            }

            OutputFile.Close();
        }

        private void RespondWithMqttConfigurationFile()
        {
            if (NDConfiguration.DefaultConfiguration.MQTT != null)
            {
                NDConfiguration.DefaultConfiguration.MQTT.WriteMqttConfigurationToFile(@"\SD\\mqtt_configuration.csv");
            }
            else
            {
                FileStream OutputFile = new FileStream(@"\SD\\mqtt_configuration.csv", FileMode.Create, FileAccess.Write);
                OutputFile.Close();
            }
            HttpService.Send("mqtt_configuration.csv");
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

        internal ILocalAccessServiceDataSource DataSource
        {
            get
            {
                return dataSource;
            }
            set
            {
                dataSource = value;
            }
        }
    }
}
