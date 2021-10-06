using MACOs.Services.Commands;

namespace MACOs.Services.Clients
{
    public interface IClient
    {
        void Send(ICommand cmd);
        string Receive();
        string Query(ICommand cmd);
        void Disconnect();

        void Dispose();


    }
}
