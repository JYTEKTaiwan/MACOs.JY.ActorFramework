using MACOs.JY.ActorFramework.Clients;
using MACOs.JY.ActorFramework.Core.Commands;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;

namespace MACOs.JY.ActorFramework.Implement.NetMQ
{
    public class NetMQClient : IClient
    {
        private NetMQSocket _socket;
        private string addr;
        public NetMQClient()
        {

        }
        public void Connect(object dest)
        {
            try
            {
                addr = dest.ToString();
                _socket = new DealerSocket();
                _socket.Connect(addr);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        public string Receive()
        {
            try
            {
                return _socket.ReceiveMultipartStrings()[0];

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Disconnect()
        {
            try
            {
                _socket.Disconnect(addr);
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
                if (_socket != null && !_socket.IsDisposed)
                {
                    Disconnect();
                    _socket.Dispose();
                }

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
                _socket.SendFrame(JsonConvert.SerializeObject(cmd));
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

