using MACOs.JY.ActorFramework.Core.Devices;
using System;

namespace MACOs.JY.ActorFramework.Core.Commands
{
    public interface ICommand
    {
        string Name { get; }
        Type Type { get;}
        IDevice Instance { get; set; }
        object Execute();
        string ConvertToString(object obj);
    }
}
