using System;
using Microsoft.SPOT;
using CloudLib;

namespace NetduinoCore
{
    class MQTTLogger : NDLogger
    {
        private MQTTCloudPlatform platform;

        public MQTTLogger(MQTTCloudPlatform platform)
        {
            this.platform = platform;
            this.handler = logToMQTT;
        }

        private void logToMQTT(string message)
        {
            if (platform != null)
            {
                CLEvent e = new CLEvent((int)CLEventType.LogMessage, message);
                try
                {
                    platform.PostEvent(e);
                }
                catch
                {
                }
            }
        }
    }
}
