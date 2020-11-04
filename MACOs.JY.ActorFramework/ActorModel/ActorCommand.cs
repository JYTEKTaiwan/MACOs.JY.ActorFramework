using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace MACOs.JY.ActorFramework
{
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
    public class ActorCommandCollection
    {
        public Dictionary<string, MethodInfo> SupportedMethods { get; } = new Dictionary<string, MethodInfo>();
        public bool Exists(string key)
        {
            return SupportedMethods.Any(x => x.Key == key);
        }
        public void Add(MethodInfo item)
        {
            SupportedMethods.Add(((ActorCommandAttribute)item.GetCustomAttribute(typeof(ActorCommandAttribute))).Name, item);
        }

        public MethodInfo GetMethod(string key)
        {
            return SupportedMethods[key];
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
}
