using MACOs.JY.ActorFramework.Core.Commands;
using MACOs.JY.ActorFramework.Core.Devices;
using MACOs.JY.ActorFramework.Core;
using MACOs.JY.ActorFramework.Implement.NetMQ;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Threading;
using MACOs.JY.ActorFramework.Clients;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Windows.Forms;

namespace net472_UnitTest
{
    [TestClass]
    public class ServerExceptionTest
    {
        [TestMethod]
        public void RunWithoutDatabusLoaded()
        {
            var svrc = new TestService();
            string response;

            response = svrc.ExecuteCommand(new Command("Test"));
            svrc.Dispose();
            Assert.IsTrue(response == "Test");
        }
        [TestMethod]
        public void BroadcastException_AlphabetInIP()
        {

            Assert.ThrowsException<BeaconException>(new Action(() =>
            {
                var svrc = new TestService();

                svrc.LoadDataBus(new NetMQDataBusContext()
                {
                    BeaconIP = "127.0.0.aa",
                    BeaconPort = 9999,
                    AliasName = "Test"
                }); ; ;
                svrc.Dispose();

            }));

        }

        [TestMethod]
        public void BroadcastException_BadString()
        {

            Assert.ThrowsException<BeaconException>(new Action(() =>
            {
                var svrc = new TestService();

                svrc.LoadDataBus(new NetMQDataBusContext()
                {
                    BeaconIP = "127.0.0.100",
                    BeaconPort = 9999,
                    AliasName = "Test"
                }); ; ;
                svrc.Dispose();

            }));
        }

        [TestMethod]
        public void BroadcastException_BadPort()
        {

            Assert.ThrowsException<BeaconException>(new Action(() =>
            {
                var svrc = new TestService();

                svrc.LoadDataBus(new NetMQDataBusContext()
                {
                    BeaconIP = "",
                    BeaconPort = 99999,
                    AliasName = "Test"
                }); ; ;
                svrc.Dispose();

            }));
        }

        [TestMethod]
        public void SocketException_AlphabetInIP()
        {

            Assert.ThrowsException<BeaconException>(new Action(() =>
            {
                var svrc = new TestService();

                svrc.LoadDataBus(new NetMQDataBusContext()
                {
                    BeaconIP = "192.168.50.a",
                    BeaconPort = 9999,
                    AliasName = "Test"
                }); ; ;
                svrc.Dispose();

            }));
        }
        [TestMethod]
        public void SocketException_BadString()
        {

            Assert.ThrowsException<BeaconException>(new Action(() =>
            {
                var svrc = new TestService();

                svrc.LoadDataBus(new NetMQDataBusContext()
                {
                    BeaconIP = "192.168.20.55",
                    BeaconPort = 9999,
                    AliasName = "Test"
                }); ; ;
                svrc.Dispose();

            })); 
        }


        public class TestService : DeviceBase
        {
            public Command<string> TestCommand { get; } = new Command<string>("WalkyTalky", null);
            public Command<double[]> QueryCommand { get; } = new Command<double[]>("ArrayData", null);
            public Command Command { get; } = new Command("Test");
            public string Test()
            {
                return "Test";
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

    [TestClass]
    public class ClientContextExceptionTest
    {
        private static  TestService svrc;
        private string ip;

        [ClassInitialize]
        public static void GlobalSetup(TestContext context)
        {
            NetMQDataBus.CleanUp(false);
            svrc = new TestService();
            svrc.LoadDataBus(new NetMQDataBusContext()
            {
                AliasName = "Test",
            }); ;

        }
        [ClassCleanup]
        public static void Cleanup()
        {
            svrc.Dispose();
            NetMQDataBus.CleanUp(true);

        }
        [TestInitialize]
        public void Initialize()
        {
            ip = Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString();
        }

        [TestCleanup]
        public void TearDown()
        {

        }

        [TestMethod]
        public void Context_WrongPort()
        {
            Assert.ThrowsException<BeaconException>(new Action(() =>
            {
                var client = new NetMQClientContext("Test",99999, ip).Search();
            }));
        }

        [TestMethod]
        public void Context_WrongIP()
        {
            Assert.ThrowsException<BeaconException>(new Action(() =>
            {
                var client = new NetMQClientContext("Test",9999,"192.168.50.111").Search();
            }));
            
        }
        [TestMethod]
        public void Context_AlphabetInIP()
        {
            Assert.ThrowsException<BeaconException>(new Action(() =>
            {

                var client = new NetMQClientContext( "Test", 9999, "192.168.50.a").Search();

            }));
        }
        [TestMethod]
        public void Context_WrongAlias()
        {
            var a=Assert.ThrowsException<NetMQClientException>(new Action(() =>
            {
                var client = new NetMQClientContext("hTest").Search();
            }));

        }


        public class TestService : DeviceBase
        {
            public Command<string> TestCommand { get; } = new Command<string>("WalkyTalky", null);
            public Command<double[]> QueryCommand { get; } = new Command<double[]>("ArrayData", null);
            public Command Command { get; } = new Command("Test");
            public string Test()
            {
                return "Test";
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

    [TestClass]
    public class ClientCommunicationTest
    {
        public TestContext TestContext { get; set; }
        private static TestService svrc;
        private string ip;
        [ClassInitialize]
        public static void GlobalSetup(TestContext context)
        {
            NetMQDataBus.CleanUp(false);
            svrc = new TestService();
            svrc.LoadDataBus(new NetMQDataBusContext()
            {
                AliasName = "Test",
            }); ;

        }
        [ClassCleanup]
        public static void Cleanup()
        {
            svrc.Dispose();
            NetMQDataBus.CleanUp(true);

        }
        [TestInitialize]
        public void Initialize()
        {
            ip = Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString();
        }

        [TestCleanup]
        public void TearDown()
        {

        }

        [TestMethod]
        public void Command_WrongMethodName()
        {
            IClient client = null;
            try
            {
                Assert.ThrowsException<Exception>(new Action(() =>
                {
                    client = new NetMQClientContext("Test") { ListeningIP=ip}.Search();
                    var ans = client.Query(new Command("Heo"));

                }));

            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                client?.Dispose();

            }
        }
        [TestMethod]
        public void Command_WrongParameterType()
        {
            IClient client = null;
            try
            {
                var a = Assert.ThrowsException<Exception>(new Action(() =>
                {
                    client = new NetMQClientContext("Test") { ListeningIP = ip }.Search();
                    client.Query(new Command<double, string, string, float>("WalkyTalky", 0, "", "", 0));

                }));

            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                client?.Dispose();

            }
        }

        [TestMethod]
        public void Command_VoidReturn()
        {
            IClient client = null;

            try
            {
                client = new NetMQClientContext("Test") { ListeningIP = ip }.Search();
                var ans = client.Query(TestService.NotReturnCommand);
                Assert.IsTrue(string.IsNullOrEmpty(ans));
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                client?.Dispose();
            }


        }

        [TestMethod]
        public void Command_ThrowExceptionOnExecution()
        {
            
            IClient client = null;
            try
            {
                Assert.ThrowsException<Exception>(new Action(() =>
                {
                    client = new NetMQClientContext("Test") { ListeningIP = ip }.Search();
                    var ans = client.Query(new Command("ThrowException"));
                }));
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                client?.Dispose();
            }
            
            
        }
        [TestMethod]
        public void MultipleClients()
        {
            IClient client = null;
            IClient client2 = null;
            try
            {
                client = new NetMQClientContext("Test") { ListeningIP = ip, ListeningPort = 1234 }.Search();
                Thread.Sleep(100);
                client2 = new NetMQClientContext("Test") { ListeningIP = ip,ListeningPort=5678 }.Search();
                string ans = "";
                var t = Task.Run(() => { ans = client.Query(TestService.SaySomething.Generate("1")); });
                var ans2 = client2.Query(TestService.SaySomething.Generate("2"));
                t.Wait();
                Assert.IsTrue(ans == "1" && ans2 == "2");

            }
            catch (Exception ex)
            {

                throw ex;
            }
            finally
            {
                client?.Dispose();
                client2?.Dispose();
            }

        }
        [TestMethod]
        public void MultipThreadSafeTest()
        {
            IClient client = null;
            try
            {
                client = new NetMQClientContext("Test") { ListeningIP = ip }.Search();

                SemaphoreSlim _pool = new SemaphoreSlim(10, 10);
                Task<string>[] tasks = new Task<string>[10];

                int _padding = -1;
                
                for (int i = 0; i < tasks.Length; i++)
                {
                    Random rand = new Random();
                    tasks[i] = Task.Run(() =>
                    {                        
                        _pool.Wait();
                        int padding = Interlocked.Add(ref _padding, 1);
                        var value = padding.ToString();
                        var ans =client.Query(new Command<string>("Hello", value));
                        _pool.Release();
                        return ans; 
                    });
                }
                Task.WaitAll(tasks);
                bool check = true;
                var sorted = tasks.Select(x => x.Result).ToArray();
                Array.Sort(sorted);
                for (int i = 0; i < sorted.Length; i++)
                {
                    check = check && sorted[i] == i.ToString();
                }

                    Assert.IsTrue(true);

            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                client?.Dispose();
            }
        }
        [TestMethod]
        public void ReconnectTest()
        {
            IClient client = null;
            try
            {
                client = new NetMQClientContext("Test") { ListeningIP = ip }.Search();
                var ans = client.Query(TestService.SaySomething.Generate("A"));                                 
                bool check = ans=="A";
                client.Dispose();
                client = null;
                Thread.Sleep(100);
                client = new NetMQClientContext("Test") { ListeningIP = ip }.Search();
                ans = client.Query(TestService.SaySomething.Generate("B"));
                check = check && ans == "B";
                Assert.IsTrue(check);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                client?.Dispose();
            }
        }

        public class TestService : DeviceBase
        {
            public static Command<string> TestCommand { get; } = new Command<string>("WalkyTalky", null);
            public static Command<double[]> QueryCommand { get; } = new Command<double[]>("ArrayData", null);
            public static Command Command { get; } = new Command("Test");
            public static Command NotReturnCommand { get; } = new Command("NotReturn");

            public static Command<string> SaySomething { get; } = new Command<string>("Hello","");
            public void NotReturn()
            {

            }

            public void ThrowException()
            {
                throw new Exception("SimulatedError");
            }
            public string Test()
            {
                return "Test";
            }

            public string Hello(string str)
            {
                return str;
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

}
