using MACOs.JY.ActorFramework.Core.Commands;

namespace MACOs.JY.ActorFramework.Clients
{
    public interface IClient
    {
        string TargetName { get; set; }
        void Send(ICommand cmd, int timeoutMilliSecond = -1);
        string Receive(int timeoutMilliSecond = -1);
        string Query(ICommand cmd, int timeoutMilliSecond = -1);
        void Dispose();
    }
}
