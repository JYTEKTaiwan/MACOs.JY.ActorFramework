using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MACOs.JY.ActorFramework.CommModules
{
    internal class Queue : InnerCommunicator
    {

        private Channel<ActorCommand> cmdChannel = Channel.CreateUnbounded<ActorCommand>();
        private ConcurrentQueue<ActorCommand> q_cmd = new ConcurrentQueue<ActorCommand>();
        private Thread t_cmd;
        private volatile bool _isRunning = false;
        private ActorCommand cmd;

        private async void CommandLoop()
        {
            while (_isRunning)
            {
                var cmd=await cmdChannel.Reader.ReadAsync();
                this.OnCommandReceived(this, cmd);
                Thread.Sleep(1);
            }
        }

        public override async void Send(ActorCommand cmd)
        {
           await cmdChannel.Writer.WriteAsync(cmd);
          
        }

        public override void Start()
        {
            this.ID = cmdChannel.GetHashCode().ToString();
            _isRunning = true;
            t_cmd = new Thread(CommandLoop);
            t_cmd.Start();
        }

        public override void Stop()
        {
            q_cmd = new ConcurrentQueue<ActorCommand>();
            _isRunning = false;
            t_cmd.Join(500);
            if (t_cmd.IsAlive)
            {
                t_cmd.Abort();
            }
        }
    }
}