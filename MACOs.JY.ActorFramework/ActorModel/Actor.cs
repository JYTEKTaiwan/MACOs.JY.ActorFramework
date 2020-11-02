using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Config;
using NLog.LayoutRenderers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
        private Logger _logService;
        private bool logEnabled = false;
        private InternalCommnucationModule _internalComm;
        private InnerCommunicator _comm;
        private DelConverter _converter;
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
                        _converter = ConvertNetMQ;
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
                _logService = value ? LogManager.GetLogger(UniqueID) : null;
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
            catch (Exception ex)
            {
                _logService?.Error(ex);
                throw ex;
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
            catch (Exception ex)
            {
                _logService?.Error(ex);
                throw ex;
            }
        }

        /// <summary>
        /// Get the first return element that store in the FIFO of actor object, this function will hold the thread until new data is derived or timeout
        /// </summary>
        /// <typeparam name="T">type of the result object</typeparam>
        /// <param name="timeout">timeout in milliseconds</param>
        /// <returns>true if new element is deququed</returns>
        public T GetFeeedback<T>(bool keepNullValue = false, int timeout = 1000)
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
                    return default(T);
                }
                if (ans is T)
                {
                    return (T)ans;
                }
                try
                {
                    return (T)System.Convert.ChangeType(ans, typeof(T));
                }
                catch (InvalidCastException)
                {
                    _logService?.Warn("Invalid casting");
                    return default(T);
                }
            }
            catch (Exception ex)
            {
                _logService?.Error(ex);
                throw ex;
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
                    try
                    {
                        result = (T)System.Convert.ChangeType(ans, typeof(T));
                    }
                    catch (InvalidCastException)
                    {
                        _logService?.Warn("Invalid casting");

                        result = default(T);
                    }
                    return true;
                }
                else
                {
                    result = default(T);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logService?.Error(ex);

                throw ex;
            }
        }
        public T Execute<T>(string methodName, params object[] param)
        {
            try
            {
                _comm.Send(new ActorCommand(methodName, param));
                return GetFeeedback<T>(false, TimeoutDefaultValue);
            }
            catch (Exception ex)
            {
                _logService?.Error(ex);
                throw ex;
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
            catch (Exception ex)
            {
                _logService?.Error(ex);
                throw ex;
            }
        }

        public void Execute(string methodName, params object[] param)
        {
            try
            {
                _comm.Send(new ActorCommand(methodName, param));
                //bypass the return value, either is null or not
                GetFeeedback<object>(false, TimeoutDefaultValue);
            }
            catch (Exception ex)
            {
                _logService?.Error(ex);
                throw ex;
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
                GetFeeedback<object>(false, timeout);
            }
            catch (Exception ex)
            {
                _logService?.Error(ex);
                throw ex;
            }
        }

        #endregion Public Methods

        #region Private Methods
        private delegate object DelConverter(Type t, string value);


        private object ConvertNetMQ(Type t, string value)
        {
            if (t.GetTypeInfo().IsEnum)
                return Enum.Parse(t, value);
            if (t.GetTypeInfo().IsArray)
            {
                Type element = t.GetElementType();

                switch (element.Name)
                {
                    case "Short":
                        return JArray.Parse(value).Values<short>().ToArray();
                    case "Byte":
                        return JArray.Parse(value).Values<byte>().ToArray();
                    case "UShort":
                        return JArray.Parse(value).Values<ushort>().ToArray();
                    case "Int":
                        return JArray.Parse(value).Values<int>().ToArray();
                    case "Uint":
                        return JArray.Parse(value).Values<uint>().ToArray();
                    case "Float":
                        return JArray.Parse(value).Values<float>().ToArray();
                    case "Long":
                        return JArray.Parse(value).Values<long>().ToArray();
                    case "Double":
                        return JArray.Parse(value).Values<double>().ToArray();
                    default:
                        return new object();
                }

            }
            else
            {
                return System.Convert.ChangeType(value, t);
            }

        }
        #endregion

        #region Events
        private void CommandReceived(object sender, ActorCommand e)
        {
            _logService?.Info("Execute command: " + e.Name);
            _logService?.Debug("Execution starts: " + e.ToString());
            var res = new object();
            try
            {
                var mi = methods.First(x => (x.GetCustomAttribute(typeof(ActorCommandAttribute)) as ActorCommandAttribute).Name == e.Name);

                if (_internalComm == InternalCommnucationModule.NetMQ)
                {
                    var types = mi.GetParameters().Select(x => x.ParameterType).ToArray();
                    for (int i = 0; i < types.Count(); i++)
                    {
                        Type t = types.ElementAt(i);
                        e.Parameters[i] = _converter.Invoke(types[i], e.Parameters[i].ToString());
                    }
                }
                res = mi.Invoke(this, e.Parameters);
                response.Enqueue(res);
            }
            catch (Exception ex)
            {
                if (ex is ArgumentException || ex is TargetParameterCountException)
                {
                    _logService?.Error("Parameter Error");
                }
                else
                {
                    _logService?.Error(ex);
                }
                throw ex;
            }
            _logService?.Debug("Execution complete: " + res.ToString());
            _logService?.Info("Completed: " + e.Name);
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

        public static string ToJson(ActorCommand cmd)
        {
            return JsonConvert.SerializeObject(cmd);
        }

        public static ActorCommand FromJson(string jsonString)
        {
            return JsonConvert.DeserializeObject<ActorCommand>(jsonString);
        }

        public override string ToString()
        {
            string str = Name.ToString() + ": ";
            for (int i = 0; i < Parameters.Length; i++)
            {
                var end = i == Parameters.Length - 1 ? "" : ",";
                str += (Parameters[i].GetType().IsArray ? Parameters[i].GetType().FullName : Parameters[i].ToString()) + end;
            }
            return str;
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
}