using Newtonsoft.Json.Linq;

namespace MACOs.Services.Devices
{
    public interface IDeviceContext
    {
        IDevice NewInstance();

    }
}
