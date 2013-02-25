using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroMQ;
using ZeroMQ.Devices;

namespace Daytona
{
    public class Pipe
    {
        public ZmqContext Context { get; set; }

        public Pipe(ZmqContext context)
        {
            this.Context = context;
            Setup();
        }

        void Setup()
        {

            using (ZmqSocket frontend = this.Context.CreateSocket(SocketType.SUB), backend = this.Context.CreateSocket(SocketType.PUB))
            {
                frontend.Bind("tcp://*:5556");
                frontend.SubscribeAll();

                //  This is our public endpoint for subscribers
                backend.Bind("tcp://*:5555"); // i use local to be able to run the example, this could be the public ip instead eg. tcp://10.1.1.0:8100

                //var device = new ZeroMQ.Devices.ForwarderDevice(this.Context, "tcp://*:5556", "tcp://*:5555", DeviceMode.Blocking);
                //device.Start();
                //  Shunt messages out to our own subscribers
                while (true)
                {
                    bool hasMore = true;
                    while (hasMore)
                    {
                        string message = frontend.Receive(Encoding.Unicode);
                        hasMore = frontend.ReceiveMore;
                        backend.Send(message, Encoding.Unicode);
                    }
                }
            }
        }
    }
}
