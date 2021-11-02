using MACOs.JY.ActorFramework.Clients;
using MACOs.JY.ActorFramework.Core.Commands;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MACOs.JY.ActorFramework.Implement.NetMQ
{
    public class NetMQClient : IClient, IDisposable
    {
        private Logger _logger = LogManager.GetCurrentClassLogger();
        private NetMQSocket _socket;
        private bool isDisposed = false;
        public delegate void SocketAccepted();

        public SocketAccepted SocketAccept;
        public string EndPoint { get; set; } = "";
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMilliseconds(5000);
        public NetMQClient()
        {
            AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
            _socket = new DealerSocket();
            _logger.Info($"Client {this.GetHashCode()} is created");
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
                _socket.Unbind(EndPoint);
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

                    passed = success && ans == GlobalCommand.Connected;
                    if (passed)
                    {
                        _logger.Debug($"Message \"CONNECTED\" has been received successfully");
                    }
                    else
                    {
                        _logger.Debug($"Message \"CONNECTED\" is not received.");
                        throw new NetMQClientException("Connection is established, but ACK is not finished yet");
                    }

                }
                else
                {
                    _logger.Debug($"Message \"ACCEPTED\" has not been sent");
                    throw new NetMQClientException("Connection is not established yet");
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
                    List<string> msg = new List<string>();

                    var pass=_socket.TryReceiveMultipartStrings(Timeout, ref msg,4);
                    if (pass)
                    {
                        for (int i = 0; i < msg.Count; i++)
                        {
                            _logger.Debug($"Frame {i}: {msg[i]}");
                        }
                        var returnVal = msg[0];
                        _logger.Info("Message has been received");
                        return returnVal;
                    }
                    else
                    {
                        throw new NetMQClientException("Receiving message timeout");
                    }
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
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            catch (Exception ex)
            {
                LogError(ex);
                throw ex;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }
            if (disposing)

            {
                _logger.Trace("Start disposing client object");

                if (_socket != null && !_socket.IsDisposed)
                {
                    try
                    {
                        UnBind();
                    }
                    catch (Exception) { }


                    //_socket?.Close();
                    _socket?.Dispose();
                    _socket = null;
                    _logger.Debug("Dispose the socket");

                }

                isDisposed = true;


                _logger.Info("client object disposed");

            }
        }
        public void Send(ICommand cmd)
        {
            try
            {
                _logger.Trace("Start sending message");

                lock (this)
                {
                    _logger.Debug((cmd as CommandBase).String);

                    var pass = _socket.TrySendFrame(Timeout, JsonConvert.SerializeObject(cmd));
                    if (pass)
                    {
                        _logger.Info("Message has been sent");
                    }
                    else
                    {
                        throw new NetMQClientException($"Sending message timeout");
                    }

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
            Dispose(false);
        }

        private void LogError(Exception ex)
        {
            _logger.Error($"[{ex.Message}] {ex.StackTrace}");
        }
    }

}

