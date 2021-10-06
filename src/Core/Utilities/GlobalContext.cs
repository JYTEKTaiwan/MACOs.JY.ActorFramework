using MACOs.JY.ActorFramework.Core.Devices;
using System.Collections;
using System.Collections.Generic;

namespace MACOs.JY.ActorFramework.Core.Utilities
{
    public delegate string DataReadyEvent(object sender, object args);
    public delegate string ExecuteCompleteEvent(object sender, string result);
    public class DeviceCollection : List<IDevice>
    {
    }
}
