using MACOs.JY.ActorFramework.Core.Devices;
using Newtonsoft.Json;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MACOs.JY.ActorFramework.Core.Commands
{
    public abstract class CommandBase : ICommand
    {
        public BindingFlags flags { get; } = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        public IDevice Instance { get; set; }

        public string Name { get; }

        public Type Type { get; }

        public CommandBase(string name, Type type)
        {
            Name = name;
            Type = type;
        }

        protected CommandBase()
        {
        }

        public abstract string Execute();

        public static CommandBase Create(string methodName)
        {
            return new Command(methodName);
        }
        public static CommandBase Create<T1>(string methodName,T1 param1)
        {
            return new Command<T1>(methodName,param1);
        }
        public static CommandBase Create<T1,T2>(string methodName, T1 param1,T2 param2)
        {
            return new Command<T1,T2>(methodName, param1,param2);

        }
        public static CommandBase Create<T1, T2,T3>(string methodName, T1 param1, T2 param2,T3 param3)
        {
            return new Command<T1, T2,T3>(methodName, param1, param2,param3);

        }
        public static CommandBase Create<T1, T2, T3,T4>(string methodName, T1 param1, T2 param2, T3 param3,T4 param4)
        {
            return new Command<T1, T2, T3,T4>(methodName, param1, param2, param3,param4);

        }
        public static CommandBase Create<T1, T2, T3, T4,T5>(string methodName, T1 param1, T2 param2, T3 param3, T4 param4,T5 param5)
        {
            return new Command<T1, T2, T3, T4,T5>(methodName, param1, param2, param3, param4,param5);

        }
        public static CommandBase Create<T1, T2, T3, T4, T5,T6>(string methodName, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5,T6 param6)
        {
            return new Command<T1, T2, T3, T4, T5,T6>(methodName, param1, param2, param3, param4, param5,param6);

        }
        public static CommandBase Create<T1, T2, T3, T4, T5, T6,T7>(string methodName, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6,T7 param7)
        {
            return new Command<T1, T2, T3, T4, T5, T6,T7>(methodName, param1, param2, param3, param4, param5, param6,param7);

        }

    }
    public class Command : CommandBase
    {

        public Command(string name) : base(name, typeof(Command))
        {
        }
        public override string Execute()
        {
            var mi = Instance.GetType().GetMethod(Name,this.flags);
            var res = mi.Invoke(Instance, null);
            return res.ToString();
        }
        public CommandBase Generate()
        {
            var obj = new Command(this.Name);
            return obj;
        }

    }
    public class Command<T1> : CommandBase
    {
        private static Type[] parameterTypes = new Type[] { typeof(T1) };

        public Tuple<T1> Parameter { get; set; }

        public Command(string name, T1 param1) : base(name, typeof(Command<T1>))
        {
            Parameter = new Tuple<T1>(param1);
        }
        public override string Execute()
        {            
            var mi = Instance.GetType().GetMethod(Name,this.flags,null,parameterTypes,null);
            var res = mi.Invoke(Instance, new object[] { Parameter.Item1 });
            return res.ToString();
        }
        public CommandBase Generate(T1 param1)
        {
            var obj = new Command<T1>(this.Name, param1);
            return obj;
        }

    }
    public class Command<T1, T2> : CommandBase
    {
        private static Type[] parameterTypes = new Type[] { typeof(T1), typeof(T2) };

        public Tuple<T1, T2> Parameter { get; set; }
        public Command(string name, T1 param1, T2 param2) : base(name, typeof(Command<T1, T2>))
        {
            Parameter = new Tuple<T1, T2>(param1, param2);
        }

        public override string Execute()
        {
            var mi = Instance.GetType().GetMethod(Name, this.flags, null, parameterTypes, null);
            var res = mi.Invoke(Instance, new object[] { Parameter.Item1, Parameter.Item2 });
            return res.ToString();
        }
        public CommandBase Generate(T1 param1, T2 param2)
        {
            var obj = new Command<T1, T2>(this.Name, param1, param2);
            return obj;
        }

    }
    public class Command<T1, T2, T3> : CommandBase
    {
        private static Type[] parameterTypes = new Type[] { typeof(T1), typeof(T2), typeof(T3) };

        public Tuple<T1, T2, T3> Parameter { get; set; }
        public Command(string name, T1 param1, T2 param2, T3 param3) : base(name, typeof(Command<T1, T2, T3>))
        {
            Parameter = new Tuple<T1, T2, T3>(param1, param2, param3);

        }

        public override string Execute()
        {
            var mi = Instance.GetType().GetMethod(Name, this.flags, null, parameterTypes, null);
            var res = mi.Invoke(Instance, new object[] { Parameter.Item1, Parameter.Item2, Parameter.Item3 });
            return res.ToString();
        }
        public CommandBase Generate(T1 param1, T2 param2, T3 param3)
        {
            var obj = new Command<T1, T2, T3>(this.Name, param1, param2, param3);
            return obj;
        }

    }
    public class Command<T1, T2, T3, T4> : CommandBase
    {
        private static Type[] parameterTypes = new Type[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) };

        public Tuple<T1, T2, T3, T4> Parameter { get; set; }
        public Command(string name, T1 param1, T2 param2, T3 param3, T4 param4) : base(name, typeof(Command<T1, T2, T3, T4>))
        {
            Parameter = new Tuple<T1, T2, T3, T4>(param1, param2, param3, param4);

        }

        public override string Execute()
        {
            var mi = Instance.GetType().GetMethod(Name, this.flags, null, parameterTypes, null);
            var res = mi.Invoke(Instance, new object[] { Parameter.Item1, Parameter.Item2, Parameter.Item3, Parameter.Item4 });
            return res.ToString();
        }
        public CommandBase Generate(T1 param1, T2 param2, T3 param3, T4 param4)
        {
            var obj = new Command<T1, T2, T3, T4>(this.Name, param1, param2, param3, param4);
            return obj;
        }

    }
    public class Command<T1, T2, T3, T4, T5> : CommandBase
    {
        private static Type[] parameterTypes = new Type[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) };

        public Tuple<T1, T2, T3, T4, T5> Parameter { get; set; }
        public Command(string name, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5) : base(name, typeof(Command<T1, T2, T3, T4, T5>))
        {
            Parameter = new Tuple<T1, T2, T3, T4, T5>(param1, param2, param3, param4, param5);

        }

        public override string Execute()
        {
            var mi = Instance.GetType().GetMethod(Name, this.flags, null, parameterTypes, null);
            var res = mi.Invoke(Instance, new object[] { Parameter.Item1, Parameter.Item2, Parameter.Item3, Parameter.Item4, Parameter.Item5 });
            return res.ToString();
        }
        public CommandBase Generate(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5)
        {
            var obj = new Command<T1, T2, T3, T4, T5>(this.Name, param1, param2, param3, param4, param5);
            return obj;
        }

    }
    public class Command<T1, T2, T3, T4, T5,T6> : CommandBase
    {
        private static Type[] parameterTypes = new Type[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6) };

        public Tuple<T1, T2, T3, T4, T5, T6> Parameter { get; set; }
        public Command(string name, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6) : base(name, typeof(Command<T1, T2, T3, T4, T5, T6>))
        {
            Parameter = new Tuple<T1, T2, T3, T4, T5, T6>(param1, param2, param3, param4, param5,param6);
        }

        public override string Execute()
        {
            var mi = Instance.GetType().GetMethod(Name, this.flags, null, parameterTypes, null);
            var res = mi.Invoke(Instance, new object[] { Parameter.Item1, Parameter.Item2, Parameter.Item3, Parameter.Item4, Parameter.Item5 ,Parameter.Item6});
            return res.ToString();
        }
        public CommandBase Generate(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6)
        {
            var obj = new Command<T1, T2, T3, T4, T5, T6>(this.Name, param1, param2, param3, param4, param5,param6);
            return obj;
        }

    }
    public class Command<T1, T2, T3, T4, T5, T6,T7> : CommandBase
    {
        private static Type[] parameterTypes = new Type[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7) };

        public Tuple<T1, T2, T3, T4, T5, T6, T7> Parameter { get; set; }
        public Command(string name, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7) : base(name, typeof(Command<T1, T2, T3, T4, T5, T6>))
        {
            Parameter = new Tuple<T1, T2, T3, T4, T5, T6, T7>(param1, param2, param3, param4, param5, param6,param7);
        }

        public override string Execute()
        {
            var mi = Instance.GetType().GetMethod(Name, this.flags, null, parameterTypes, null);
            var res = mi.Invoke(Instance, new object[] { Parameter.Item1, Parameter.Item2, Parameter.Item3, Parameter.Item4, Parameter.Item5, Parameter.Item6,Parameter.Item7 });
            return res.ToString();
        }
        public CommandBase Generate(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7)
        {
            var obj = new Command<T1, T2, T3, T4, T5, T6, T7>(this.Name, param1, param2, param3, param4, param5, param6,param7);
            return obj;
        }

    }

}
