using MACOs.JY.ActorFramework.Core.Commands;
using MACOs.JY.ActorFramework.Core.Devices;
using MACOs.JY.ActorFramework.Implement.NetMQ;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;

namespace net50_DEMO
{
    class Program
    {
        static void Main(string[] args)
        {
            DeviceBase server = new TestService();
            server.LoadDataBus(new NetMQDataBusContext()
            {
                BeaconIPAddress = "",
                BeaconPort = 9999,
                Port = -1,
                AliasName = "DEMO",
                IsSilent = false,
                LocalIP = @"tcp://127.0.0.1"

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
                    client.Send(TestService.TestCommand.Generate(str));
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


    public class TestService : DeviceBase
    {
        public static Command<string> TestCommand { get; } = new Command<string>("WalkyTalky", null);
        public static Command<double[]> QueryCommand { get; } = new Command<double[]>("ArrayData", null);
        public string WalkyTalky(string content)
        {
            return string.Format($"[{DateTime.Now.ToString()}] Roger!\t{content}");
        }
        public string Sum(int x, int y, int z, int t)
        {
            return (x + y + z + t).ToString();
        }
        public string ArrayData(double[] data)
        {
            return JsonConvert.SerializeObject(data);
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
