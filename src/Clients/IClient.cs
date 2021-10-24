using MACOs.JY.ActorFramework.Core.Commands;

namespace MACOs.JY.ActorFramework.Clients
{
    public interface IClient
    {
        void Send(ICommand cmd);
        string Receive();
        string Query(ICommand cmd);
        void Dispose();
    }
}
