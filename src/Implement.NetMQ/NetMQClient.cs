using MACOs.JY.ActorFramework.Clients;
using MACOs.JY.ActorFramework.Core.Commands;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MACOs.JY.ActorFramework.Implement.NetMQ
{
    public class NetMQClient : IClient
    {
        private NetMQSocket _socket;
        private bool isDisposed = false;
        public delegate void SocketAccepted();

        public SocketAccepted SocketAccept;
        public string EndPoint { get; set; } = "";
        public NetMQClient()
        {
            AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
            _socket = new DealerSocket();
        }

        private void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            NetMQConfig.Cleanup(false);
        }

        public void Bind(string ip, int port)
        {
            if (port <= 0)
            {
                _socket.BindRandomPort(ip);
            }
            else
            {
                _socket.Bind(ip + ":" + port);
            }
            EndPoint = _socket.Options.LastEndpoint;
        }

        public void UnBind()
        {
            _socket.Unbind(_socket.Options.LastEndpoint);
        }

        public bool StartListening(int timeoutMilliSecond)
        {
            if (_socket.TrySendFrame(TimeSpan.FromMilliseconds(timeoutMilliSecond), GlobalCommand.Accepted)            )
            {
                //if pass, means client has been connected by router socket

                //Invoke delegate method
                SocketAccept.Invoke();

                //Try Receive frame from router socket (eg: ACK)
                string ans = "";
                var pass = _socket.TryReceiveFrameString(TimeSpan.FromMilliseconds(timeoutMilliSecond), out ans);
                return pass && ans == GlobalCommand.Connected;
            }
            else
            {
                return false;
            }
        }
        public string Receive()
        {
            try
            {
                lock (this)
                {
                    return _socket.ReceiveMultipartStrings()[0];
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Dispose()
        {
            try
            {
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


            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Send(ICommand cmd)
        {
            try
            {
                lock (this)
                {
                    _socket.SendFrame(JsonConvert.SerializeObject(cmd));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public string Query(ICommand cmd)
        {
            try
            {
                lock (this)
                {
                    Send(cmd);
                    //return string begins
                    var res = Receive();
                    if (res.Contains("[Error]:"))
                    {
                        throw new Exception(res);
                    }
                    else
                    {
                        return res;
                    }

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


        ~NetMQClient()
        {
            Dispose();
        }

    }

}

