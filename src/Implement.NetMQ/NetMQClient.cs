﻿using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System.Threading.Tasks;
using MACOs.JY.ActorFramework.Clients;
using MACOs.JY.ActorFramework.Core.Commands;
using System;
using Newtonsoft.Json.Linq;

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
            addr = dest.ToString();
            _socket = new DealerSocket();
            _socket.Connect(addr);
        }


        public string Receive()
        {
            return _socket.ReceiveMultipartStrings()[0];
        }

        public void Disconnect()
        {
            _socket.Disconnect(addr);
        }
        public void Dispose()
        {
            if (_socket != null && !_socket.IsDisposed)
            {
                Disconnect();
                _socket.Dispose();
            }
        }

        public void Send(ICommand cmd)
        {
            _socket.SendFrame(JsonConvert.SerializeObject(cmd));
        }

        public string Query(ICommand cmd)
        {
            Send(cmd);
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


        ~NetMQClient()
        {
            Dispose();
        }

    }

}

