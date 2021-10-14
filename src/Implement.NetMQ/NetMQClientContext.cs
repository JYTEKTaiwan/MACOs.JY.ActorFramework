using MACOs.JY.ActorFramework.Clients;
using NetMQ;
using NetMQ.Sockets;
using System;

namespace MACOs.JY.ActorFramework.Implement.NetMQ
{
    public class NetMQClientContext : IClientContext
    {
        private static object objLock = new object();
        public int Port { get; set; }
        public string Alias { get; set; }
        public string Address { get; set; } = "";

        public NetMQClientContext(int port, string alias, string address = "")
        {
            Port = port;
            Alias = alias;
            Address = address;
        }
        public IClient Search()
        {
            var client = new NetMQClient();
            var _logger = NLog.LogManager.GetCurrentClassLogger();
            _logger.Trace("Start searching designated peer");
            string connectedString = "";
            var _beacon = new NetMQBeacon();
            if (string.IsNullOrEmpty(Address))
            {
                _beacon.ConfigureAllInterfaces(Port);
            }
            else
            {
                _beacon.Configure(Port, Address);
            }
            _beacon.Subscribe(Alias);
            BeaconMessage msg;
            if (_beacon.TryReceive(TimeSpan.FromSeconds(5), out msg))
            {
                connectedString = msg.String.Split('=')[1];
                _logger.Debug($"Peer is found at {connectedString}");
                _logger.Info("Peer is found");
                client.Connect(connectedString);
            }
            else
            {
                _logger.Error("Peer search timeout");
                client= null;
            }
            _beacon.Unsubscribe();
            _beacon.Dispose();
            return client;
        }

    }
}
