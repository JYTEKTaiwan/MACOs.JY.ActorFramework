using MACOs.JY.ActorFramework.Clients;
using NetMQ;
using System;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MACOs.JY.ActorFramework.Implement.NetMQ
{
    public enum SocketType
    {
        tcp,
        inproc,
    }

    /// <summary>
    /// Context that is used to automatically create NetMQClient object
    /// </summary>
    [Serializable]
    public class NetMQClientContext : IClientContext
    {
        private NetMQBeacon _beacon;
        /// <summary>
        /// Beacon will be published through this port. Default is 9999
        /// </summary>
        public int BeaconPort { get; set; } = 9999;
        /// <summary>
        /// Alias name that beacon will be searching for, must be assigned
        /// </summary>
        public string TargetAlias { get; set; }
        /// <summary>
        /// Beacon will be puclished through this ip setting (ex:xxx.xxx.xxx.xxx). Use empty string if "127.0.0.1" is needed. Default value is empty string
        /// </summary>
        public string BeaconIP { get; set; } = "";
        /// <summary>
        /// Socket type for NetMQ DealerSocket. Default is tcp
        /// </summary>
        public SocketType Type { get; set; } = SocketType.tcp;

        /// <summary>
        /// DealerSocket will be listening at this port. Random assigned if value is -1. Default value is -1
        /// </summary>
        public int ListeningPort { get; set; } = -1;
        /// <summary>
        /// DealerSocket will be listening on thie ip address. Defult value is first IPV.4 ip address
        /// </summary>
        public string ListeningIP { get; set; }= Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString();

        public NetMQClientContext(string targetAlias, int beaconPort=9999, string beaconIp="" )
        {
            BeaconPort = beaconPort;
            TargetAlias = targetAlias;
            BeaconIP = beaconIp;
        }
        public IClient Search()
        {
            NetMQClient client = null;
            bool isAccepted = false;
            bool res = false;
            try
            {
                if (string.IsNullOrEmpty(TargetAlias))
                {
                    throw new NetMQClientException("Target alias cannot be empty");
                }
                client = new NetMQClient();
                BeaconConfig();

                var _logger = NLog.LogManager.GetCurrentClassLogger();
                _logger.Trace("Start searching designated peer");

                client.Bind(Type + "://" + ListeningIP, ListeningPort);
                client.SocketAccept = () =>
                {
                    isAccepted = true;
                    _beacon.Silence();
                };
                _beacon.Publish(TargetAlias + ">" + client.EndPoint);
                res = client.StartListening(5000);
                client.SocketAccept = null;
                _beacon.Silence();
                _beacon.Dispose();

                if (!res)
                {
                    if (isAccepted)
                    {
                        throw new NetMQClientException($"Client not found: Connection is established, but ACK failed. Address:{ListeningIP}:{ListeningPort}");
                    }
                    else
                    {
                        throw new NetMQClientException($"Client not found: Connection is not established. Address: {_beacon.BoundTo}");

                    }
                }

                return client;

            }
            catch (Exception ex)
            {
                client?.Dispose();
                client = null;
                throw ex;
            }
        }

        private void BeaconConfig()
        {
            try
            {
                _beacon = new NetMQBeacon();

                bool emptyIP = string.IsNullOrEmpty(BeaconIP);
                if (BeaconIP == "127.0.0.1")
                {
                    throw new BeaconException($"Please Use empty string when assigning 127.0.0.1 as ip address");
                }
                //chcek if beacon port larger than 65536
                if (BeaconPort >= 65536 || BeaconPort < 0)
                {
                    throw new BeaconException($"Beacon port must between 0 and 65536: {BeaconPort}");
                }

                if (!emptyIP)
                {
                    try
                    {
                        //Check if IP string is convertable
                        IPAddress ip = IPAddress.Parse(BeaconIP);
                    }
                    catch (Exception ex)
                    {
                        throw new BeaconException($"Bad format for IP parameter: {BeaconIP}", ex);
                    }
                }

                //configure beacon
                if (emptyIP)
                {
                    _beacon.ConfigureAllInterfaces(BeaconPort);
                }
                else
                {
                    _beacon.Configure(BeaconPort, BeaconIP);
                }

                //check if beacon is sucessfully bounded to endpoint
                if (string.IsNullOrEmpty(_beacon.BoundTo))
                {
                    _beacon.Dispose();

                    throw new BeaconException($"Beacon binding failed: {BeaconIP}");
                }
            }
            catch (Exception ex)
            {
                _beacon.Dispose();
                throw ex;
            }

        }



    }

    public class NetMQClientException : Exception
    {
        public NetMQClientException()
        {
        }

        public NetMQClientException(string message) : base(message)
        {
        }

        public NetMQClientException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NetMQClientException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
