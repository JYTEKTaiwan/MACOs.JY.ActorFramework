using System;
using System.Runtime.Serialization;

namespace MACOs.JY.ActorFramework
{
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
