using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json.Linq;
using System;

namespace MACOs.JY.ActorFramework.CommModules
{
    internal class NetMQ : InnerCommunicator
    {
        internal class ShimHandler : IShimHandler
        {
            private PairSocket shim;
            private NetMQPoller poller;
            public string ID { get; private set; }

            public event EventHandler<ActorCommand> ShimCommandReceived;

            public void Initialise(object state)
            {
            }

            public void Run(PairSocket shim)
            {
                this.shim = shim;
                ID = shim.Options.LastEndpoint;
                shim.ReceiveReady += OnShimReady;
                shim.SignalOK();
                poller = new NetMQPoller() { shim };
                poller.Run();
            }

            private void OnShimReady(object sender, NetMQSocketEventArgs e)
            {
                string command = e.Socket.ReceiveFrameString();
                if (command == NetMQActor.EndShimMessage)
                {
                    poller.Stop();
                    return;
                }
                var cmd = ActorCommand.FromJson(command);               
                ShimCommandReceived?.Invoke(this, cmd);
            }
        }

        private NetMQActor actor;
        private NetMQMessage msg = new NetMQMessage();

        public override void Send(ActorCommand cmd)
        {
            msg.Clear();
            msg.Append(ActorCommand.ToJson(cmd));
            actor.SendMultipartMessage(msg);
        }

        public override void Start()
        {
            try
            {
                if (actor != null)
                    return;

                var _shim = new ShimHandler();
                _shim.ShimCommandReceived += this.OnCommandReceived;
                actor = NetMQActor.Create(_shim);
                this.ID = _shim.GetHashCode().ToString();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public override void Stop()
        {
            try
            {
                if (actor != null)
                {
                    actor.Dispose();
                    actor = null;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}