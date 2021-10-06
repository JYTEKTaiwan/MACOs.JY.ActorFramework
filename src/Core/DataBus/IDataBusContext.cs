using Newtonsoft.Json.Linq;

namespace MACOs.JY.ActorFramework.Core.DataBus
{
    public interface IDataBusContext
    {
        IDataBus NewInstance();
    }
}
