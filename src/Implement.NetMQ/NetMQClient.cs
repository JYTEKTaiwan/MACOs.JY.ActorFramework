using MACOs.JY.ActorFramework.Clients;
using MACOs.JY.ActorFramework.Core.Commands;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;

namespace MACOs.JY.ActorFramework.Implement.NetMQ
{
    public class NetMQClient : IClient, IDisposable
    {
        private Logger _logger;
        private NetMQSocket _socket;
        private bool isDisposed = false;
        public delegate void SocketAccepted();

        public SocketAccepted SocketAccept;
        public string EndPoint { get; set; } = "";
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMilliseconds(5000);
        public string TargetName { get; set; }
        public NetMQClient(bool enableLogging = true)
        {
            AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
            _socket = new DealerSocket();
            _logger = enableLogging ? LogManager.GetCurrentClassLogger() : LogManager.CreateNullLogger();
            _logger.Info($"Client {this.GetHashCode()} is created");
        }

        private void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            Dispose();
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

        public bool StartListening(int timeoutMilliSecond = 5000)
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
        public string Receive(int timeoutMilliSecond = -1)
        {
            try
            {
                _logger.Trace("Start Receiving message");

                lock (this)
                {
                    List<string> msg = new List<string>();
                    if (timeoutMilliSecond > 0)
                    {
                        var pass = _socket.TryReceiveMultipartStrings(TimeSpan.FromMilliseconds(timeoutMilliSecond), ref msg, 4);
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
                    else
                    {
                        msg = _socket.ReceiveMultipartStrings();
                        for (int i = 0; i < msg.Count; i++)
                        {
                            _logger.Debug($"Frame {i}: {msg[i]}");
                        }
                        var returnVal = msg[0];
                        _logger.Info("Message has been received");
                        return returnVal;

                    }
                }

            }
            catch (Exception ex)
            {
                LogError(ex);
                throw ex;
            }
        }
        public void Send(ICommand cmd, int timeoutMilliSecond = -1)
        {
            try
            {
                _logger.Trace("Start sending message");

                lock (this)
                {
                    _logger.Debug((cmd as CommandBase).GetSimplifiedString());
                    if (timeoutMilliSecond > 0)
                    {
                        var pass = _socket.TrySendFrame(TimeSpan.FromMilliseconds(timeoutMilliSecond), JsonConvert.SerializeObject(cmd));
                        if (pass)
                        {
                            _logger.Info("Message has been sent");
                        }
                        else
                        {
                            throw new NetMQClientException($"Sending message timeout");
                        }
                    }
                    else
                    {
                        _socket.SendFrame(JsonConvert.SerializeObject(cmd));
                        _logger.Info("Message has been sent");
                    }

                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                throw ex;
            }

        }
        public string Query(ICommand cmd, int timeoutMilliSecond = -1)
        {
            try
            {
                _logger.Trace("Start querying message");

                lock (this)
                {
                    Send(cmd, timeoutMilliSecond);
                    //return string begins
                    var res = Receive(timeoutMilliSecond);
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

