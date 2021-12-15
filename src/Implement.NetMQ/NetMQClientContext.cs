using MACOs.JY.ActorFramework.Clients;
using NetMQ;
using System;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;

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
    public class NetMQClientContext : IClientContext, IDisposable
    {
        private NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private NetMQBeacon _beacon;
        private bool isDisposed = false;
        private IPAddress ip;
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
        public bool EnableLogging { get; set; } = false;

        /// <summary>
        /// DealerSocket will be listening at this port. Random assigned if value is -1. Default value is -1
        /// </summary>
        public int ListeningPort { get; set; } = -1;
        /// <summary>
        /// DealerSocket will be listening on thie ip address. Defult value is first IPV.4 ip address
        /// </summary>
        public string ListeningIP { get; set; } = Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString();
        public int Timeout { get; set; } = 5000;
        public NetMQClientContext(string targetAlias, int beaconPort = 9999, string beaconIp = "")
        {
            _logger.Trace("Start creating object");
            BeaconPort = beaconPort;
            TargetAlias = targetAlias;
            BeaconIP = beaconIp;
            _logger.Info("Object is created successfully");
        }
        public IClient Search()
        {
            NetMQClient client = null;
            bool isAccepted = false;
            bool res = false;
            try
            {
                _logger.Trace("Start searching the peer");
                if (string.IsNullOrEmpty(TargetAlias))
                {
                    throw new NetMQClientException("Target alias cannot be empty");
                }
                client = new NetMQClient(this.EnableLogging) { TargetName = TargetAlias };
                BeaconConfigure();
                client.Bind(Type + "://" + ListeningIP, ListeningPort);
                var cts = new CancellationTokenSource();

                client.SocketAccept = () =>
                {
                    isAccepted = true;
                    //once client got connected, shutdown the beacon task
                    _beacon.Silence();
                };

                _beacon.Publish($"{TargetAlias}>{client.EndPoint}", TimeSpan.FromSeconds(1));
                res = client.StartListening(Timeout);
                client.SocketAccept = null;
                _beacon.Dispose();

                if (!res)
                {
                    client.UnBind();
                    client?.Dispose();
                    client = null;
                    if (isAccepted)
                    {
                        throw new NetMQClientException($"Client not found: Connection is established, but ACK failed. Address:{ListeningIP}:{ListeningPort}");
                    }
                    else
                    {
                        throw new NetMQClientException($"Client not found: Connection is not established. Address: {ip}:{BeaconPort}");

                    }
                }

                return client;

            }
            catch (Exception ex)
            {
                var ttt = ex.GetType();
                LogError(ex);
                _beacon.Silence();
                _beacon?.Dispose();
                client?.Dispose();
                throw ex;
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~NetMQClientContext()
        { Dispose(false); }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }
            if (disposing)
            {
                _beacon?.Dispose();
                isDisposed = true;
            }
        }
        private void BeaconConfigure()
        {
            try
            {
                _logger.Trace("Start configuring beacon");
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
                        ip = IPAddress.Parse(BeaconIP);
                        _beacon.Configure(BeaconPort, ip.ToString());
                    }
                    catch (Exception ex)
                    {
                        throw new BeaconException($"Bad format for IP parameter: {BeaconIP}", ex);
                    }
                }
                else
                {
                    _beacon.ConfigureAllInterfaces(BeaconPort);
                }



                //check if beacon is sucessfully bounded to endpoint
                if (string.IsNullOrEmpty(_beacon.BoundTo))
                {
                    throw new BeaconException($"Beacon binding failed: {BeaconIP}");
                }
                _logger.Info("Beacon configuration is done");

            }
            catch (Exception ex)
            {
                LogError(ex);
                _beacon?.Silence();
                _beacon?.Dispose();
                throw ex;
            }

        }

        private void LogError(Exception ex)
        {
            _logger.Error($"[{ex.Message}] {ex.StackTrace}");
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
