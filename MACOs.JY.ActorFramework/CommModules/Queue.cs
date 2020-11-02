using System.Collections.Concurrent;
using System.Threading;

namespace MACOs.JY.ActorFramework.CommModules
{
    internal class Queue : InnerCommunicator
    {
        private ConcurrentQueue<ActorCommand> q_cmd = new ConcurrentQueue<ActorCommand>();
        private Thread t_cmd;
        private volatile bool _isRunning = false;
        private ActorCommand cmd;

        private void CommandLoop()
        {
            while (_isRunning)
            {
                if (q_cmd.TryDequeue(out cmd))
                {
                    this.OnCommandReceived(this, cmd);
                }
                Thread.Sleep(1);
            }
        }

        public override void Send(ActorCommand cmd)
        {
            q_cmd.Enqueue(cmd);
        }

        public override void Start()
        {
            this.ID = q_cmd.GetHashCode().ToString();
            _isRunning = true;
            t_cmd = new Thread(CommandLoop);
            t_cmd.Start();
        }

        public override void Stop()
        {
            _isRunning = false;
            t_cmd.Join(500);
            if (t_cmd.IsAlive)
            {
                t_cmd.Abort();
            }
        }
    }
}