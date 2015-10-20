using System;
using Microsoft.SPOT;

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

        public static NDMQTTConfiguration DefaultConfiguration
        {
            get
            {
                NDMQTTConfiguration conf = new NDMQTTConfiguration();
                conf.UserName = "mkresz";
                conf.Password = "qwe12ASD";
                conf.HostName = "ec2-52-29-5-113.eu-central-1.compute.amazonaws.com";
                conf.HostPort = 1883;

                return conf;
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
            this.MQTT = NDMQTTConfiguration.DefaultConfiguration;
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
