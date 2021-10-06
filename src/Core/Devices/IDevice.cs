using MACOs.Services.Commands;
using MACOs.Services.DataBus;
using MACOs.Services.Utilities;

namespace MACOs.Services.Devices
{
    public interface IDevice
    {
        string Name { get; set; }
        /// <summary>
        /// Load databus from context object
        /// </summary>
        /// <param name="databusContext">IDataBusContext object</param>
        void LoadDataBus(IDataBusContext databusContext);
        /// <summary>
        /// Load databus object and apply
        /// </summary>
        /// <param name="databus">IDataBus object</param>
        void LoadDataBus(IDataBus databus);

        /// <summary>
        /// Execute the command object and return the response
        /// </summary>
        /// <param name="msg">Command message</param>
        string ExecuteCommand(ICommand cmd);

        ICommand ConvertToCommand(object msg);
        /// <summary>
        /// Public event after command is executed
        /// </summary>
        event ExecuteCompleteEvent OnExecutionComplete;
        /// <summary>
        /// Kill and dispose the object
        /// </summary>
        void Dispose();
    }
}
