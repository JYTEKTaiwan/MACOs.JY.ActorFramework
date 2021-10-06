using MACOs.Services.Devices;
using System;

namespace MACOs.Services.Commands
{
    public interface ICommand
    {
        string Name { get; }
        Type Type { get;}
        IDevice Instance { get; set; }
        string Execute();
    }
}
