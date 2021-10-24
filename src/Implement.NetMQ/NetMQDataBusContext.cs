using MACOs.JY.ActorFramework.Core.DataBus;
using Newtonsoft.Json;
using System;

namespace MACOs.JY.ActorFramework.Implement.NetMQ
{
    [Serializable]
    public class NetMQDataBusContext : IDataBusContext
    {
        [JsonProperty]
        public int BeaconPort { get; set; } = 9999;
        [JsonProperty]
        public string IPAddress { get; set; } ="";
        [JsonProperty]
        public string AliasName { get; set; } = "";
        [JsonProperty]
        public int Port { get; set; } = -1;
        public SocketType Type { get; set; } = SocketType.tcp;
        public IDataBus NewInstance()
        {
            var bus = new NetMQDataBus(this);
            return bus;

        }
    }
}
