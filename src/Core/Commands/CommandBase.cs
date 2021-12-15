using MACOs.JY.ActorFramework.Core.Devices;
using System;
using System.Reflection;

namespace MACOs.JY.ActorFramework.Core.Commands
{
    public static class GlobalCommand
    {
        public const string Connected = "CONNECTED";
        public const string Accepted = "ACCEPTED";
    }

    public static class CommandContext
    {
        public static BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    }
    public abstract class CommandBase : ICommand
    {
        public string MethodName { get; set; }
        public string ParameterQualifiedName { get; }
        public CommandBase(string name)
        {
            MethodName = name;
            ParameterQualifiedName = this.GetType().AssemblyQualifiedName;
        }

        protected CommandBase()
        {
        }

        public abstract object Execute(IDevice instance);

        public abstract string GetSimplifiedString();
        public string DefaultConvertString(object obj)
        {
            return obj != null ? obj.ToString() : "";
        }
        public virtual string ResultConvert(object obj)
        {
            return obj != null ? obj.ToString() : "";
        }
        public static CommandBase Create(string methodName)
        {
            return new Command(methodName);
        }
        public static CommandBase Create<T1>(string methodName, T1 param1)
        {
            return new Command<T1>(methodName, param1);
        }
        public static CommandBase Create<T1, T2>(string methodName, T1 param1, T2 param2)
        {
            return new Command<T1, T2>(methodName, param1, param2);

        }
        public static CommandBase Create<T1, T2, T3>(string methodName, T1 param1, T2 param2, T3 param3)
        {
            return new Command<T1, T2, T3>(methodName, param1, param2, param3);

        }
        public static CommandBase Create<T1, T2, T3, T4>(string methodName, T1 param1, T2 param2, T3 param3, T4 param4)
        {
            return new Command<T1, T2, T3, T4>(methodName, param1, param2, param3, param4);

        }
        public static CommandBase Create<T1, T2, T3, T4, T5>(string methodName, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5)
        {
            return new Command<T1, T2, T3, T4, T5>(methodName, param1, param2, param3, param4, param5);

        }
        public static CommandBase Create<T1, T2, T3, T4, T5, T6>(string methodName, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6)
        {
            return new Command<T1, T2, T3, T4, T5, T6>(methodName, param1, param2, param3, param4, param5, param6);

        }
        public static CommandBase Create<T1, T2, T3, T4, T5, T6, T7>(string methodName, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7)
        {
            return new Command<T1, T2, T3, T4, T5, T6, T7>(methodName, param1, param2, param3, param4, param5, param6, param7);

        }

        public Type[] GetParameterTypes()
        {
            return this.GetType().GenericTypeArguments;
        }

    }
    public class Command : CommandBase
    {

        public Command(string name) : base(name)
        {
        }

        public override string ResultConvert(object obj)
        {
            return base.DefaultConvertString(obj);
        }

        public override object Execute(IDevice instance)
        {
            var mi = instance.GetType().GetMethod(MethodName, CommandContext.flags);
            if (mi == null)
            {
                throw new CommandNotFoundException($"Method not found: {MethodName}");
            }
            else
            {
                var response = mi.Invoke(instance, null);
                return response;
            }
        }
        public CommandBase Generate()
        {
            var obj = new Command(this.MethodName);
            return obj;
        }

        public override string GetSimplifiedString()
        {
            return $"[{base.MethodName}]:";

        }
    }
    public class Command<T1> : CommandBase
    {
        public Tuple<T1> Parameter { get; set; }

        public Command(string name, T1 param1) : base(name)
        {
            Parameter = new Tuple<T1>(param1);

        }
        public override object Execute(IDevice instance)
        {
            var ParameterTypes = this.GetParameterTypes();
            var mi = instance.GetType().GetMethod(MethodName, CommandContext.flags, null, ParameterTypes, null);
            if (mi == null)
            {
                throw new CommandNotFoundException($"Method not found: {MethodName}({ParameterTypes[0].Name})");
            }
            else
            {
                return mi.Invoke(instance, new object[] { Parameter.Item1 });
            }
        }
        public CommandBase Generate(T1 param1)
        {
            var obj = new Command<T1>(this.MethodName, param1);
            return obj;
        }
        public override string ResultConvert(object obj)
        {
            return base.DefaultConvertString(obj);
        }
        public override string GetSimplifiedString()
        {
            return $"[{base.MethodName}]: {Parameter.Item1}";

        }

    }
    public class Command<T1, T2> : CommandBase
    {
        public Tuple<T1, T2> Parameter { get; set; }
        public Command(string name, T1 param1, T2 param2) : base(name)
        {
            Parameter = new Tuple<T1, T2>(param1, param2);

        }

        public override object Execute(IDevice instance)
        {
            var ParameterTypes = this.GetParameterTypes();

            var mi = instance.GetType().GetMethod(MethodName, CommandContext.flags, null, ParameterTypes, null);
            if (mi == null)
            {
                throw new CommandNotFoundException($"Method not found: {MethodName}({ParameterTypes[0].Name},{ParameterTypes[1].Name})");
            }
            else
            {
                return mi.Invoke(instance, new object[] { Parameter.Item1, Parameter.Item2 });

            }

        }
        public CommandBase Generate(T1 param1, T2 param2)
        {
            var obj = new Command<T1, T2>(this.MethodName, param1, param2);
            return obj;
        }
        public override string ResultConvert(object obj)
        {
            return base.DefaultConvertString(obj);
        }
        public override string GetSimplifiedString()
        {
            return $"[{base.MethodName}]: {Parameter.Item1},{Parameter.Item2}";

        }

    }
    public class Command<T1, T2, T3> : CommandBase
    {
        public Tuple<T1, T2, T3> Parameter { get; set; }
        public Command(string name, T1 param1, T2 param2, T3 param3) : base(name)
        {
            Parameter = new Tuple<T1, T2, T3>(param1, param2, param3);

        }

        public override object Execute(IDevice instance)
        {
            var ParameterTypes = this.GetParameterTypes();

            var mi = instance.GetType().GetMethod(MethodName, CommandContext.flags, null, ParameterTypes, null);
            if (mi == null)
            {
                throw new CommandNotFoundException($"Method not found: " +
                    $"{MethodName}(" +
                    $"{ParameterTypes[0].Name}," +
                    $"{ParameterTypes[1].Name}," +
                    $"{ParameterTypes[2].Name})");
            }
            else
            {
                return mi.Invoke(instance, new object[] { Parameter.Item1, Parameter.Item2, Parameter.Item3 });
            }

        }
        public CommandBase Generate(T1 param1, T2 param2, T3 param3)
        {
            var obj = new Command<T1, T2, T3>(this.MethodName, param1, param2, param3);
            return obj;
        }
        public override string ResultConvert(object obj)
        {
            return base.DefaultConvertString(obj);
        }
        public override string GetSimplifiedString()
        {
            return $"[{base.MethodName}]: {Parameter.Item1},{Parameter.Item2}.{Parameter.Item3}";

        }

    }
    public class Command<T1, T2, T3, T4> : CommandBase
    {
        public Tuple<T1, T2, T3, T4> Parameter { get; set; }
        public Command(string name, T1 param1, T2 param2, T3 param3, T4 param4) : base(name)
        {
            Parameter = new Tuple<T1, T2, T3, T4>(param1, param2, param3, param4);

        }

        public override object Execute(IDevice instance)
        {
            var ParameterTypes = this.GetParameterTypes();

            var mi = instance.GetType().GetMethod(MethodName, CommandContext.flags, null, ParameterTypes, null);
            if (mi == null)
            {
                throw new CommandNotFoundException($"Method not found: " +
    $"{MethodName}(" +
    $"{ParameterTypes[0].Name}," +
    $"{ParameterTypes[1].Name}," +
    $"{ParameterTypes[2].Name}," +
    $"{ParameterTypes[3].Name})");

            }
            else
            {
                return mi.Invoke(instance, new object[] { Parameter.Item1, Parameter.Item2, Parameter.Item3, Parameter.Item4 });
            }

        }
        public CommandBase Generate(T1 param1, T2 param2, T3 param3, T4 param4)
        {
            var obj = new Command<T1, T2, T3, T4>(this.MethodName, param1, param2, param3, param4);
            return obj;
        }
        public override string ResultConvert(object obj)
        {
            return base.DefaultConvertString(obj);
        }
        public override string GetSimplifiedString()
        {
            return $"[{base.MethodName}]: {Parameter.Item1},{Parameter.Item2}.{Parameter.Item3},{Parameter.Item4}";

        }

    }
    public class Command<T1, T2, T3, T4, T5> : CommandBase
    {

        public Tuple<T1, T2, T3, T4, T5> Parameter { get; set; }
        public Command(string name, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5) : base(name)
        {
            Parameter = new Tuple<T1, T2, T3, T4, T5>(param1, param2, param3, param4, param5);

        }

        public override object Execute(IDevice instance)
        {
            var ParameterTypes = this.GetParameterTypes();

            var mi = instance.GetType().GetMethod(MethodName, CommandContext.flags, null, ParameterTypes, null);
            if (mi == null)
            {
                throw new CommandNotFoundException($"Method not found: " +
    $"{MethodName}(" +
    $"{ParameterTypes[0].Name}," +
    $"{ParameterTypes[1].Name}," +
    $"{ParameterTypes[2].Name}," +
    $"{ParameterTypes[3].Name}," +
    $"{ParameterTypes[4].Name})");
            }

            else
            {
                return mi.Invoke(instance, new object[] { Parameter.Item1, Parameter.Item2, Parameter.Item3, Parameter.Item4, Parameter.Item5 });
            }

        }
        public CommandBase Generate(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5)
        {
            var obj = new Command<T1, T2, T3, T4, T5>(this.MethodName, param1, param2, param3, param4, param5);
            return obj;
        }
        public override string ResultConvert(object obj)
        {
            return base.DefaultConvertString(obj);
        }
        public override string GetSimplifiedString()
        {
            return $"[{base.MethodName}]: {Parameter.Item1},{Parameter.Item2}.{Parameter.Item3},{Parameter.Item4},{Parameter.Item5}";

        }

    }
    public class Command<T1, T2, T3, T4, T5, T6> : CommandBase
    {

        public Tuple<T1, T2, T3, T4, T5, T6> Parameter { get; set; }
        public Command(string name, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6) : base(name)
        {
            Parameter = new Tuple<T1, T2, T3, T4, T5, T6>(param1, param2, param3, param4, param5, param6);

        }

        public override object Execute(IDevice instance)
        {
            var ParameterTypes = this.GetParameterTypes();

            var mi = instance.GetType().GetMethod(MethodName, CommandContext.flags, null, ParameterTypes, null);
            if (mi == null)
            {
                throw new CommandNotFoundException($"Method not found: " +
    $"{MethodName}(" +
    $"{ParameterTypes[0].Name}," +
    $"{ParameterTypes[1].Name}," +
    $"{ParameterTypes[2].Name}," +
    $"{ParameterTypes[3].Name}," +
    $"{ParameterTypes[4].Name}," +
    $"{ParameterTypes[5].Name})");
            }
            else
            {
                return mi.Invoke(instance, new object[] { Parameter.Item1, Parameter.Item2, Parameter.Item3, Parameter.Item4, Parameter.Item5, Parameter.Item6 });
            }
        }
        public CommandBase Generate(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6)
        {
            var obj = new Command<T1, T2, T3, T4, T5, T6>(this.MethodName, param1, param2, param3, param4, param5, param6);
            return obj;
        }
        public override string ResultConvert(object obj)
        {
            return base.DefaultConvertString(obj);
        }
        public override string GetSimplifiedString()
        {
            return $"[{base.MethodName}]: {Parameter.Item1},{Parameter.Item2}.{Parameter.Item3},{Parameter.Item4},{Parameter.Item5},{Parameter.Item6}";

        }

    }
    public class Command<T1, T2, T3, T4, T5, T6, T7> : CommandBase
    {
        public Tuple<T1, T2, T3, T4, T5, T6, T7> Parameter { get; set; }
        public Command(string name, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7) : base(name)
        {
            Parameter = new Tuple<T1, T2, T3, T4, T5, T6, T7>(param1, param2, param3, param4, param5, param6, param7);
        }

        public override object Execute(IDevice instance)
        {
            var ParameterTypes = this.GetParameterTypes();

            var mi = instance.GetType().GetMethod(MethodName, CommandContext.flags, null, ParameterTypes, null);
            if (mi == null)
            {
                throw new CommandNotFoundException($"Method not found: " +
    $"{MethodName}(" +
    $"{ParameterTypes[0].Name}," +
    $"{ParameterTypes[1].Name}," +
    $"{ParameterTypes[2].Name}," +
    $"{ParameterTypes[3].Name}," +
    $"{ParameterTypes[4].Name}," +
    $"{ParameterTypes[5].Name}," +
    $"{ParameterTypes[6].Name})");
            }
            else
            {
                return mi.Invoke(instance, new object[] { Parameter.Item1, Parameter.Item2, Parameter.Item3, Parameter.Item4, Parameter.Item5, Parameter.Item6, Parameter.Item7 });
            }
        }
        public CommandBase Generate(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7)
        {
            var obj = new Command<T1, T2, T3, T4, T5, T6, T7>(this.MethodName, param1, param2, param3, param4, param5, param6, param7);
            return obj;
        }
        public override string ResultConvert(object obj)
        {
            return base.DefaultConvertString(obj);
        }
        public override string GetSimplifiedString()
        {
            return $"[{base.MethodName}]: {Parameter.Item1},{Parameter.Item2}.{Parameter.Item3},{Parameter.Item4},{Parameter.Item5},{Parameter.Item6},{Parameter.Item7}";

        }

    }

}
