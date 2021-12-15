using System;
using System.Runtime.Serialization;

namespace MACOs.JY.ActorFramework.Core
{
    public class DataBusNotLoadedExceptions : Exception
    {
        public DataBusNotLoadedExceptions()
        {
        }

        public DataBusNotLoadedExceptions(string message) : base(message)
        {
        }

        public DataBusNotLoadedExceptions(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DataBusNotLoadedExceptions(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
    public class CommandNotFoundException : Exception
    {
        public CommandNotFoundException()
        {
        }

        public CommandNotFoundException(string message) : base(message)
        {
        }

        public CommandNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected CommandNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
