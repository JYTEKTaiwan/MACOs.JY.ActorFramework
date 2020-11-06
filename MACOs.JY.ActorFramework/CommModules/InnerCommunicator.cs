using System;

namespace MACOs.JY.ActorFramework
{
    internal abstract class InnerCommunicator
    {
        public event EventHandler<ActorCommand> CommandReceived;

        public string ID { get; set; }

        public abstract void Start();

        public abstract void Stop();

        public abstract void Send(ActorCommand cmd);

        public void OnCommandReceived(object sender, ActorCommand e)
        {
            CommandReceived?.Invoke(sender, e);
        }

        public static InnerCommunicator CreateInstance(InternalCommnucationModule module)
        {
            switch (module)
            {
                case InternalCommnucationModule.NetMQ:
                    return new CommModules.NetMQ();

                case InternalCommnucationModule.ConcurrentQueue:
                    return new CommModules.Queue();

                default:
                    return new CommModules.NetMQ();
            }
        }

        public void ClearEvent()
        {
            if (CommandReceived != null)
            {
                foreach (EventHandler<ActorCommand> item in CommandReceived.GetInvocationList())
                {
                    CommandReceived -= item;
                }
            }
        }
    }

    public enum InternalCommnucationModule
    {
        NetMQ,
        ConcurrentQueue,
    }
}