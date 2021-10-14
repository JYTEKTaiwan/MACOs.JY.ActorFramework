using MACOs.JY.ActorFramework.Core.Commands;
using MACOs.JY.ActorFramework.Core.Devices;
using MACOs.JY.ActorFramework.Implement.NetMQ;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;

namespace net472_DEMO
{
    class Program
    {
        static void Main(string[] args)
        {
            TestService server = new TestService();
            server.LoadDataBus(new NetMQDataBusContext()
            {
                BeaconIPAddress = "",
                BeaconPort=9999,
                Port=-1,
                AliasName="DEMO",
                IsSilent=false,
                LocalIP=@"tcp://127.0.0.1"                
            });

            var clientConnInfo = new NetMQClientContext(9999, "DEMO");
            var client = clientConnInfo.Search();
            var g = client.Query(new Command("Test"));
            Console.WriteLine(g);
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
                    var res=server.ExecuteCommand(server.QueryCommand.Generate(data));
                    sw.Restart();
                    res = server.ExecuteCommand(server.QueryCommand.Generate(data));
                    var elapsed = sw.ElapsedMilliseconds;
                    Console.WriteLine(res);
                    Console.WriteLine($"{DateTime.Now.ToLongTimeString()}\t{elapsed}ms");
                    Console.WriteLine();
                }
                else
                {
                    sw.Restart();
                    var res = server.ExecuteCommand(new Command<string>("WalkyTalky", str));
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
        public Command<string> TestCommand { get; } = new Command<string>("WalkyTalky", null);
        public Command<double[]> QueryCommand { get; } = new Command<double[]>("ArrayData", null);
        public Command Command { get; } = new Command("Test");
        public string Test()
        {
            return "Done";
        }
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
}
