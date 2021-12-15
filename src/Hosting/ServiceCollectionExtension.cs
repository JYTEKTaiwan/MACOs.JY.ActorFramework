using MACOs.JY.ActorFramework.Clients;
using MACOs.JY.ActorFramework.Core.Devices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MACOs.JY.ActorFramework.Hosting
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddDevice<T>(this IServiceCollection series, IConfigurationSection section)
        {
            if (typeof(IDevice).IsAssignableFrom(typeof(T)))
            {
                series.AddScoped<IDevice>(x => DeviceFactory.Create<T>(section));
            }
            return series;
        }
        public static IServiceCollection AddClient<T>(this IServiceCollection series, IClientContext ctxt)
        {
            if (typeof(IClient).IsAssignableFrom(typeof(T)))
            {
                series.AddScoped<IClient>(x => ClientFactory.Create<T>(ctxt));
            }
            return series;
        }
        public static ClientCollection ToClientCollection(this IEnumerable<IClient> clients)
        {
            return new ClientCollection(clients);
        }
    }

    public class ClientCollection : List<IClient>
    {
        public ClientCollection(IEnumerable<IClient> collection) : base(collection)
        {
        }
        new public IClient this[string name]
        {
            get
            {
                return (IClient)this.First(x => x.TargetName == name);
            }
        }
    }
}