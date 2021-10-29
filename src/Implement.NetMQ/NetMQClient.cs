using MACOs.JY.ActorFramework.Clients;
using MACOs.JY.ActorFramework.Core.Commands;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MACOs.JY.ActorFramework.Implement.NetMQ
{
    public class NetMQClient : IClient
    {
        private Logger _logger = LogManager.GetCurrentClassLogger();
        private NetMQSocket _socket;
        private bool isDisposed = false;
        private NetMQMonitor _monitor;
        private bool isConnected = false;
        public delegate void SocketAccepted();

        public SocketAccepted SocketAccept;
        public string EndPoint { get; set; } = "";
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMilliseconds(5000);
        public NetMQClient()
        {
            AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
            _socket = new DealerSocket();
            _monitor = new NetMQMonitor(_socket, "inproc://" + _socket.GetHashCode(), SocketEvents.All);
            _monitor.EventReceived += SocketEvent;
            _monitor.StartAsync();
        }

        private void SocketEvent(object sender, NetMQMonitorEventArgs e)
        {
            _logger.Trace(e.SocketEvent + "@" + e.Address);
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
                    isConnected = true;
                    break;
                case SocketEvents.AcceptFailed:
                    break;
                case SocketEvents.Closed:
                    break;
                case SocketEvents.CloseFailed:
                    break;
                case SocketEvents.Disconnected:
                    isConnected = false;
                    break;
                case SocketEvents.All:
                    break;
                default:
                    break;
            }
        }

        private void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            _logger.Trace("Domain Unload");
            NetMQConfig.Cleanup(false);
        }

        public void Bind(string ip, int port)
        {
            try
            {
                _logger.Trace("Start binding the address");
                if (port <= 0)
                {
                    var num = _socket.BindRandomPort(ip);
                    _logger.Debug($"Binds at random port {num}");

                }
                else
                {
                    _socket.Bind(ip + ":" + port);
                    _logger.Debug($"Binds at port {port}");
                }
                EndPoint = _socket.Options.LastEndpoint;
                _logger.Info("Binding completed");

            }
            catch (Exception ex)
            {
                LogError(ex);
                throw ex;
            }
        }

        public void UnBind()
        {
            try
            {
                _logger.Trace("Start unbinding the address");
                _socket.Unbind(_socket.Options.LastEndpoint);
                _logger.Info("Unbinding completed");

            }
            catch (Exception ex)
            {
                LogError(ex);
                throw ex;
            }

        }

        public bool StartListening(int timeoutMilliSecond)
        {
            try
            {
                _logger.Trace("Start listening for connection");
                bool passed = false;
                if (_socket.TrySendFrame(TimeSpan.FromMilliseconds(timeoutMilliSecond), GlobalCommand.Accepted))
                {
                    _logger.Debug($"Message \"ACCEPTED\" has been sent successfully");
                    //if pass, means client has been connected by router socket

                    _logger.Trace("Fire \"SocketAccept\" event");
                    //Invoke delegate method
                    SocketAccept.Invoke();

                    //Try Receive frame from router socket (eg: ACK)
                    string ans = "";
                    var success = _socket.TryReceiveFrameString(TimeSpan.FromMilliseconds(timeoutMilliSecond), out ans);
                    _logger.Debug($"Message \"CONNECTED\" has been received successfully");

                    passed = success && ans == GlobalCommand.Connected;
                }
                else
                {
                    _logger.Debug($"Message \"ACCEPTED\" has not been sent");
                    passed = false;
                }

                _logger.Info($"Client search " + (passed ? "pass" : "fail"));
                return passed;

            }
            catch (Exception ex)
            {
                LogError(ex);
                throw ex;
            }
        }
        public string Receive()
        {
            try
            {
                _logger.Trace("Start Receiving message");

                lock (this)
                {
                    var msg = _socket.ReceiveMultipartStrings();                    
                    if (!isConnected)
                    {
                        throw new NetMQClientException("Not connected");
                    }

                    for (int i = 0; i < msg.Count; i++)
                    {
                        _logger.Debug($"Frame {i}: {msg[i]}");
                    }
                    var returnVal = msg[0];
                    _logger.Info("Message has been received");
                    return returnVal;
                }

            }
            catch (Exception ex)
            {
                LogError(ex);
                throw ex;
            }
        }

        public void Dispose()
        {
            try
            {
                _logger.Trace("Start disposing client object");

                _logger.Debug("Stops the monitor task");
                _monitor.Stop();

                _logger.Debug("Dispose the socket");
                if (!isDisposed)
                {
                    if (_socket != null && !_socket.IsDisposed)
                    {
                        try
                        {
                            UnBind();
                        }
                        catch (Exception) { }

                        //_socket?.Close();
                        _socket?.Dispose();
                        isDisposed = true;
                    }

                }
                GC.Collect();
                _logger.Info("client object disposed");

            }
            catch (Exception ex)
            {
                LogError(ex);
                throw ex;
            }
        }

        public void Send(ICommand cmd)
        {
            try
            {
                _logger.Trace("Start sending message");

                lock (this)
                {
                    var msg = JsonConvert.SerializeObject(cmd);
                    JToken token = JToken.Parse(msg);
                    _logger.Debug($"[{cmd.Name}]{token["Parameter"].ToString().Replace(Environment.NewLine,"")}");
                    
                    if (!isConnected)
                    {
                        throw new NetMQClientException("Not connected");
                    }

                    var pass =_socket.TrySendFrame(Timeout, JsonConvert.SerializeObject(cmd));
                    _logger.Info("Message has been sent");
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                throw ex;
            }

        }

        public string Query(ICommand cmd)
        {
            try
            {
                _logger.Trace("Start querying message");

                lock (this)
                {
                    Send(cmd);
                    //return string begins
                    var res = Receive();
                    if (res.Contains("[Error]:"))
                    {
                        _logger.Error(res);
                        throw new Exception(res);
                    }
                    else
                    {                        
                        _logger.Info("Query completed");
                        return res;
                    }

                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                throw ex;
            }

        }


        ~NetMQClient()
        {
            Dispose();
        }

        private void LogError(Exception ex)
        {
            _logger.Error($"[{ex.Message}] {ex.StackTrace}");
        }
    }

}

