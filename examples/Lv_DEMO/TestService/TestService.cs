using MACOs.JY.ActorFramework.Core.Commands;
using MACOs.JY.ActorFramework.Core.Devices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestService
{
    public class TestService : DeviceBase
    {
        public static Command<int> NumberCommand { get; set; } = new Command<int>("Number", 5);

        public Command<string> TestCommand { get; } = new Command<string>("WalkyTalky", null);
        public Command<double[]> QueryCommand { get; } = new Command<double[]>("ArrayData", null);
        public Command Command { get; } = new Command("Test");

        public string Number(int x)
        {
            return x.ToString();
        }

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

    }
}
