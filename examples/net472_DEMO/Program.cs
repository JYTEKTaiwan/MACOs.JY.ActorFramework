using MACOs.JY.ActorFramework.Core.Commands;
using MACOs.JY.ActorFramework.Core.Devices;
using MACOs.JY.ActorFramework.Implement.NetMQ;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace net472_DEMO
{
    class Program
    {
        static void Main(string[] args)
        {
            DeviceBase server = new TestService();
            server.LoadDataBus(new NetMQDataBusContext()
            {
                BeaconIPAddress = "",
                BeaconPort=9999,
                Port=-1,
                AliasName="DEMO",
                IsSilent=false,
                LocalIP=@"tcp://127.0.0.1"
                
            });
           
            var clientConnIndo = new NetMQClientContext(9999, "DEMO");
            var client = clientConnIndo.Search();

            var sw = new Stopwatch();
            while (true)
            {
                Console.Write("Enter Command: ");
                var str = Console.ReadLine();
                int len;
                if (str == "Q")
                {
                    break;
                }
                else if (int.TryParse(str, out len))
                {
                    var data = new double[len];
                    client.Send(TestService.QueryCommand.Generate(data));
                    var res = client.Receive();
                    sw.Restart();
                    client.Send(TestService.QueryCommand.Generate(data));
                    res = client.Receive();
                    var elapsed = sw.ElapsedMilliseconds;
                    Console.WriteLine(res);
                    Console.WriteLine($"{DateTime.Now.ToLongTimeString()}\t{elapsed}ms");
                    Console.WriteLine();
                }
                else
                {
                    sw.Restart();
                    client.Send(new Command<string>("WalkyTalky", str));
                    var res = client.Receive();
                    var elapsed = sw.ElapsedMilliseconds;
                    Console.WriteLine(res);
                    Console.WriteLine($"{DateTime.Now.ToLongTimeString()}\t{elapsed}ms");
                    Console.WriteLine();
                }
            }
            server.Dispose();
        }
    }


    public class CommandDateTime<T1> : Command<T1>
    {
        public CommandDateTime(string name, T1 param1) : base(name, param1)
        {
        }

        public override string ConvertToString(object obj)
        {
            return ((double[])obj).Length.ToString();
        }
    }

    public class TestService : DeviceBase
    {
        public static Command<string> TestCommand { get; } = new Command<string>("WalkyTalky", null);
        public static Command<double[]> QueryCommand { get; } = new Command<double[]>("ArrayData", null);

        //public static CommandDateTime<int> ArrCommand { get; } = new CommandDateTime<int>("Array", null);
        public string WalkyTalky(string content)
        {
            return string.Format($"[{DateTime.Now.ToString()}] Roger!\t{content}");
        }
        public void Sum(int x, int y, int z, int t)
        {
            (x + y + z + t).ToString();
        }
        public string ArrayData(double[] data)
        {
            return JsonConvert.SerializeObject(data);
        }
        public double[] Array (int len)
        {
            return new double[len];
        }

    }

    public class TestServiceContext : IDeviceContext
    {
        public void LoadFromJson(JToken token)
        {

        }

        public IDevice NewInstance()
        {
            return new TestService();
        }
    }
}
