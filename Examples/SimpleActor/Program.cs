using System;
using System.IO;
using System.Threading;
using MACOs.JY.ActorFramework;

namespace SimpleActor
{
    class Program
    {
        static void Main(string[] args)
        {


            ActorFactory.Export();
            var test = new TestActor();
            test.StartService();
            //var test = new TestActor();
            test.Execute<DateTime>("Now");
            //DateTime dt = test.WhatTimeIsIt();
            //DateTime dt2 = test2.WhatTimeIsIt();
            //Console.WriteLine(dt);
            //Console.WriteLine(dt2);

            //ActorFactory.EnableLogging();

            //var dev=ActorFactory.Create<Machine>(false, "daq_device");
            //var ans=dev.Execute<int>("Length",new double[] { 1, 2, 3, 4, 5 });
            //Console.WriteLine(ans);
            ////dev.ExecuteAsync("Initial", 5);
            ////dev.ExecuteAsync("ConfigureTiming", 10000,100);
            ////dev.ExecuteAsync("Start");
            ////var data=dev.Execute<double[]>("ReadData");
            ////foreach (var item in data)
            ////{
            ////    Console.WriteLine(item);
            ////}
            //dev.Execute("Stop");
            Console.ReadKey();
            ActorFactory.StopAllActors();
        }
    }



    public class TestActor:Actor
    {
        [ActorCommand("Now")]
        public DateTime WhatTimeIsIt()
        {
            Thread.Sleep(1000);
            Console.WriteLine(DateTime.Now);
            return DateTime.Now;
        }

        public DateTime myMethod()
        {
            return DateTime.Now;
        }

        [ActorCommand("Welcome")]
        public string Hello()
        {
            return "Hello";
        }
    }


    public class Machine : Actor
    {
        private double _sampleRate;
        private int _samples;
        private double[] data;

        [ActorCommand("Length")]
        private int Length(double[] data)
        {
            return data.Length;
        }
        [ActorCommand("Initial")]
        private void Initial(int boardID)
        {

        }

        [ActorCommand("ConfigureTiming")]
        private void ConfigureTiming(double sampleRate, int samples)
        {
            _sampleRate = sampleRate;
            _samples = samples;
        }

        [ActorCommand("Start")]
        private void Start()
        {
            data = new double[_samples];
            for (int i = 0; i < _samples; i++)
            {
                data[i] = 10.0 * Math.Sin(2 * Math.PI / 20 * i);
            }

        }
        [ActorCommand("ReadData")]
        private double[] ReadData()
        {
            double ratio = _sampleRate / _samples;
            Random r = new Random();
            for (int i = 0; i < _samples; i++)
            {
                data[i] = 10.0 * Math.Sin(2 * Math.PI / ratio * i) + (r.NextDouble() - 0.5);
            }
            return data;
        }

        [ActorCommand("Stop")]
        private void Stop()
        {
        }


    }
}
