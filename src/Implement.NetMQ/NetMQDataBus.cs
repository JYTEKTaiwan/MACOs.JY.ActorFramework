using MACOs.JY.ActorFramework.Core.Commands;
using MACOs.JY.ActorFramework.Core.DataBus;
using MACOs.JY.ActorFramework.Core.Utilities;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using System;
using System.Net;
using System.Runtime.Serialization;
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
        private string connectedString = "";
        private CancellationTokenSource cts;

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
            cts = new CancellationTokenSource();
            WaitForConnection();

            _poller.RunAsync();
            _logger.Info("Poller starts");
        }

        /// <summary>
        /// Stop
        /// </summary>
        public void Stop()
        {
            cts.Cancel();
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
                if (string.IsNullOrEmpty(_config.AliasName))
                {
                    Name = this.GetHashCode().ToString();
                }
                else
                {
                    Name = _config.AliasName;
                }

                RouterSocketConfig();
                BeaconConfig();

                _poller = new NetMQPoller();
                _poller.Add(_serverSocket);
                _poller.Add(_beacon);

                _monitor = new NetMQMonitor(_serverSocket, $"inproc://{_serverSocket.GetHashCode()}", SocketEvents.All);
                _monitor.AttachToPoller(_poller);
                _monitor.EventReceived += Socket_Events;


                _serverSocket.ReceiveReady += _serverSocket_ReceiveReady;


                _logger.Info("Configuration is done");

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        public static void CleanUp(bool block = true)
        {
            NetMQConfig.Cleanup(block);
        }
        private void RouterSocketConfig()
        {
            try
            {
                _serverSocket = new RouterSocket();
                
            }
            catch (Exception ex)
            {
                _serverSocket.Dispose();
                throw new ArgumentException($"Socket created failed", ex);
            }

        }

        private void BeaconConfig()
        {
            try
            {
                _beacon = new NetMQBeacon();

                bool emptyIP = string.IsNullOrEmpty(_config.BeaconIP);
                if (_config.BeaconIP == "127.0.0.1")
                {
                    throw new BeaconException($"Please Use empty string when assigning 127.0.0.1 as ip address");
                }
                //chcek if beacon port larger than 65536
                if (_config.BeaconPort >= 65536 || _config.BeaconPort < 0)
                {
                    throw new BeaconException($"Beacon port must between 0 and 65536: {_config.BeaconPort}");
                }

                if (!emptyIP)
                {
                    try
                    {
                        //Check if IP string is convertable
                        IPAddress ip = IPAddress.Parse(_config.BeaconIP);
                    }
                    catch (Exception ex)
                    {
                        throw new BeaconException($"Bad format for IP parameter: {_config.BeaconIP}", ex);
                    }
                }

                //configure beacon
                if (emptyIP)
                {
                    _beacon.ConfigureAllInterfaces(_config.BeaconPort);
                }
                else
                {
                    _beacon.Configure(_config.BeaconPort, _config.BeaconIP);
                }

                //check if beacon is sucessfully bounded to endpoint
                if (string.IsNullOrEmpty(_beacon.BoundTo))
                {
                    _beacon.Dispose();

                    throw new BeaconException($"Beacon binding failed: {_config.BeaconIP}");
                }
            }
            catch (Exception ex)
            {
                _beacon.Dispose();
                throw ex;
            }

        }
        private void WaitForConnection()
        {
            Task.Run(() =>
            {
                _beacon.Subscribe(Name);
                _logger.Debug($"Beacon listens at port {_config.BeaconPort}");

                while (!cts.IsCancellationRequested)
                {
                    BeaconMessage msg;
                    if (_beacon.TryReceive(TimeSpan.FromMilliseconds(100), out msg))
                    {
                        _beacon.Unsubscribe();
                        _logger.Debug($"Beacon Received from {msg.PeerAddress}");
                        //Check 
                        var info = msg.String.Split('>')[1];
                        _serverSocket.Connect(info);
                        _beacon.Subscribe(_config.AliasName);

                    }
                }
            });

        }
       
        /// <summary>
        /// TCP socket event handler
        /// </summary>
        private void Socket_Events(object sender, NetMQMonitorEventArgs e)
        {
            switch (e.SocketEvent)
            {
                case SocketEvents.Connected:

                    break;
                case SocketEvents.ConnectDelayed:
                    break;
                case SocketEvents.ConnectRetried:
                    break;
                case SocketEvents.Listening:
                    break;
                case SocketEvents.BindFailed:
                    break;
                case SocketEvents.Accepted:
                    break;
                case SocketEvents.AcceptFailed:
                    break;
                case SocketEvents.Closed:
                    break;
                case SocketEvents.CloseFailed:
                    break;
                case SocketEvents.Disconnected:
                    break;
                case SocketEvents.All:
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// New data is received from routersocket
        /// </summary>
        private void _serverSocket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {            
            if (e.IsReadyToReceive)
            {
                RoutingKey id;
                string ans = "";
                try
                {
                    _logger.Trace("New data is coming");
                    id = e.Socket.ReceiveRoutingKey();
                    var content = e.Socket.ReceiveMultipartStrings();
                    if (content[0]== GlobalCommand.Accepted)
                    {
                        _serverSocket.SendMoreFrame(id);
                        _serverSocket.SendFrame(GlobalCommand.Connected);
                        _beacon.Subscribe(_config.AliasName);

                    }
                    else
                    {
                        _logger.Debug($"Content: {content[0]}");
                        ans = OnDataReady?.Invoke(sender, content[0]);
                        _logger.Debug("OnDataReady is fired");
                        _logger.Debug($"Return data: {ans}");

                        _serverSocket.SendMoreFrame(id);
                        _serverSocket.SendFrame(ans);
                        _logger.Info("Response sent");

                    }

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
                if (_poller != null)
                {
                    if (_poller.IsRunning)
                    {
                        cts.Cancel();
                        Thread.Sleep(100);

                        _monitor.EventReceived -= Socket_Events;
                        _serverSocket.ReceiveReady -= _serverSocket_ReceiveReady;

                        Stop();
                        _poller.RemoveAndDispose(_serverSocket);
                        _poller.RemoveAndDispose(_beacon);
                        _logger.Debug("Clear sockets in poller");

                    }
                    _monitor.DetachFromPoller();
                    _monitor.Dispose();
                    _poller.Dispose();
                    _logger.Debug("Dispose poller");
                    IsDisposed = true;
                    _logger.Info("Object is successfully disposed");
                    GC.Collect();

                }

            }

        }

        ~NetMQDataBus()
        {
        }

    }
    public class BeaconException : Exception
    {
        public BeaconException()
        {
        }

        public BeaconException(string message) : base(message)
        {
        }

        public BeaconException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected BeaconException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

}
