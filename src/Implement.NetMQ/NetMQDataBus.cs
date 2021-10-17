using MACOs.JY.ActorFramework.Core;
using MACOs.JY.ActorFramework.Core.DataBus;
using MACOs.JY.ActorFramework.Core.Utilities;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
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
    public class NetMQDataBus : IDataBus
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
        public event DataReadyEvent OnDataReady;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config">context information</param>
        public NetMQDataBus(NetMQDataBusContext config)
        {
            AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
            _config = config;
            _logger.Info("Object is created");
        }

        private void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            //this will close all netmq sockets in the background
            NetMQConfig.Cleanup(false);
        }


        /// <summary>
        /// Start
        /// </summary>
        public void Start()
        {
            _poller.RunAsync();
            _logger.Info("Poller starts");
        }

        /// <summary>
        /// Stop
        /// </summary>
        public void Stop()
        {
            _poller.Stop();
            _logger.Info("Poller stops");
        }
        /// <summary>
        /// Configure
        /// </summary>
        public void Configure()
        {
            try
            {
                _logger.Trace("Begin configuration");

                RouterSocketConfig();

                BeaconConfig();

                _poller = new NetMQPoller();
                _poller.Add(_serverSocket);
                _poller.Add(_beacon);

                _monitor = new NetMQMonitor(_serverSocket, $"inproc://{_config.AliasName}", SocketEvents.All);
                _monitor.AttachToPoller(_poller);
                _monitor.EventReceived += Socket_Events;

                BeaconStart();

                Name = _config.AliasName;
                _logger.Info("Configuration is done");

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public static void CleanUp(bool block=true)
        {
            NetMQConfig.Cleanup(block);
        }
        private void RouterSocketConfig()
        {
            _serverSocket = new RouterSocket();

            //check ip address (containing non-numbers or other invalid input
            if (!_config.LocalIP.Contains("://"))
            {
                throw new ArgumentException($"Invalid listener address:  {_config.LocalIP}");
            }

            //Check if ip string is valid
            var str = _config.LocalIP.Split(new char[] { '/', '/' })[2];
            try
            {
                IPAddress ip = IPAddress.Parse(str);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Invalid listener address", ex);
            }

            //Chcek the port and configure
            try
            {
                if (_config.Port>=65536)
                {
                    throw new ArgumentException("Invalid listener port, must smaller than 65536");
                }
                else if (_config.Port<0)
                {
                    int port = _serverSocket.BindRandomPort(_config.LocalIP);
                    _logger.Debug($"Socket binds to {_config.LocalIP}:{port}");

                }
                else
                {
                    _serverSocket.Bind($"{_config.LocalIP}:{_config.Port}");
                    _logger.Debug($"Socket binds to {_config.LocalIP}:{_config.Port}");
                }
            }
            catch (Exception ex)
            {            
                throw new ArgumentException("Invalid listener address",ex);
            }

            _serverSocket.ReceiveReady += _serverSocket_ReceiveReady;

        }

        private void BeaconConfig()
        {
            _beacon = new NetMQBeacon();

            //chcek if beacon port larger than 65536
            if (_config.BeaconPort >= 65536 || _config.BeaconPort<0)
            {
                _beacon.Dispose();
                throw new ArgumentException("Beacon port must between 0 and 65536");
            }


            //configure beacon
            if (string.IsNullOrEmpty(_config.BeaconIPAddress))
            {
                _beacon.ConfigureAllInterfaces(_config.BeaconPort);
            }
            else
            {
                try
                {
                    //Check if IP string is convertable
                    IPAddress ip = IPAddress.Parse(_config.BeaconIPAddress);
                }
                catch (Exception ex)
                {
                    _beacon.Dispose();

                    throw new ArgumentException("Invalid Beacon address", ex);
                }
                _beacon.Configure(_config.BeaconPort, _config.BeaconIPAddress);
            }


            //check if beacon is sucessfully bounded to endpoint
            if (string.IsNullOrEmpty(_beacon.BoundTo))
            {
                _beacon.Dispose();

                throw new ArgumentException("Invalid Beacon address");
            }
        }

        private void BeaconStart()
        {
            //Silent the beacon or not
            try
            {
                if (!_config.IsSilent)
                {
                    _beacon.Publish(_config.AliasName + "=" + _serverSocket.Options.LastEndpoint, TimeSpan.FromSeconds(1));
                    _logger.Debug($"Beacon broacasts at port {_config.BeaconPort}");

                }

            }
            catch (Exception ex)
            {
                _beacon.Dispose();
                throw ex;
            }

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
                RoutingKey id;
                string ans = "";
                try
                {
                    _logger.Trace("New data is coming");
                    id = e.Socket.ReceiveRoutingKey();
                    var content = e.Socket.ReceiveMultipartStrings();

                    _logger.Debug($"Content: {content[0]}");
                    ans = OnDataReady?.Invoke(sender, content[0]);
                    _logger.Debug("OnDataReady is fired");
                    _logger.Debug($"Return data: {ans}");

                    _serverSocket.SendMoreFrame(id);
                    _serverSocket.SendFrame(ans);
                    _logger.Info("Response sent");

                }
                catch (Exception ex)
                {
                    _logger.Error($"Invoke error: {ex.Message}");
                    _serverSocket.SendMoreFrame(id);
                    _serverSocket.SendFrame($"[Error]: {ex.Message}");
                }

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
                    //_serverSocket.Unbind(_serverSocket.Options.LastEndpoint);
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

            }

        }

        public string Query(string jsonContent)
        {
            return OnDataReady?.Invoke(null, jsonContent);
        }

        ~NetMQDataBus()
        {

        }

    }

}
