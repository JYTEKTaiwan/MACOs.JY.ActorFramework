using MACOs.JY.ActorFramework.Clients;
using NetMQ;
using System;
using System.Net;

namespace MACOs.JY.ActorFramework.Implement.NetMQ
{
    public class NetMQClientContext : IClientContext
    {
        private NetMQBeacon _beacon;
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
            NetMQClient client;
            var _logger = NLog.LogManager.GetCurrentClassLogger();
            _logger.Trace("Start searching designated peer");
            string connectedString = "";

            BeaconConfig();
            _beacon.Subscribe(Alias);

            BeaconMessage msg;
            if (_beacon.TryReceive(TimeSpan.FromSeconds(5), out msg))
            {
                connectedString = msg.String.Split('=')[1];
                _logger.Debug($"Peer is found at {connectedString}");
                _logger.Info("Peer is found");
                client = new NetMQClient();
                client.Connect(connectedString);
            }
            else
            {
                _logger.Error("Peer search timeout");
                client = null;
            }
            _beacon.Unsubscribe();
            _beacon.Dispose();
            return client;
        }

        private void BeaconConfig()
        {
            _beacon = new NetMQBeacon();

            //chcek if beacon port larger than 65536
            if (Port >= 65536 || Port < 0)
            {
                _beacon.Dispose();
                throw new ArgumentException("Beacon port must between 0 and 65536");
            }


            //configure beacon
            if (string.IsNullOrEmpty(Address))
            {
                _beacon.ConfigureAllInterfaces(Port);
            }
            else
            {
                try
                {
                    //Check if IP string is convertable
                    IPAddress ip = IPAddress.Parse(Address);
                }
                catch (Exception ex)
                {
                    _beacon.Dispose();

                    throw new ArgumentException("Invalid Beacon address", ex);
                }
                _beacon.Configure(Port, Address);
            }


            //check if beacon is sucessfully bounded to endpoint
            if (string.IsNullOrEmpty(_beacon.BoundTo))
            {
                _beacon.Dispose();

                throw new ArgumentException("Invalid Beacon address");
            }
        }


    }
}
