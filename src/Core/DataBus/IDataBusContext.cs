using Newtonsoft.Json.Linq;

namespace MACOs.Services.DataBus
{
    public interface IDataBusContext
    {
        IDataBus NewInstance();
    }
}
