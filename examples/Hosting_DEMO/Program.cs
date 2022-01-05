using MACOs.JY.ActorFramework.Clients;
using MACOs.JY.ActorFramework.Core.Commands;
using MACOs.JY.ActorFramework.Core.Devices;
using MACOs.JY.ActorFramework.Hosting;
using MACOs.JY.ActorFramework.Implement.NetMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Reflection;


Console.WriteLine("========================================================================================");
Console.WriteLine("== Welcome to MACOs.JY.ActorFramework example, there are 3 types of command supported ==");
Console.WriteLine("========================================================================================");
Console.WriteLine();


var config = new ConfigurationBuilder()
.SetBasePath(System.IO.Directory.GetCurrentDirectory())
.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
.Build();
IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    services
     .AddDevice<TestService>(config.GetSection("Dev1"))
     .AddDevice<TestService>(config.GetSection("Dev2"))
     .AddClient<IClient>(new NetMQClientContext("DEMO"))
     .AddClient<IClient>(new NetMQClientContext("AnotherDEMO"))
     )
    .Build();

host.RunAsync();

var logger=host.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation($"Two targets are created, 'DEMO' & 'AnotherDEMO'");


StartScopedService(host.Services, CommandBase.Create("WalkyTalky", DateTime.Now.ToString()));
Thread.Sleep(1000);
StartScopedService(host.Services, CommandBase.Create("WalkyTalky", DateTime.Now.ToString()));

Console.WriteLine("PRESS ANY KEY TO EXIT");
Console.ReadKey();
host.StopAsync();

logger.LogInformation("STOP");

static void StartScopedService(IServiceProvider services,CommandBase cmd)
{
    var logger = services.GetService<ILogger<Program>>();
    services.CreateScope();
    var devs = services.GetServices<IDevice>();
    var clients = services.GetServices<IClient>().ToClientCollection();


    foreach (var item in clients)
    {
        var response = item.Query(cmd);
        logger.LogDebug($"Target[{item.TargetName}]: {response}");
    }
    // Can also use target name to get the right instance
    var answer = clients["DEMO"].Query(cmd);
    logger.LogDebug($"Target[{clients["DEMO"].TargetName}]: {answer}");
    answer = clients["AnotherDEMO"].Query(cmd);
    logger.LogDebug($"Target[{clients["AnotherDEMO"].TargetName}]: {answer}");
}


public class TestService : DeviceBase
{

    [Command]
    public string Test()
    {
        return $"{this.BusAlias}-{this.GetHashCode()}";
    }
    [Command]
    public string WalkyTalky(string content)
    {

        return string.Format($"[{DateTime.Now.ToString()}] [{this.BusAlias}*{this.GetHashCode()}]Roger!\t{content}");
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

    public string Dummy(int a, string b, DateTime c)
    {
        return null;
    }
    public DateTime Now()
    {
        return DateTime.Now;
    }
}
