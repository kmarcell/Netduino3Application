using System;
using System.Net;
using System.Threading;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Net.NetworkInformation;

using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

using NetduinoCore;

namespace Netduino3Application
{
    public class Program
    {
        static IApplication application;

        internal static IApplication Application
        {
            get
            {
                return application;
            }
            set
            {
                application = value;
            }
        }

        public static void Main()
        {
            try
            {
                runApplication();
            }
            catch (Exception e)
            {
                Debug.Print("Unhandled exception happened, program restarted.");
                Debug.Print(e.StackTrace);
            }

            Thread.Sleep(Timeout.Infinite);
        }

        public static void runApplication()
        {
            Application = new Application();
            Application.applicationWillStart();
            waitForNetworkSetUp();
            setupBroadcast();
            Application.didFinishLaunching();
        }

        static void waitForNetworkSetUp()
        {
            while (IPAddress.GetDefaultLocalAddress() == IPAddress.Any) ;
        }

        static void setupBroadcast()
        {
            if (!NDBroadcastAddress.sharedInstance.isBroadcasting)
            {
                NDBroadcastAddress.sharedInstance.startBroadcast(NDConfiguration.DefaultConfiguration.BroadcastAddress);
            }  
        }
    }
}
