﻿using Newtonsoft.Json.Linq;

namespace MACOs.JY.ActorFramework.Core.Devices
{
    public interface IDeviceContext
    {
        IDevice NewInstance();

    }
}
