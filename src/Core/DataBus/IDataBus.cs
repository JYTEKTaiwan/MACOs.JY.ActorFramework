using MACOs.Services.Utilities;

namespace MACOs.Services.DataBus
{
    public interface IDataBus
    {
        string Name { get; set; }
        /// <summary>
        /// Check if instance is released
        /// </summary>
        bool IsDisposed { get; set; }
        /// <summary>
        /// Public event for new data coming
        /// </summary>
        event DataReadyEvent OnDataReady;
        string Query(string jsonContent);

        /// <summary>
        /// Configure
        /// </summary>
        void Configure();
        /// <summary>
        /// Start
        /// </summary>
        void Start();
        /// <summary>
        /// Stop
        /// </summary>
        void Stop();
        /// <summary>
        /// Stop (if running) and release the instance
        /// </summary>
        void Kill();

    }
}
