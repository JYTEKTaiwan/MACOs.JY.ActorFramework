using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Config;
using NLog.LayoutRenderers;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace MACOs.JY.ActorFramework
{
    public abstract class Actor
    {
        #region Private Fields
        private ConcurrentQueue<object> response = new ConcurrentQueue<object>();
        private List<MethodInfo> methods = new List<MethodInfo>();
        private Logger _logService = LogManager.CreateNullLogger();
        private bool logEnabled = false;
        private InternalCommnucationModule _internalComm;
        private InnerCommunicator _comm;
        #endregion

        #region Public Properties
        public int TimeoutDefaultValue { get; set; } = 5000;
        public string ActorClassName { get; }

        public string UniqueID { get; private set; }

        public string ActorAliasName { get; set; }
        public InternalCommnucationModule InternalCommType
        {
            get { return _internalComm; }
            set
            {

                if (_comm != null)
                {
                    StopService();
                }
                switch (value)
                {
                    case InternalCommnucationModule.NetMQ:
                        break;
                    case InternalCommnucationModule.ConcurrentQueue:
                        break;
                    default:
                        break;
                }
                _comm = InnerCommunicator.CreateInstance(value);
                StartService();
                _internalComm = value;

            }
        }
        public bool LogEnabled
        {
            get { return logEnabled; }
            set
            {
                _logService = value ? LogManager.GetLogger(UniqueID) : LogManager.CreateNullLogger();
                logEnabled = value;
            }
        }

        #endregion Public Properties

        #region Constructor

        /// <summary>
        /// Create an actor that accepts, executes, and responds in its own thread.
        /// </summary>
        public Actor()
        {
            LayoutRenderer.Register<BuildConfigLayoutRender>("buildConfiguration");
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var mi in this.GetType().GetMethods(flags))
            {
                var att = mi.GetCustomAttribute(typeof(ActorCommandAttribute));
                if (att != null)
                {
                    methods.Add(mi);
                }
            }
            this.ActorClassName = this.GetType().Name;
            ActorAliasName = ActorClassName;
            UniqueID = this.GetHashCode().ToString();
            InternalCommType = InternalCommnucationModule.NetMQ;
        }

        ~Actor()
        {
            StopService();
        }

        #endregion Constructor

        #region Public Methods

        /// <summary>
        /// Start the actor
        /// </summary>
        public void StartService()
        {
            _comm.ClearEvent();
            _comm.CommandReceived += CommandReceived;
            _comm.Start();
        }

        /// <summary>
        /// Stop the actor
        /// </summary>
        public void StopService()
        {
            _comm.Stop();
            _comm.ClearEvent();

        }
        public void ExecuteAsync(ActorCommand cmd)
        {
            try
            {
                _comm.Send(cmd);
            }
            catch (ActorException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                string msg = string.Format("Unknown Error");
                _logService.Error(msg);
                throw new ActorException(msg, ex);
            }
        }

        /// <summary>
        /// Send and execute the command asynchronously 
        /// </summary>
        /// <param name="cmd">command object</param>
        public void ExecuteAsync(string methodName, params object[] param)
        {
            try
            {
                _comm.Send(new ActorCommand(methodName, param));
            }
            catch (ActorException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                string msg = string.Format("Unknown Error");
                _logService.Error(msg);
                throw new ActorException(msg, ex);
            }
        }

        /// <summary>
        /// Get the first return element that store in the FIFO of actor object, this function will hold the thread until new data is derived or timeout
        /// </summary>
        /// <typeparam name="T">type of the result object</typeparam>
        /// <param name="timeout">timeout in milliseconds</param>
        /// <returns>true if new element is deququed</returns>
        public T GetFeeedback<T>(bool keepNullValue = false, int timeout = 5000)
        {
            object ans;
            Stopwatch sw = new Stopwatch();
            try
            {
                sw.Restart();
                do
                {
                    if (response.TryDequeue(out ans))
                    {
                        if (ans != null || keepNullValue)
                        {
                            break;
                        }
                    }
                } while (sw.ElapsedMilliseconds <= timeout);

                if (sw.ElapsedMilliseconds > timeout)
                {
                    throw new ActorException(string.Format("Timeout, value={0}",timeout));
                }
                if (ans == null)
                {                    
                    return default(T);
                }                
                return (T)System.Convert.ChangeType(ans, typeof(T));
            }
            catch (ActorException ex)
            {
                _logService.Error(ex.Message);
                throw ex;
            }
            catch (InvalidCastException ex)
            {
                string msg = string.Format("Invalid Casting");
                _logService.Error(msg);
                throw new ActorException(msg, ex);
            }
            catch (Exception ex)
            {
                string msg = string.Format("Unknown Error");
                _logService.Error(msg);
                throw new ActorException(msg, ex);
            }
        }

        /// <summary>
        /// Get the first return element that store in the FIFO of actor object asynchronously
        /// </summary>
        /// <typeparam name="T">type of the result object</typeparam>
        /// <param name="result">result object</param>
        /// <returns>true if new element is deququed</returns>
        public bool GetFeeedbackAsync<T>(out T result)
        {
            object ans;
            try
            {
                bool dataAvailable = response.TryDequeue(out ans);
                if (dataAvailable && ans != null)
                {
                    if (ans is T)
                    {
                        result = (T)ans;
                    }
                    result = (T)System.Convert.ChangeType(ans, typeof(T));
                    return true;
                }
                else
                {
                    result = default(T);
                    return false;
                }
            }
            catch (InvalidCastException ex)
            {
                string msg = string.Format("Invalid Casting");
                _logService.Error(msg);
                throw new ActorException(msg, ex);
            }

            catch (Exception ex)
            {
                string msg = string.Format("Unknown Error");
                _logService.Error(msg);
                throw new ActorException(msg, ex);
            }
        }
        public T Execute<T>(string methodName, params object[] param)
        {
            try
            {
                _comm.Send(new ActorCommand(methodName, param));
                return GetFeeedback<T>(false, TimeoutDefaultValue);
            }
            catch (ActorException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                string msg = string.Format("Unknown Error");
                _logService.Error(msg);
                throw new ActorException(msg, ex);
            }

        }

        /// <summary>
        /// Send and execute the command asynchronously. This method is for functions that have return value
        /// </summary>
        /// <typeparam name="T">type of return value</typeparam>
        /// <param name="cmd">command object</param>
        /// <returns></returns>
        public T Execute<T>(ActorCommand cmd, int timeout = 1000)
        {
            try
            {
                _comm.Send(cmd);
                return GetFeeedback<T>(false, timeout);
            }
            catch (ActorException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                string msg = string.Format("Unknown Error");
                _logService.Error(msg);
                throw new ActorException(msg, ex);
            }
        }

        public void Execute(string methodName, params object[] param)
        {
            try
            {
                _comm.Send(new ActorCommand(methodName, param));
                //bypass the return value, either is null or not
                GetFeeedback<object>(true, TimeoutDefaultValue);
            }
            catch (ActorException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                string msg = string.Format("Unknown Error");
                _logService.Error(msg);
                throw new ActorException(msg, ex);
            }
        }

        /// <summary>
        /// Send and execute the command asynchronously. This method is for functions that void return value
        /// </summary>
        /// <param name="cmd">command object</param>
        public void Execute(ActorCommand cmd, int timeout = 1000)
        {
            try
            {
                _comm.Send(cmd);
                //bypass the return value, either is null or not
                GetFeeedback<object>(true, timeout);
            }
            catch (ActorException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                string msg = string.Format("Unknown Error");
                _logService.Error(msg);
                throw new ActorException(msg, ex);
            }
        }

        #endregion Public Methods

        #region Private Methods


        #endregion

        #region Events
        private void CommandReceived(object sender, ActorCommand e)
        {
            _logService.Info("Execute command: " + e.Name);
            _logService.Debug("Execution starts: " + e.ToString());
            try
            {

                var mi = methods.First(x => (x.GetCustomAttribute(typeof(ActorCommandAttribute)) as ActorCommandAttribute).Name == e.Name);
                var res = mi.Invoke(this, e.Parameters);
                response.Enqueue(res);
                _logService.Debug("Execution complete: " + res.ToString());
                _logService.Info("Completed: " + e.Name);

            }
            catch (InvalidOperationException ex)
            {
                string msg = string.Format("Method \"{0}\" is not found", e.Name);
                _logService.Error(msg);
                throw new ActorException(msg, ex);

            }
            catch (TargetParameterCountException ex)
            {
                string msg = string.Format("Number of parameters ({0}) is not correct", e.Parameters.Length);
                _logService.Error(msg);
                throw new ActorException(msg, ex);
            }
            catch (ArgumentException ex)
            {
                string msg = string.Format("Type of parameters is not correct");
                _logService.Error(msg);
                throw new ActorException(msg, ex);

            }

            catch (Exception ex)
            {
                string msg = string.Format("Unknown Error");
                _logService.Error(msg);
                throw new ActorException(msg, ex);
            }
        }

        #endregion

        #region Static Methods
        public static Dictionary<string, ParameterInfo[]> GetCommandList(Type t)
        {
            var dict = new Dictionary<string, ParameterInfo[]>();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var item in t.GetMethods(flags))
            {
                var att = item.GetCustomAttribute(typeof(ActorCommandAttribute));
                if (att != null)
                {
                    dict.Add(((ActorCommandAttribute)att).Name, item.GetParameters());
                }
            }
            return dict;
        }

        #endregion

    }

    public class ActorCommand
    {
        public string Name { get; set; }
        public object[] Parameters { get; set; }
        public ActorCommand(string name, params object[] param)
        {
            Name = name;
            Parameters = param;
        }
        public static string ToJson(ActorCommand cmd, JsonConverter[] converters = null)
        {
            JObject jo = new JObject();
            List<JsonConverter> conv = new List<JsonConverter>();
            if (converters != null)
            {
                conv.AddRange(converters);
            }
            conv.Add(new ArrayConverter());
            jo.Add(new JProperty("Types", new JArray(cmd.Parameters.Select(x => x.GetType().FullName).ToArray())));
            jo.Add(new JProperty("Name", cmd.Name));
            List<string> param = new List<string>();
            foreach (var item in cmd.Parameters)
            {
                param.Add(JsonConvert.SerializeObject(item, Formatting.Indented, conv.ToArray()));
            }
            jo.Add(new JProperty("Parameters", new JArray(param)));

            return jo.ToString();
        }
        public static ActorCommand FromJson(string jsonString, JsonConverter[] converters = null)
        {

            ActorCommand cmd;
            List<JsonConverter> conv = new List<JsonConverter>();
            if (converters != null)
            {
                conv.AddRange(converters);
            }
            conv.Add(new ArrayConverter());

            JObject jo = JObject.Parse(jsonString);
            string name = jo["Name"].Value<string>();
            Type[] types = jo["Types"].Values<string>().Select(x => Type.GetType(x)).ToArray();
            List<object> param = new List<object>();
            var items = jo["Parameters"].Children().ToArray();
            for (int i = 0; i < items.Count(); i++)
            {
                param.Add(JsonConvert.DeserializeObject(items[i].ToString(), types[i], conv.ToArray()));
            }
            cmd = new ActorCommand(name, param.ToArray());
            return cmd;
        }

        public override string ToString()
        {
            string str = Name.ToString() + ": ";
            for (int i = 0; i < Parameters.Length; i++)
            {
                var end = i == Parameters.Length - 1 ? "" : ",";
                str += Parameters[i].ToString() + end;
            }
            return str;
        }
    }
    public class ArrayConverter : JsonConverter
    {

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JArray.FromObject(value).WriteTo(writer);
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            Type element = objectType.GetElementType();
            var rawValues = JArray.Load(reader).Values<object>().ToArray();
            Array arr = Array.CreateInstance(element, rawValues.Count());
            for (int i = 0; i < rawValues.Count(); i++)
            {
                arr.SetValue(Convert.ChangeType(rawValues[i], element), i);
            }
            return arr;
        }
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsArray;
        }
    }

    public class ActorCommandAttribute : Attribute
    {
        public string Name { get; set; }

        public ActorCommandAttribute(string name)
        {
            Name = name;
        }
    }

    [LayoutRenderer("buildConfiguration")]
    [ThreadAgnostic]
    internal class BuildConfigLayoutRender : LayoutRenderer
    {
        private string buildconfig;

        private string GetBuildConfig()
        {
            if (buildconfig != null)
            {
                return buildconfig;
            }
#if DEBUG
            buildconfig = "Debug";
#else
            buildconfig = "Release";
#endif
            return buildconfig;
        }

        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            builder.Append(GetBuildConfig());
        }
    }


    public class ActorException : Exception
    {
        public ActorException()
        {
        }

        public ActorException(string message) : base(message)
        {
        }

        public ActorException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ActorException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

    }
}