using MACOs.JY.ActorFramework.Core.Commands;
using MACOs.JY.ActorFramework.Core.Devices;
using MACOs.JY.ActorFramework.Implement.NetMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace net50_DEMO
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                    services
                    .AddScoped<TestService>()
                    ).Build();
            await host.RunAsync();

            Console.WriteLine("========================================================================================");
            Console.WriteLine("== Welcome to MACOs.JY.ActorFramework example, there are 3 types of command supported ==");
            Console.WriteLine("==      1. key in Q will leave the program                                            ==");
            Console.WriteLine("==      2. key in any text except Q will immediate response                           ==");
            Console.WriteLine("==      3. key in number will reponse the double array with the assigned size         ==");
            Console.WriteLine("========================================================================================");

            TestService server = host.Services.GetRequiredService<TestService>();
            var ip = Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString();
            //var ip = "127.0.0.1";
            server.LoadDataBus(new NetMQDataBusContext()
            {
                AliasName = "DEMO",
            });

            var clientConnInfo = new NetMQClientContext("DEMO") { ListeningIP = ip };
            var client = clientConnInfo.Search();
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
                    var res = client.Query(server.QueryCommand.Generate(data));
                    sw.Restart();
                    res = client.Query(server.QueryCommand.Generate(data));
                    var elapsed = sw.ElapsedMilliseconds;
                    Console.WriteLine(res);
                    Console.WriteLine($"{DateTime.Now.ToLongTimeString()}\t{elapsed}ms");
                    Console.WriteLine();
                }
                else if (str == "Now")
                {
                    sw.Restart();
                    var res = client.Query(new DateTimeCommand("Now"));
                    var elapsed = sw.ElapsedMilliseconds;
                    Console.WriteLine(res);
                    Console.WriteLine($"{DateTime.Now.ToLongTimeString()}\t{elapsed}ms");
                    Console.WriteLine();

                }
                else
                {
                    sw.Restart();
                    CommandBase cmd = CommandBase.Create("WalkyTalky", str);
                    var res = client.Query(cmd);
                    var elapsed = sw.ElapsedMilliseconds;
                    Console.WriteLine(res);
                    Console.WriteLine($"{DateTime.Now.ToLongTimeString()}\t{elapsed}ms");
                    Console.WriteLine();
                }
            }

            client.Dispose();
            server.Dispose();
        }
    }

    public class DateTimeCommand : Command
    {
        public DateTimeCommand(string name) : base(name)
        {
        }

        public override string ConvertToString(object obj)
        {
            return ((DateTime)obj).ToString("HH:mm:ss.fff");
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
        public double[] Array(int len)
        {
            return new double[len];
        }
        public DateTime Now()
        {
            return DateTime.Now;
        }
    }
}
