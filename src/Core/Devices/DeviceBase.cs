using MACOs.JY.ActorFramework.Core.Commands;
using MACOs.JY.ActorFramework.Core.DataBus;
using MACOs.JY.ActorFramework.Core.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if NET6_0_OR_GREATER
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
#endif


namespace MACOs.JY.ActorFramework.Core.Devices
{
    /// <summary>
    /// Base object that is used in MACOs.Service
    /// Consists of (1) User-defined class and (2) DataBus object
    /// </summary>
    public abstract class DeviceBase : IDevice
    {
        private readonly Dictionary<string, Type> _methodsLookUp;
        private readonly Logger _logger;

        private IDataBus _bus;

        private bool isDisposed = false;
        public string Name { get; set; }
        public string BusAlias { get; set; }

        public bool EnableLogging { get; set; } = true;
        public string ConnectionInfo { get; set; }
        /// <summary>
        /// public event after the command is executed
        /// </summary>
        public event ExecuteCompleteEvent OnExecutionComplete;

        public DeviceBase()
        {
            Name = this.GetType().Name;
            _logger = EnableLogging ? NLog.LogManager.GetLogger($"{this.GetType().FullName}") : LogManager.CreateNullLogger();
            _logger.Trace("Begin creaeting DeviceBase object");
            _logger.Info("Object is created");
            _methodsLookUp = ParsingMethodInfos();
        }
        private Dictionary<string, Type> ParsingMethodInfos()
        {
            var dict = new Dictionary<string, Type>();
            var flags = BindingFlags.Public | BindingFlags.Instance;
            MethodInfo[] methods;
            if (this.GetType().GetMethods().Where(x=>x.GetCustomAttributes<CommandAttribute>(false).Count()>0).Count()>0)
            {
                //CommandInfoAttribute is used, only takes methods with CommandInfoAttribute exists
                methods = this.GetType().GetMethods().Where(x =>
                {
                    return x.GetCustomAttributes<CommandAttribute>(false).Count() > 0;
                }).ToArray();
            }
            else
            {
                //no CommandInfoAttribute is found, search the derived class methods instead
                methods = this.GetType().GetMethods().Where(x =>
                {
                    return !x.IsVirtual && !x.IsSpecialName;
                }).ToArray();
            }

            var cmdBase = typeof(CommandBase).GetMethods().Where(m => m.Name == "Create").ToArray();

            foreach (var item in methods)
            {
                var param = item.GetParameters();
                MethodInfo method;
                if (param.Length != 0)
                {
                    //if parameter count >0
                    method = cmdBase[param.Length].MakeGenericMethod(param.Select(x => x.ParameterType).ToArray());
                }
                else
                {
                    //if parameter count =0(non-generic method)
                    method = cmdBase[0];

                }
                dict.Add(item.Name, method.ReturnType);
            }
            return dict;
        }
        /// <summary>
        /// New data event from databus
        /// </summary>
        private string Bus_OnDataReady(object sender, object args)
        {
            //Convert to ICommand, execute, and convert the response to string
            try
            {
                _logger.Trace("New command is received");

                var cmd = ConvertToCommand(args);

                _logger.Trace("Start executing the command and converting the response into string");
                var result = cmd.Execute(this);
                var ans = cmd.ConvertResultToString(result);
                _logger.Debug($"Command is executed with result: {ans}");
                _logger.Info($"Command is received and executed ");

                _logger.Debug("Firing OnExecutionComplete event");
                OnExecutionComplete?.Invoke(this, ans);

                return ans;
            }
            catch (CommandNotFoundException ex)
            {
                LogError(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error: {ex.Message}");
                throw ex;
            }
        }
        /// <summary>
        /// Execute command
        /// </summary>
        /// <param name="msg">Command object</param>
        public virtual string ExecuteCommand(ICommand cmd)
        {
            return Bus_OnDataReady(null, JsonConvert.SerializeObject(cmd));
        }
        public virtual ICommand ConvertToCommand(object msg)
        {
            ICommand cmd;
            string methodName = "";
            try
            {
                _logger.Trace("Start converting to ICommand");
                var str = msg.ToString();

                methodName = JToken.Parse(str)["MethodName"].ToString();
                cmd = JsonConvert.DeserializeObject(str, _methodsLookUp[methodName]) as ICommand;
                _logger.Debug((cmd as CommandBase).GetSimplifiedString());
                _logger.Info($"Command is converted successfully");
                return cmd;
            }
            catch  (KeyNotFoundException ex)
            {
                var err= new CommandNotFoundException($"Command '{methodName}' not found", ex);
                LogError(err);
                throw err;
            }
            catch (Exception ex)
            {
                LogError(ex);
                throw ex;
            }
        }
        public virtual void Dispose()
        {
            try
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }
            if (disposing)
            {
                _logger.Trace("Start disposing the object");
                _bus?.Dispose();
                _logger.Info("Object is successfully disposed");

                isDisposed = true;
            }
        }
        /// <summary>
        /// Load databus from IDataBusContext object
        /// </summary>
        /// <param name="databusContext">Context object for coresponding databus.</param>
        public void LoadDataBus(IDataBusContext databusContext)
        {
            try
            {
                _logger.Debug("Start loading databus from context");
                _bus = databusContext.NewInstance();
                _bus.Configure();
                BusAlias = _bus.Name;
                _bus.OnDataReady += Bus_OnDataReady;
                _bus.Start();
                _logger.Info("Databus is loaded and starts");
            }
            catch (Exception ex)
            {
                _bus?.Dispose();
                LogError(ex);
                throw ex;
            }

        }

        /// <summary>
        /// Load databus object and apply
        /// </summary>
        /// <param name="databus">Instance of DataBus</param>
        public void LoadDataBus(IDataBus databus)
        {
            try
            {
                _logger.Debug("Load databus from IDataBus object");
                _bus = databus;
                _bus.Configure();
                BusAlias = _bus.Name;
                _bus.OnDataReady += Bus_OnDataReady;
                _bus.Start();
                _logger.Info("Databus is loaded and starts");

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        ~DeviceBase()
        {
            Dispose(false);
        }
        private void LogError(Exception ex)
        {
            _logger.Error($"[{ex.Message}] {ex.StackTrace}");
        }

    }
}
