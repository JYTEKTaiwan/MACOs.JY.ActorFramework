using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            Stopwatch sw = new Stopwatch();
            sw.Restart();
            var dev = ActorFactory.Create<Machine>(true, "daq_device");
            Console.WriteLine("Create\t" + sw.ElapsedMilliseconds);

            for (int i = 0; i < 10; i++)
            {
                sw.Restart();
                var a = dev.Execute<int>("Len", new double[,] { { 1, 2, 3 }, { 4, 5, 6 } });
                Console.WriteLine("Complete\t" + sw.ElapsedMilliseconds);

            }
            Console.ReadKey();
            ActorFactory.StopAllActors();
        }
    }

    public class Machine:Actor
    {
        public static List<int> list = new List<int>();

        public static void Add(int i)
        {
            list.Add(i);
        }

        private double _sampleRate;
        private int _samples;
        private double[] data;

        [ActorCommand("Len")]
        private int Length(double[,] data)
        {
            return data.Length;
        }

        
        private void Initial(int boardID)
        {
        }

        private void ConfigureTiming(double sampleRate, int samples)
        {
            _sampleRate = sampleRate;
            _samples = samples;
        }

        private void Start()
        {
            data = new double[_samples];
            for (int i = 0; i < _samples; i++)
            {
                data[i] = 10.0 * Math.Sin(2 * Math.PI / 20 * i);
            }

        }
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

        private void Stop()
        {
        }


    }
}
