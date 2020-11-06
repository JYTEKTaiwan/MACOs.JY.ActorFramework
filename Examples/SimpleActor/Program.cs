using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MACOs.JY.ActorFramework;

namespace SimpleActor
{
    class Program
    {
        static async Task Main(string[] args)
        {


            ActorFactory.EnableLogging();

            var dev = ActorFactory.Create<Machine>(true, "daq_device");
            dev.InternalCommType = InternalCommnucationModule.ConcurrentQueue;
            var t = await dev.ExecuteAsync<int>("Length", new double[] { 1, 2, 3, 4, 5 });
            Console.WriteLine(t);
            dev.Execute("Initial", 5);
            await dev.ExecuteAsync("ConfigureTiming", 10000, 100);
            dev.ExecuteAsync("Start");
            var data = await dev.ExecuteAsync<double[]>("ReadData");
            dev.Execute("Stop");
            Console.ReadKey();
            ActorFactory.StopAllActors();
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
