﻿using NLog;
using NLog.LayoutRenderers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MACOs.JY.ActorFramework
{
    public abstract class Actor
    {
        #region Private Fields

        private Channel<object> response = Channel.CreateUnbounded<object>();
        private ActorCommandCollection methods = new ActorCommandCollection();
        private Logger _logService = LogManager.CreateNullLogger();
        private bool logEnabled = false;
        private InternalCommnucationModule _internalComm;
        private InnerCommunicator _comm;
        #endregion Private Fields

        #region Public Properties

        /// <summary>
        /// Default Timeout value for reading response (default=5000)
        /// </summary>
        public int TimeoutDefaultValue { get; set; } = 5000;

        /// <summary>
        /// Class name of the actor
        /// </summary>
        public string ActorClassName { get; }

        /// <summary>
        /// Unique ID for each of different actors
        /// </summary>
        public string UniqueID { get; private set; }

        /// <summary>
        /// User-defined alias name
        /// </summary>
        public string ActorAliasName { get; set; }

        /// <summary>
        /// Internal comuunication ways to choose, default is NetMQ
        /// </summary>
        private InternalCommnucationModule InternalCommType
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
        protected Actor()
        {
            LayoutRenderer.Register<BuildConfigLayoutRender>("buildConfiguration");
            methods = Actor.GetCommandList(this.GetType());
            this.ActorClassName = this.GetType().Name;
            ActorAliasName = ActorClassName;
            InternalCommType = InternalCommnucationModule.ConcurrentQueue;
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
            UniqueID = ActorAliasName + "-" + this.GetHashCode().ToString();
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

        /// <summary>
        /// Asynchronously send and executes the ActorCommand
        /// </summary>
        /// <param name="cmd">command that actor supports</param>
        public async Task DoAsync(ActorCommand cmd)
        {
            try
            {
                await Task.Run(() =>
                {
                    CheckCommandCompatilibity(cmd);
                    _comm.Send(cmd);
                });
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
        /// Asynchronously send and executes the ActorCommand
        /// </summary>
        /// <param name="methodName"> method name</param>
        /// <param name="param">method parameters</param>
        public async Task DoAsync(string methodName, params object[] param)
        {
            try
            {
                var cmd = new ActorCommand(methodName, param);
                await ExecuteAsync(cmd);

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
        /// Synchronously send and executes the ActorCommand
        /// </summary>
        /// <param name="cmd">command that actor supports</param>
        public void Do(ActorCommand cmd)
        {
            try
            {
                CheckCommandCompatilibity(cmd);
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
        /// Synchronously send and executes the ActorCommand
        /// </summary>
        /// <param name="methodName"> method name</param>
        /// <param name="param">method parameters</param>
        public void Do(string methodName, params object[] param)
        {
            try
            {
                var cmd = new ActorCommand(methodName, param);
                Do(cmd);

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
        /// Get the first element of type T stored in the FIFO of actor object, this function will hold the thread until new data is derived or timeout
        /// </summary>
        /// <typeparam name="T">type of the result object</typeparam>
        /// <param name="keepNullValue">keep the null return value</param>
        /// <param name="timeout">timeout in milliseconds</param>
        /// <returns></returns>
        public T GetFeeedback<T>(bool keepNullValue = false, int timeout = 5000)
        {
            T result = default(T);
            Stopwatch sw = new Stopwatch();
            try
            {
                sw.Restart();
                do
                {
                    if (TryGetFeeedback<T>(keepNullValue, out result))
                    {
                        break;
                    }
                    Thread.Sleep(1);
                } while (sw.ElapsedMilliseconds <= timeout);

                if (sw.ElapsedMilliseconds > timeout)
                {
                    throw new ActorException(string.Format("Timeout, value={0}", timeout));
                }
                if (result == null)
                {
                    return default(T);
                }
                else
                {
                    return result;
                }
            }
            catch (ActorException ex)
            {
                _logService.Error(ex.Message);
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
        /// Get the first return element that store in the FIFO of actor object asynchronously
        /// </summary>
        /// <typeparam name="T">type of the result object</typeparam>
        /// <param name="result">result object</param>
        /// <returns>true if new element is deququed</returns>
        public bool TryGetFeeedback<T>(bool keepNullValue,out T result)
        {
            object ans;
            try
            {
                if (response.Reader.TryRead(out ans))
                {
                    if (ans == null)
                    {
                        if (keepNullValue)
                        {
                            result = default(T);
                            return true;
                        }
                        else
                        {
                            result = default(T);
                            return false;
                        }
                    }
                    else 
                    {
                        if (ans is T)
                        {
                            result = (T)ans;
                        }
                        else
                        {
                            result = (T)System.Convert.ChangeType(ans, typeof(T));
                        }
                        return true;

                    }

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

        /// <summary>
        /// Get the first return element that store in the FIFO of actor object asynchronously
        /// </summary>
        /// <typeparam name="T">type of the result object</typeparam>
        /// <param name="result">result object</param>
        /// <returns>true if new element is deququed</returns>
        public async Task<T> GetFeeedbackAsync<T>(bool keepNullValue = false, int timeout = 5000)
        {
            try
            {
                T result = default(T);
                await Task.Run(() =>
                {
                    result = GetFeeedback<T>(keepNullValue, timeout);
                });
                return result;
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
        /// Send and execute the command synchronously. This method will wait until new element of type T shows up or timeout happens
        /// </summary>
        /// <typeparam name="T">type of return value</typeparam>
        /// <param name="cmd">command object</param>
        /// <returns></returns>
        public T Execute<T>(string methodName, params object[] param)
        {
            try
            {
                var cmd = new ActorCommand(methodName, param);
                CheckCommandCompatilibity(cmd);

                _comm.Send(cmd);
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
        /// Send and execute the command synchronously. This method will wait until new element of type T shows up or timeout happens
        /// </summary>
        /// <typeparam name="T">type of return value</typeparam>
        /// <param name="cmd">command object</param>
        /// <returns></returns>
        public T Execute<T>(ActorCommand cmd, int timeout = 5000)
        {
            try
            {
                CheckCommandCompatilibity(cmd);
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

        /// <summary>
        /// Send and execute the command synchronously. This method will wait until execution completed
        /// </summary>
        /// <param name="methodName">method name</param>
        /// <param name="param">method parameters</param>
        public void Execute(string methodName, params object[] param)
        {
            try
            {
                var cmd = new ActorCommand(methodName, param);
                CheckCommandCompatilibity(cmd);

                _comm.Send(cmd);
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
        /// Send and execute the command synchronously. This method will wait until execution completed
        /// </summary>
        /// <param name="cmd">command object</param>
        /// <param name="timeout">timeout value, default=5000</param>
        public void Execute(ActorCommand cmd, int timeout = 5000)
        {
            try
            {
                CheckCommandCompatilibity(cmd);

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
        /// <summary>
        /// Send and execute the command asynchronously. This method will wait until new element of type T shows up or timeout happens
        /// </summary>
        /// <typeparam name="T">type of return value</typeparam>
        /// <param name="cmd">command object</param>
        /// <returns></returns>
        public async Task<T> ExecuteAsync<T>(string methodName, params object[] param)
        {
            try
            {
                var cmd = new ActorCommand(methodName, param);
                return await ExecuteAsync<T>(cmd, TimeoutDefaultValue);
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
        /// Send and execute the command asynchronously. This method will wait until new element of type T shows up or timeout happens
        /// </summary>
        /// <typeparam name="T">type of return value</typeparam>
        /// <param name="cmd">command object</param>
        /// <returns></returns>
        public async Task<T> ExecuteAsync<T>(ActorCommand cmd, int timeout = 5000)
        {
            try
            {
                T result = default(T);
                await Task.Run(() =>
                {
                    CheckCommandCompatilibity(cmd);
                    _comm.Send(cmd);
                    result = GetFeeedback<T>(false, timeout);
                });
                return result;
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
        /// Send and execute the command asynchronously. This method will wait until execution completed
        /// </summary>
        /// <param name="methodName">method name</param>
        /// <param name="param">method parameters</param>
        public async Task ExecuteAsync(string methodName, params object[] param)
        {
            try
            {
                var cmd = new ActorCommand(methodName, param);
                await ExecuteAsync(cmd, TimeoutDefaultValue);
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
        /// Send and execute the command asynchronously. This method will wait until execution completed
        /// </summary>
        /// <param name="cmd">command object</param>
        /// <param name="timeout">timeout value, default=5000</param>
        public async Task ExecuteAsync(ActorCommand cmd, int timeout = 5000)
        {
            try
            {
                await Task.Run(() =>
                {
                    CheckCommandCompatilibity(cmd);

                    _comm.Send(cmd);
                    //bypass the return value, either is null or not
                    GetFeeedback<object>(true, timeout);

                });
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

        private void CommandReceived(object sender, ActorCommand e)
        {
            _logService.Info("Execute command: " + e.Name);
            _logService.Debug("Execution starts: " + e.ToString());
            try
            {
                var res = methods.GetMethod(e.Name).Invoke(this, e.Parameters);
                response.Writer.WriteAsync(res).AsTask().Wait();
                OnMsgExecutionDone(e, res);
                _logService.Debug("Execution complete: " + res?.ToString());
                _logService.Info("Completed: " + e.Name);
            }
            catch (ArgumentException ex)
            {
                string msg = string.Format("[{0}]Type of parameters are incorrect", e.Name);
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

        private void OnMsgExecutionDone(ActorCommand cmd, object result)
        {
            MessageExecutionDone?.Invoke(this, new MessageExecutionDoneArgs(cmd, result));
        }

        private void CheckCommandCompatilibity(ActorCommand cmd)
        {
            Type t = typeof(ActorCommandAttribute);

            //check the method name
            if (!methods.Exists(cmd.Name))
            {
                string msg = string.Format("Method \"{0}\" is not found", cmd.Name);
                _logService.Error(msg);
                throw new ActorException(msg);
            }

            //check the parameters count
            var param = methods.GetMethod(cmd.Name).GetParameters();
            if (cmd.Parameters.Length != param.Length)
            {
                string msg = string.Format("[{0}]Number of parameters is not correct. Expected={1}, Actual={2}", cmd.Name, param.Length, cmd.Parameters.Length);
                _logService.Error(msg);
                throw new ActorException(msg);
            }
        }

        #endregion Private Methods

        #region Events

        /// <summary>
        /// Event after execution is completed
        /// </summary>
        public event EventHandler<object> MessageExecutionDone;

        /// <summary>
        /// Execution Completed event arguments
        /// </summary>
        public class MessageExecutionDoneArgs : EventArgs
        {
            public object ReturnData { get; set; }
            public ActorCommand Command { get; set; }

            public MessageExecutionDoneArgs(ActorCommand cmd, object returnValue)
            {
                Command = cmd;
                ReturnData = returnValue;
            }
        }

        #endregion Events

        #region Static Methods

        /// <summary>
        /// Get the supported command list, which is commented by ActorCommandAttribute
        /// </summary>
        /// <param name="t">Type of Actor</param>
        /// <returns></returns>
        public static ActorCommandCollection GetCommandList(Type t)
        {
            ActorCommandCollection methods = new ActorCommandCollection();

            var dict = new Dictionary<string, ParameterInfo[]>();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var item in t.GetMethods(flags))
            {
                var att = item.GetCustomAttribute(typeof(ActorCommandAttribute));
                if (att != null)
                {
                    methods.Add(item);
                }
            }
            return methods;
        }

        #endregion Static Methods
    }
}