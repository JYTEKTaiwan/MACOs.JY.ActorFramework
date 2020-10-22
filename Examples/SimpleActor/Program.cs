﻿using System;
using MACOs.JY.ActorFramework;

namespace SimpleActor
{
    class Program
    {
        static void Main(string[] args)
        {
            ActorFactory.EnableLogging(false);

               var dev=ActorFactory.Create<Machine>(false, "daq_device");
            dev.ExecuteAsync("Initial", 5);
            dev.ExecuteAsync("ConfigureTiming", 10000,100);
            dev.ExecuteAsync("Start");
            var data=dev.Execute<double[]>("ReadData");
            foreach (var item in data)
            {
                Console.WriteLine(item);
            }
            dev.Execute("Stop");
            Console.ReadKey();
            ActorFactory.StopAllActors();
        }
    }


    public class Machine:Actor
    {
        private double _sampleRate;
        private int _samples;
        private double[] data;
        [ActorCommand("Initial")]
        private void Initial( int boardID)
        {
            
        }

        [ActorCommand("ConfigureTiming")]
        private void ConfigureTiming(double sampleRate,int samples)
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
                data[i] = 10.0 * Math.Sin(2 * Math.PI / ratio * i)+(r.NextDouble()-0.5);
            }
            return data;
        }

        [ActorCommand("Stop")]
        private void Stop()
        {
        }


    }
}