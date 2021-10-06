using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace MACOs.JY.ActorFramework.Implement.NetMQ
{
    [Serializable]
    public class NetMQDataBusContext : IDataBusContext
    {
        [JsonProperty]
        public string LocalIP { get; set; } = "";
        [JsonProperty]
        public int BeaconPort { get; set; } = 9999;
        [JsonProperty]
        public string BeaconIPAddress { get; set; }
        [JsonProperty]
        public string AliasName { get; set; }
        [JsonProperty]
        public int Port { get; set; } = -1;
        [JsonProperty]
        public bool IsSilent { get; set; } = false;
        public IDataBus NewInstance()
        {
            var bus = new NetMQDataBus(this);
            return bus;

        }
    }
}
