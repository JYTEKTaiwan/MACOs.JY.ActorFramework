using MACOs.JY.ActorFramework.Core.DataBus;
using Newtonsoft.Json;
using System;

namespace MACOs.JY.ActorFramework.Implement.NetMQ
{
    /// <summary>
    /// DataBusContex object that store the parameter
    /// </summary>
    [Serializable]
    public class NetMQDataBusContext : IDataBusContext
    {
        /// <summary>
        /// Beacon will be subscribed from this port. Default is 9999
        /// </summary>
        [JsonProperty]
        public int BeaconPort { get; set; } = 9999;
        /// <summary>
        /// Beacon will be subscribed from thie ip address (ex xxx.xxx.xxx.xxx). Use empty string if "127.0.0.1" is need. Default is empty string
        /// </summary>
        [JsonProperty]
        public string BeaconIP { get; set; } = "";
        /// <summary>
        /// The unique alias name that Beacon will be subscribed, auto assigned random number if keep empty
        /// </summary>
        [JsonProperty]
        public string AliasName { get; set; }

        public SocketType Type { get; set; } = SocketType.tcp;

        public bool EnableLogging { get; set; } = false;
        public IDataBus NewInstance()
        {
            var bus = new NetMQDataBus(this);
            return bus;

        }
    }
}
