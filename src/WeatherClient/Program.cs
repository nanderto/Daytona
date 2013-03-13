using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroMQ;

namespace WeatherClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Collecting updates from weather server…");

            // default zipcode is 10001
            string zipcode = "10001 "; // the reason for having a space after 10001 is in case of the message would start with 100012 which we are not interested in

            if (args.Length > 0)
                zipcode = args[1] + " ";

            using (var context = ZmqContext.Create())
            {
                using (ZmqSocket subscriber = context.CreateSocket(SocketType.SUB))
                {
                    //subscriber.Subscribe(Encoding.Unicode.GetBytes(zipcode));
                    subscriber.SubscribeAll();
                    subscriber.Connect("tcp://localhost:5553");

                    const int updatesToCollect = 100;
                    int totalTemperature = 0;

                    for (int updateNumber = 0; updateNumber < updatesToCollect; updateNumber++)
                    {
                        string update = subscriber.Receive(Encoding.Unicode);
                        totalTemperature += Convert.ToInt32(update.Split()[1]);
                    }

                    Console.WriteLine("Average temperature for zipcode {0} was {1}F", zipcode, totalTemperature / updatesToCollect);
                    Console.ReadLine();
                }
            }



        }
    }
}
