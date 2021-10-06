using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System.Threading.Tasks;
using MACOs.JY.ActorFramework.Clients;
using MACOs.JY.ActorFramework.Core.Commands;

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
            return _socket.ReceiveMultipartStrings()[1];
        }

        public void Disconnect()
        {
            _socket.Disconnect(addr);
        }
        public void Dispose()
        {
            if (_socket!=null&&_socket.IsDisposed)
            {
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
            return Receive();
        }


        ~NetMQClient()
        {
            Dispose();
        }

    }

}

