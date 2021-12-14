using MACOs.JY.ActorFramework.Core.Commands;
using MACOs.JY.ActorFramework.Core.DataBus;
using MACOs.JY.ActorFramework.Core.Utilities;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using Newtonsoft.Json.Linq;
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

        // Two necessary socket in the object
        // 1. RouterSocket - handler different DealerSocket and send response back to each of them
        // 2. BeaconMessage - Dicovery in the local domain(UDP)
        private NetMQSocket _serverSocket;
        private NetMQBeacon _beacon;
        // Multi-Thread support for NetMQ protocol
        private readonly NetMQDataBusContext _config;
        private CancellationTokenSource cts_beaconListening;
        private CancellationTokenSource cts_readMessage;
        private NLog.Logger _logger;

        public bool IsDisposed { get; set; } = false;
        public string Name { get; set; }
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMilliseconds(1000);
        public event DataReadyEvent OnDataReady;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config">context information</param>
        public NetMQDataBus(NetMQDataBusContext config)
        {
            AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
            _config = config;
            _logger= NLog.LogManager.GetLogger($"{this.GetType().Name}_{_config.AliasName}");
            _logger.Info("Object is created");
            
        }

        #region Public Methods
        public static void CleanUp(bool block = true)
        {
            NetMQConfig.Cleanup(block);
        }

        /// <summary>
        /// Start
        /// </summary>
        public void Start()
        {
            _logger.Trace("DataBus starts");
            cts_beaconListening = new CancellationTokenSource();
            cts_readMessage = new CancellationTokenSource();

            StartListeningBeaconAsync();
            ReadMessageAsync();
            _logger.Info("DataBus start running");
        }

        /// <summary>
        /// Stop
        /// </summary>
        public void Stop()
        {
            _logger.Trace("DataBus stops");
            cts_beaconListening?.Cancel();
            cts_readMessage?.Cancel();
            _logger.Info("DataBus stop running");
        }
        /// <summary>
        /// Configure
        /// </summary>
        public void Configure()
        {
            try
            {
                _logger.Trace("Start configuring databus object");
                if (string.IsNullOrEmpty(_config.AliasName))
                {
                    _logger.Debug("Alias is null or empty, overwritten by has code");
                    Name = this.GetHashCode().ToString();
                }
                else
                {
                    Name = _config.AliasName;
                }

                BeaconConfigure();

                RouterSocketConfig();

                _logger.Info("Databus configuration is done");

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        /// <summary>
        /// Stop and release instance
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                return;
            }
            if (disposing)
            {
                _logger.Trace("Begin disposing the object");
                _logger.Debug("Stop and remove the socket and beacon");

                Stop();

                Thread.Sleep(Timeout);
                _beacon?.Dispose();
                //_serverSocket?.Disconnect(_serverSocket.Options.LastEndpoint);
                _serverSocket?.Dispose();
                _logger.Info("Socket and beacon are removed and disposed");

                IsDisposed = true;
                _logger.Info("Object is successfully disposed");


            }
        }

        #endregion

        #region Private Methods
        private void RouterSocketConfig()
        {
            try
            {
                _logger.Trace("Start configuring socket");
                _serverSocket = new RouterSocket();
                //disable reconnect function
                _serverSocket.Options.ReconnectInterval = TimeSpan.FromMilliseconds(-1);
                _logger.Info("Socket configuration is done");
            }
            catch (Exception ex)
            {
                _serverSocket.Dispose();
                throw new ArgumentException($"Socket created failed", ex);
            }

        }
        private void BeaconConfigure()
        {
            try
            {
                _logger.Trace("Start configuring beacon");

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
                    _beacon.Configure(_config.BeaconPort,_config.BeaconIP);
                }
                
                _logger.Info("Beacon configuration is done");
            }
            catch (Exception ex)
            {
                LogError(ex);
                _beacon?.Dispose();
                throw ex;
            }

        }
        private void StartListeningBeaconAsync()
        {
            Task.Run(() =>
            {
                _logger.Trace($"Start waiting for the beacon named: {Name}");
                _beacon.Subscribe(Name);
                _logger.Debug($"Beacon start listening at port {_config.BeaconPort}");
                while (!cts_beaconListening.IsCancellationRequested)
                {
                    BeaconMessage msg ;
                    if (_beacon.TryReceive(Timeout,out msg))
                    {
                        if (!msg.String.Contains("DUMMY"))
                        {
                            //_beacon.Unsubscribe();
                            _logger.Debug($"Beacon received from {msg.PeerAddress}");
                            //Check 
                            var info = msg.String.Split('>')[1];
                            _logger.Debug($"Client info was extracted: {info}");
                            _serverSocket.Connect(info);
                            _logger.Debug($"Client is connected {info}");
                        }
                        
                        
                    }
                }
            });

            Task.Run(() => 
            {
                //20211214 NetMQBeacon will throw socket exception(1040) if no other beacon is existed after object is created and configured
                //Use dummy beacon to "activate" the instance
                using (var dummyBeacon = new NetMQBeacon())
                {
                    dummyBeacon.ConfigureAllInterfaces(_config.BeaconPort);
                    dummyBeacon.Publish($"{_config.AliasName}>DUMMY");
                    dummyBeacon.Silence();
                    dummyBeacon.Dispose();
                }
            });

        }
        private void ReadMessageAsync()
        {
            Task.Run(() =>
            {
                RoutingKey id = new RoutingKey();
                string ans = "";

                while (!cts_readMessage.IsCancellationRequested)
                {
                    if (_serverSocket.TryReceiveRoutingKey(Timeout, ref id))
                    {
                        var content = _serverSocket.ReceiveMultipartStrings()[0];
                        if (content == GlobalCommand.Accepted)
                        {
                            _logger.Trace($"[{id}] \"{GlobalCommand.Accepted}\"");
                            _logger.Debug($"\"{GlobalCommand.Accepted}\" is received");
                            _logger.Info($"\"{GlobalCommand.Accepted}\" is received");

                            _serverSocket.SendMoreFrame(id);
                            _serverSocket.SendFrame(GlobalCommand.Connected);

                            _logger.Trace($"\"{GlobalCommand.Connected}\" is sent back to {id}");
                            _logger.Debug($"\"{GlobalCommand.Connected}\" is sent back");
                            _logger.Info($"\"{GlobalCommand.Connected}\" is sent back");

                            //_beacon.Subscribe(_config.AliasName);

                        }
                        else
                        {
                            _logger.Trace($"{content}");
                            _logger.Info("New command is received");

                            try
                            {
                                ans = OnDataReady?.Invoke(this, content);
                                _logger.Info("Message has been processed");
                                _logger.Debug($"Response after executiion: {ans}");
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
                }
            });
            
        }
        private void LogError(Exception ex)
        {
            _logger.Error($"[{ex.Message}] {ex.StackTrace}");
        }

        #endregion

        #region Internal Event Handler
        private void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            _logger.Trace("Domain Unload");
            Dispose();            
            //this will close all netmq sockets in the background
            NetMQConfig.Cleanup(false);
        }


        #endregion

        ~NetMQDataBus()
        {
            Dispose(false);
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
