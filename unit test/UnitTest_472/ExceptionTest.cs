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

namespace UnitTest_472
{
    [TestClass]
    public class ServerExceptionTest
    {
        [TestMethod]
        public void DatabusNotLoaded()
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

            Assert.ThrowsException<ArgumentException>(new Action(() =>
            {
                var svrc = new TestService();

                svrc.LoadDataBus(new NetMQDataBusContext()
                {
                    IPAddress = "127.0.0.aa",
                    BeaconPort = 9999,
                    AliasName = "Test"
                }); ; ;
                svrc.Dispose();

            }));

        }

        [TestMethod]
        public void BroadcastException_BadString()
        {

            Assert.ThrowsException<ArgumentException>(new Action(() =>
            {
                var svrc = new TestService();

                svrc.LoadDataBus(new NetMQDataBusContext()
                {
                    IPAddress = "127.0.0.100",
                    BeaconPort = 9999,
                    AliasName = "Test"
                }); ; ;
                svrc.Dispose();

            }));
        }

        [TestMethod]
        public void BroadcastException_BadPort()
        {

            Assert.ThrowsException<ArgumentException>(new Action(() =>
            {
                var svrc = new TestService();

                svrc.LoadDataBus(new NetMQDataBusContext()
                {
                    IPAddress = "",
                    BeaconPort = 99999,
                    AliasName = "Test"
                }); ; ;
                svrc.Dispose();

            }));
        }

        [TestMethod]
        public void SocketException_AlphabetInIP()
        {

            Assert.ThrowsException<ArgumentException>(new Action(() =>
            {
                var svrc = new TestService();

                svrc.LoadDataBus(new NetMQDataBusContext()
                {
                    Port = 9999,
                    IPAddress = "",
                    BeaconPort = 9999,
                    AliasName = "Test"
                }); ; ;
                svrc.Dispose();

            }));
        }
        [TestMethod]
        public void SocketException_BadString()
        {

            Assert.ThrowsException<ArgumentException>(new Action(() =>
            {
                var svrc = new TestService();

                svrc.LoadDataBus(new NetMQDataBusContext()
                {
                    Port = 9999,
                    IPAddress = "192.168.20.55",
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
    public class ClientExceptionTest
    {
        private TestService svrc;
        [ClassInitialize]
        public static void GlobalSetup(TestContext context)
        {
        }
        [ClassCleanup]
        public static void Cleanup()
        {
        }
        [TestInitialize]
        public void Initialize()
        {
            NetMQDataBus.CleanUp(false);
            var ip = Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString();

            svrc = new TestService();
            svrc.LoadDataBus(new NetMQDataBusContext()
            {
                IPAddress = ip,
                BeaconPort = 9999,
                AliasName = "Test"
            });


        }

        [TestCleanup]
        public void TearDown()
        {
            svrc.Dispose();
            svrc = null;
            NetMQDataBus.CleanUp(false);

        }

        [TestMethod]
        public void Context_WrongPort()
        {
            Assert.ThrowsException<ArgumentException>(new Action(() =>
            {
                var client = new NetMQClientContext(99999, "Test", "192.168.50.64").Search();
            }));
        }

        [TestMethod]
        public void Context_WrongIP()
        {
            Assert.ThrowsException<ArgumentException>(new Action(() =>
            {
                var client = new NetMQClientContext(9999, "Test","192.168.50.111").Search();
            }));
            
        }
        [TestMethod]
        public void Context_AlphabetInIP()
        {
            Assert.ThrowsException<ArgumentException>(new Action(() =>
            {

                var client = new NetMQClientContext(9999, "Test","192.168.50.a").Search();

            }));
        }
        [TestMethod]
        public void Context_WrongAlias()
        {
            var a=Assert.ThrowsException<NullReferenceException>(new Action(() =>
            {
                var client = new NetMQClientContext(9999, "hTest", "192.168.50.64").Search();
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
        private TestService svrc;
        [ClassInitialize]
        public static void GlobalSetup(TestContext context)
        {
        }
        [ClassCleanup]
        public static void Cleanup()
        {
        }
        [TestInitialize]
        public void Initialize()
        {
            NetMQDataBus.CleanUp(false);
           var ip= Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(x=>x.AddressFamily== System.Net.Sockets.AddressFamily.InterNetwork).ToString();
            svrc = new TestService();
            svrc.LoadDataBus(new NetMQDataBusContext()
            {
                IPAddress = ip,
                BeaconPort = 9999,
                AliasName = "Test"
            });


        }

        [TestCleanup]
        public void TearDown()
        {
            svrc.Dispose();
            svrc = null;
            NetMQDataBus.CleanUp(false);
        }

        [TestMethod]
        public void Command_WrongMethodName()
        {
            IClient client = null;
            try
            {
                Assert.ThrowsException<Exception>(new Action(() =>
                {
                    client = new NetMQClientContext(9999, "Test","192.168.50.64").Search();
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
                    client = new NetMQClientContext(9999, "Test", "192.168.50.64").Search();
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
                client = new NetMQClientContext(9999, "Test", "192.168.50.64").Search();
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
                    client = new NetMQClientContext(9999, "Test", "192.168.50.64").Search();
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
                client = new NetMQClientContext(9999, "Test","192.168.50.64").Search();
                client2 = new NetMQClientContext(9999, "Test","192.168.50.64").Search();
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
                client = new NetMQClientContext(9999, "Test","192.168.50.64").Search();
                Task[] tasks = new Task[10];
                string[] answers = new string[10];
                for (int i = 0; i < tasks.Length; i++)
                {
                    tasks[i] = Task.Run(() => { answers[i] = client.Query(TestService.SaySomething.Generate(i.ToString())); });
                    tasks[i].Wait();
                }
                Task.WaitAll(tasks);
                bool check = true;
                for (int i = 0; i < answers.Length; i++)
                {
                    check = check & answers[i] == i.ToString();
                }
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
            client.Dispose();
        }
        [TestMethod]
        public void ReconnectTest()
        {
            IClient client = null;
            try
            {
                client = new NetMQClientContext(9999, "Test","192.168.50.64").Search();
                var ans = client.Query(TestService.SaySomething.Generate("A"));                                 
                bool check = ans=="A";
                client.Dispose();
                client = null;
                Thread.Sleep(100);
                client= new NetMQClientContext(9999, "Test","192.168.50.64").Search();
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
            client.Dispose();
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
