using MACOs.JY.ActorFramework.Core.Commands;

namespace MACOs.JY.ActorFramework.Clients
{
    public interface IClient
    {
        void Send(ICommand cmd, int timeoutMilliSecond=-1);
        string Receive( int timeoutMilliSecond=-1);
        string Query(ICommand cmd, int timeoutMilliSecond=-1);
        void Dispose();
    }
}
