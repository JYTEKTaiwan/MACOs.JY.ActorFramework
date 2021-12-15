using MACOs.JY.ActorFramework.Clients;
using MACOs.JY.ActorFramework.Core.DataBus;
using MACOs.JY.ActorFramework.Core.Devices;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace MACOs.JY.ActorFramework.Hosting
{
    internal sealed class DeviceFactory
    {
        public static IDevice Create<T>(IConfiguration section)
        {
            Type t = Assembly.LoadFrom(section.GetValue<string>("BusAssembly"))
                .GetTypes().First(x => x.Name == section.GetValue<string>("BusClassname"));
            var _bus = Activator.CreateInstance(t);
            section.GetSection("BusParameter").Bind(_bus);

            var instance = Activator.CreateInstance(typeof(T)) as IDevice;
            if (_bus != null)
            {
                instance?.LoadDataBus((_bus as IDataBusContext));
            }
            return instance;

        }
    }
    internal sealed class ClientFactory
    {
        public static IClient Create<T>(IClientContext ctxt)
        {
            return ctxt.Search();
        }
    }

}
