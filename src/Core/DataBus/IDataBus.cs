using MACOs.JY.ActorFramework.Core.Utilities;

namespace MACOs.JY.ActorFramework.Core.DataBus
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
