using MACOs.JY.ActorFramework.Core.Devices;

namespace MACOs.JY.ActorFramework.Core.Commands
{
    public interface ICommand
    {
        string MethodName { get; set; }
        object Execute(IDevice instance);
        string ConvertResultToString(object obj);
    }
}
