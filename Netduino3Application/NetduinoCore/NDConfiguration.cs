using System;
using System.Text;
using Microsoft.SPOT;
using System.IO;

namespace NetduinoCore
{
    class NDMQTTConfiguration
    {

        public string UserName;
        public string Password;
        public string HostName;
        public int HostPort;

        public string RootTopic
        {
            get { return "users/" + this.UserName; }
        }

        public string SensorDataTopic
        {
            get { return RootTopic + "/sensors"; }
        }

        public string LogTopic
        {
            get { return RootTopic + "/log"; }
        }

        public NDMQTTConfiguration(string host, string username, string password)
        {
            HostName = host;
            UserName = username;
            Password = password;
            HostPort = 1883;
        }

        public void WriteMqttConfigurationToFile(string FileName)
        {
            FileStream OutputFile = new FileStream(FileName, FileMode.Create, FileAccess.Write);

            string host = NDConfiguration.DefaultConfiguration.MQTT.HostName;
            string username = NDConfiguration.DefaultConfiguration.MQTT.UserName;
            string password = NDConfiguration.DefaultConfiguration.MQTT.Password;

            string data = host + "," + username + "," + password + "\n";
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            OutputFile.Write(bytes, 0, bytes.Length);

            OutputFile.Close();
        }

        public static NDMQTTConfiguration ReadFromFile(string FileName)
        {
            FileStream InputFile;
            try
            {
                InputFile = new FileStream(FileName, FileMode.Open, FileAccess.Read);

                byte[] buffer = new byte[InputFile.Length];
                int readBytes = 0;
                while (InputFile.CanRead)
                {
                    readBytes += InputFile.Read(buffer, readBytes, 1024);
                }
                InputFile.Close();

                string mqttConfig = new string(Encoding.UTF8.GetChars(buffer)).TrimEnd(new char[] {'\n'});
                if (mqttConfig.Length > 0)
                {
                    string[] split = mqttConfig.Split(new char[] { ',' });
                    return new NDMQTTConfiguration(split[0], split[1], split[2]);
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            } 
        }
    }

    class NDConfiguration
    {
        public string NetbiosName;
        public string BroadcastAddress;
        public NDMQTTConfiguration MQTT;

        private NDConfiguration()
        {
            this.NetbiosName = "NETDUINO";
            this.BroadcastAddress = "192.168.0.255";
        }
        private static NDConfiguration instance;

        public static NDConfiguration DefaultConfiguration
        {
            get
            {
                if (instance == null)
                {
                    instance = new NDConfiguration();
                }
                return instance;
            }
        }
    }

}
