using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using SimpleVoc;

namespace SimpleVocSpeed
{
    class Program
    {
        static void Main(string[] args)
        {
            // Default DefaultConnectionLimit is 2
            System.Net.ServicePointManager.DefaultConnectionLimit = 20;

            var con = new SimpleVocConnection("192.168.178.20", 8008);
                con.Flush();

            var max = 100;
            var taskList = new List<Task<bool>>();

            // Make 'max' requests syncronously
            var sw = StartMeasure(max, "Sync Set");

            for (int i = 0; i < max; i++)
            {
                con.Set(new SimpleVocValue()
                {
                    Key = "Key" + i,
                    Data = "Data"
                });
            }

            StopMeasure(max, sw);
            con.Flush();

            // Make 'max' requests asyncronously
            // Requests are limited by System.Net.ServicePointManager.DefaultConnectionLimit
            sw = StartMeasure(max, "Async Set");
            
            for (int i = 0; i < max; i++)
            {
                taskList.Add(con.SetAsync(new SimpleVocValue()
                {
                    Key = "Key" + i,
                    Data = "Data"
                }));
            }

            // Wait for all requests to complete
            TaskEx.WhenAll(taskList).Wait();
            StopMeasure(max, sw);

            Console.ReadLine();
        }

        private static void StopMeasure(int max, Stopwatch sw)
        {
            sw.Stop();
            Console.WriteLine("Finished {0} requests with {1} req/sec", max, Math.Round(max / sw.Elapsed.TotalSeconds, 2));
        }

        private static Stopwatch StartMeasure(int max, string text)
        {
            Console.WriteLine("Start measure {0} requests: {1}", max, text);

            return Stopwatch.StartNew();
        }
    }
}
