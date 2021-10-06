using MACOs.Services.Utilities;
using Microsoft.Extensions.Options;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using System;
using System.Threading.Tasks;

namespace MACOs.JY.ActorFramework.Implement.NetMQ
{
    /// <summary>
    /// NetMQDataBus consists of RouterSocket and Beacon from NetMQ library. 
    /// RouterSocket deals with the incoming Dealersocket and processing the messages
    /// from dealersockets
    /// Beacon is used to broadcast in the assigned address, so everyone could find 
    /// instance through NetMQ library.
    /// </summary>
    internal class NetMQDataBus : IDataBus
    {
        private NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        // Two necessary socket in the object
        // 1. RouterSocket - handler different DealerSocket and send response back to each of them
        // 2. BeaconMessage - Dicovery in the local domain(UDP)
        private NetMQSocket _serverSocket;
        private NetMQBeacon _beacon;
        // Multi-Thread support for NetMQ protocol
        private NetMQPoller _poller;
        private NetMQMonitor _monitor;
        private readonly NetMQDataBusContext _config;

        public bool IsDisposed { get; set; } = false;
        public string Name { get; set; }
        public DealerSocket InternalClient { get; set; }

        public event DataReadyEvent OnDataReady;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config">context information</param>
        public NetMQDataBus(NetMQDataBusContext config)
        {
            _config = config;
            _logger.Info("Object is created");

        }

        /// <summary>
        /// Start
        /// </summary>
        public void Start()
        {
            _poller.RunAsync();            
            _logger.Info("Poller starts");
            InternalClient.Connect(_serverSocket.Options.LastEndpoint);            
        }
        /// <summary>
        /// Stop
        /// </summary>
        public void Stop()
        {
            InternalClient.Disconnect(_serverSocket.Options.LastEndpoint);
            _poller.StopAsync();
            _logger.Info("Poller stops");
        }
        /// <summary>
        /// Configure
        /// </summary>
        public void Configure()
        {
            _logger.Trace("Begin configuration");
            _serverSocket = new RouterSocket();
            InternalClient = new DealerSocket();

            if (_config.Port > 0)
            {
                _serverSocket.Bind($"{_config.LocalIP}:{_config.Port}");
                _logger.Debug($"Socket binds to {_config.LocalIP}:{_config.Port}");
            }
            else
            {
                int port = _serverSocket.BindRandomPort(_config.LocalIP);
                _logger.Debug($"Socket binds to {_config.LocalIP}:{port}");
            }
            _poller = new NetMQPoller();
            _serverSocket.ReceiveReady += _serverSocket_ReceiveReady;

            _beacon = new NetMQBeacon();
            string ip = string.IsNullOrEmpty(_config.BeaconIPAddress) ? "" : _config.BeaconIPAddress;
            _beacon.Configure(_config.BeaconPort, ip);
            
            if (!_config.IsSilent)
            {
                _beacon.Publish(_config.AliasName + "=" + _serverSocket.Options.LastEndpoint, TimeSpan.FromSeconds(1));
            }
            _logger.Debug($"Beacon broacasts at port {_config.BeaconPort}");

            _poller.Add(_serverSocket);
            _poller.Add(_beacon);

            _monitor = new NetMQMonitor(_serverSocket, $"inproc://{_config.AliasName}", SocketEvents.All);
            _monitor.AttachToPoller(_poller);
            _monitor.EventReceived += Socket_Events;



            Name = _config.AliasName;
            _logger.Info("Configuration is done");
        }


        /// <summary>
        /// TCP socket event handler
        /// </summary>
        private void Socket_Events(object sender, NetMQMonitorEventArgs e)
        {
            var socket = (e as NetMQMonitorSocketEventArgs).Socket;
            if (e.SocketEvent != SocketEvents.Closed)
            {
                _logger.Debug($"Socket event triggered : [{e.SocketEvent}] {socket.RemoteEndPoint}");
            }
            else
            {
                _logger.Debug($"Socket event triggered : [{e.SocketEvent}]");

            }


        }

        /// <summary>
        /// New data is received from routersocket
        /// </summary>
        private void _serverSocket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {

            //Frame 0 - RoutingKey (unique ID from DealerSocket)
            //Frame 1 - Data frame (command class name)
            //Frame 2 - Data frame (parameters)
            if (e.Socket.HasIn)
            {
                _logger.Trace("New data is coming");
                var id = e.Socket.ReceiveRoutingKey();
                var content = e.Socket.ReceiveMultipartStrings();

                _logger.Debug($"Content: {content[0]}");
                var ans = OnDataReady?.Invoke(sender, content[0]);
                _logger.Debug("OnDataReady is fired");
                _logger.Debug($"Return data: {ans}");

                _serverSocket.SendRoutingKeys(id).SendFrame(ans);

                _logger.Info("Response sent");

            }
        }
        /// <summary>
        /// Stop and release instance
        /// </summary>
        public void Kill()
        {
            _logger.Trace("Begin disposing the object");

            if (!IsDisposed)
            {
                if (_poller.IsRunning)
                {
                    _serverSocket.ReceiveReady -= _serverSocket_ReceiveReady;
                    _serverSocket.Unbind(_serverSocket.Options.LastEndpoint);
                    _logger.Debug("Stop socket");

                    if (!_config.IsSilent)
                    {
                        _beacon.Silence();
                        _logger.Debug("Stop beacon");
                    }

                    _poller.RemoveAndDispose(_serverSocket);
                    _poller.RemoveAndDispose(_beacon);
                    _logger.Debug("Clear sockets in poller");

                    Stop();
                }
                _monitor.Dispose();
                _poller.Dispose();
                _logger.Debug("Dispose poller");
                IsDisposed = true;
                _logger.Info("Object is successfully disposed");
                InternalClient.Dispose();

            }

        }

        public string Query(string jsonContent)
        {
            InternalClient.SendFrame(jsonContent);
            return InternalClient.ReceiveMultipartStrings()[1];
        }

        ~NetMQDataBus()
        {

        }

    }

}
