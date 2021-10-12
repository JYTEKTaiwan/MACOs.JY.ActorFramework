using MACOs.JY.ActorFramework.Core.Commands;
using MACOs.JY.ActorFramework.Core.DataBus;
using MACOs.JY.ActorFramework.Core.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MACOs.JY.ActorFramework.Core.Devices
{
    /// <summary>
    /// Base object that is used in MACOs.Service
    /// Consists of (1) User-defined class and (2) DataBus object
    /// </summary>
    public abstract class DeviceBase : IDevice
    {
        private Logger _logger = LogManager.GetCurrentClassLogger();
        private IDataBus _bus;

        public string Name { get; set; }
        public string ConnectionInfo { get; set; }
        /// <summary>
        /// public event after the command is executed
        /// </summary>
        public event ExecuteCompleteEvent OnExecutionComplete;
        public DeviceBase()
        {
            _logger.Trace("Begin ctor");
            _logger.Debug("Query supported methods");
            _logger.Info("Object is created");

        }
        /// <summary>
        /// New data event from databus
        /// </summary>
        private string Bus_OnDataReady(object sender, object args)
        {
            _logger.Trace("New data event is triggered");
            var cmd = ConvertToCommand(args);
            var result = cmd.Execute();
            var ans = cmd.ConvertToString(result);
            //notify event
            _logger.Debug("ExecuteCompleteEvent is fired");
            OnExecutionComplete?.Invoke(this, ans);
            _logger.Debug($"command is executed with result: {ans}");
            _logger.Info($"Command is executed ");
            return ans;
        }
        /// <summary>
        /// Execute command
        /// </summary>
        /// <param name="msg">Command object</param>
        public virtual string ExecuteCommand(ICommand cmd)
        {
            string gg = JsonConvert.SerializeObject(cmd);            
            return _bus.Query(JsonConvert.SerializeObject(cmd));
        }
        public virtual ICommand ConvertToCommand(object msg)
        {
            var str = msg.ToString() ;
            JToken token = JToken.Parse(str);
            Type t=token["Type"].ToObject<Type>();
            var cmd = JsonConvert.DeserializeObject(str, t) as ICommand;
            cmd.Instance = this;
            _logger.Debug($"Convert to ICommand");
            return cmd;
        }
        public virtual void Dispose()
        {
            _bus.Kill();
            _logger.Info("Object is successfully disposed");
        }
        /// <summary>
        /// Load databus from IDataBusContext object
        /// </summary>
        /// <param name="databusContext">Context object for coresponding databus.</param>
        public void LoadDataBus(IDataBusContext databusContext)
        {
            _logger.Debug("Load databus from context");
            _bus = databusContext.NewInstance();
            _bus.Configure();
            Name = _bus.Name;
            _bus.OnDataReady += Bus_OnDataReady;
            _bus.Start();
            _logger.Debug("Databus starts");            

        }

        /// <summary>
        /// Load databus object and apply
        /// </summary>
        /// <param name="databus">Instance of DataBus</param>
        public void LoadDataBus(IDataBus databus)
        {
            _logger.Debug("Load databus from instance");
            _bus = databus;
            _bus.Configure();
            Name = _bus.Name;
            _bus.OnDataReady += Bus_OnDataReady;
            _bus.Start();
            _logger.Debug("Databus starts");
        }

        ~DeviceBase()
        {
        }
    }
}
