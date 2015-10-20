using System;
using Microsoft.SPOT;

namespace Netduino3Application
{
    interface IApplication
    {
        void applicationWillStart();
        void didFinishLaunching();
    }
}
