using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace MACOs.JY.ActorFramework
{
    public class ActorFactory
    {
        private static Logger _log = LogManager.CreateNullLogger();

        private static List<Actor> globalActorCollection = new List<Actor>();

        public static void EnableLogging()

        {
            _log = LogManager.GetLogger("Factory");
        }
        public static void DisableLogging()
        {
            _log = LogManager.CreateNullLogger();
        }
        public static T Create<T>(bool logEnabled, string alias, params object[] param)
        {
            _log.Info("Create New Actor");
            try
            {
                Type t = typeof(T);

                if (t.BaseType == typeof(Actor))
                {
                    var g = t.GetConstructor(param.Select(x => x.GetType()).ToArray());
                    if (g != null)
                    {
                        var actor = g.Invoke(param) as Actor;
                        actor.ActorAliasName = alias;
                        actor.StartService();
                        actor.LogEnabled = logEnabled;
                        _log.Debug(string.Format("{0} is created", actor.UniqueID));
                        globalActorCollection.Add(actor);
                        return (T)Convert.ChangeType(actor, typeof(T));
                    }
                    else
                    {
                        return default(T);
                    }

                }
                else
                {
                    return default(T);
                }

            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return default(T);

            }
        }

        public static void StopAllActors()
        {
            foreach (Actor item in globalActorCollection)
            {
                item.StopService();
            }
        }

        public static void Kill(Actor actor)
        {
            actor.StopService();
            globalActorCollection.Remove(actor);
        }
        public static void Export(params Assembly[] assems)
        {
            XmlAttribute attribute;
            Type[] actors;
            if (assems.Length==0)
            {
                actors = Assembly.GetEntryAssembly().GetTypes().Where(x => x.BaseType == typeof(Actor)).ToArray();
            }
            else
            {
                List<Type> dummy = new List<Type>();
                foreach (Assembly item in assems)
                {
                    dummy.AddRange( item.GetTypes().Where(x => x.BaseType == typeof(Actor)));

                }
                actors = dummy.ToArray();
            }
            
            
            
            XmlDocument xmlDoc = new XmlDocument();
            XmlNode rootNode = xmlDoc.CreateElement("Actors");
            xmlDoc.AppendChild(rootNode);

            foreach (var item in actors)
            {
                var flags = BindingFlags.Instance|BindingFlags.Public | BindingFlags.NonPublic;

                XmlNode actorNode = xmlDoc.CreateElement(item.Name);
                attribute = xmlDoc.CreateAttribute("Namespace");
                attribute.Value = item.Namespace.ToString();
                actorNode.Attributes.Append(attribute);
                rootNode.AppendChild(actorNode);

                var cmds = item.GetMethods(flags).Where(x => x.GetCustomAttribute(typeof(ActorCommandAttribute)) != null).ToArray();

                foreach (var cmd in cmds)
                {
                    XmlNode cmdNode = xmlDoc.CreateElement(cmd.Name);
                    actorNode.AppendChild(cmdNode);

                    foreach (var param in cmd.GetParameters())
                    {
                        XmlNode paramNode = xmlDoc.CreateElement(param.Name);
                        attribute = xmlDoc.CreateAttribute("Type");
                        attribute.Value = param.ParameterType.ToString();
                        paramNode.Attributes.Append(attribute);
                        cmdNode.AppendChild(paramNode);
                    }
                }
            }
            xmlDoc.Save("actors_config.xml");
        }
    }
}