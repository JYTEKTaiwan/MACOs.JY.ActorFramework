using MACOs.JY.ActorFramework.Clients;
using NetMQ;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MACOs.JY.ActorFramework.Implement.NetMQ
{
    public enum SocketType
    {
        tcp,
        inproc,
    }
    public class NetMQClientContext : IClientContext
    {
        private NetMQBeacon _beacon;
        public int Port { get; set; }
        public string Alias { get; set; }
        public string Address { get; set; } = "";
        public SocketType Type { get; set; } = SocketType.tcp;
        public int LocalPort { get; set; } = -1;

        public NetMQClientContext(int port, string alias, string address )
        {
            Port = port;
            Alias = alias;
            Address = address;
        }
        public IClient Search()
        {
            NetMQClient client = null;
            bool isAccepted = false;
            bool res = false;
            try
            {
                client = new NetMQClient();
                _beacon = new NetMQBeacon();

                var _logger = NLog.LogManager.GetCurrentClassLogger();
                _logger.Trace("Start searching designated peer");

                BeaconCheck();

                BeaconConfig();

                client.Bind(Type + "://" + Address, LocalPort);
                client.SocketAccept = () =>
                {
                    isAccepted = true;
                    _beacon.Silence();
                };
                _beacon.Publish(Alias + ">" + client.EndPoint);
                res = client.StartListening(5000);
                client.SocketAccept = null;
                _beacon.Silence();
                _beacon.Dispose();

                if (!res)
                {
                    if (isAccepted)
                    {
                        throw new NullReferenceException("Client not found: Connection is established, but ACK failed");
                    }
                    else
                    {
                        throw new NullReferenceException("Client not found: Connection is not established");

                    }
                }

                return client;

            }
            catch (Exception ex)
            {
                _beacon?.Dispose();
                client?.Dispose();
                client = null;
                throw ex;
            }
        }

        private void BeaconConfig()
        {
            //configure beacon
            if (string.IsNullOrEmpty(Address))
            {
                _beacon.ConfigureAllInterfaces(Port);
            }
            else
            {
                _beacon.Configure(Port, Address);
            }
            //check if beacon is sucessfully bounded to endpoint
            if (string.IsNullOrEmpty(_beacon.BoundTo))
            {
                _beacon.Dispose();

                throw new ArgumentException("Invalid Beacon address");
            }

        }

        private void BeaconCheck()
        {

            //chcek if beacon port larger than 65536
            if (Port >= 65536 || Port < 0)
            {
                throw new ArgumentException("Beacon port must between 0 and 65536");
            }

            if (!string.IsNullOrEmpty(Address))
            {
                try
                {
                    //Check if IP string is convertable
                    IPAddress ip = IPAddress.Parse(Address);
                }
                catch (Exception ex)
                {

                    throw new ArgumentException("Invalid Beacon address", ex);
                }
            }

        }


    }
}
