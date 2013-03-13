using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroMQ;

namespace WeatherServer
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var context =  ZmqContext.Create())
            {
                using (ZmqSocket publisher = context.CreateSocket(SocketType.PUB))
                {
                    publisher.Connect("tcp://localhost:5550");

                    var randomizer = new Random(DateTime.Now.Millisecond);

                    while (true)
                    {
                        //  Get values that will fool the boss
                        int zipcode = randomizer.Next(0, 100000);
                        int temperature = randomizer.Next(-80, 135);
                        int relativeHumidity = randomizer.Next(10, 60);

                        string update = zipcode.ToString() + " " + temperature.ToString() + " " + relativeHumidity.ToString();

                        //  Send message to 0..N subscribers via a pub socket
                        publisher.Send(update, Encoding.Unicode);
                    }
                }
            }


        }
    }
}
