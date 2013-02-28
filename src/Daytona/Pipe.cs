using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQ;
using ZeroMQ.Devices;

namespace Daytona
{
    public class Pipe
    {
        public ZmqContext Context { get; set; }
        private CancellationTokenSource cancellationTokenSource;

        public Pipe()
        {

        }

        public Pipe(ZmqContext context)
        {
            this.Context = context;
            Setup();
        }

        public void Start(ZmqContext context)
        {
            this.Context = context;
            cancellationTokenSource = new CancellationTokenSource();

            Task.Run(() =>
            {
                Setup(this.cancellationTokenSource.Token); 
            });
        }

        public void Exit()
        {
            cancellationTokenSource.Cancel();
        }

        private void Setup(CancellationToken cancellationToken)
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
                int i = 0;
                try
                {
                    while (true)
                    {
                        bool hasMore = true;
                        var zmqMessage = new ZmqMessage();
                        while (hasMore)
                        {
                            string message = frontend.Receive(Encoding.Unicode);
                            //message = message + i.ToString();
                            //Console.WriteLine(message);
                            zmqMessage.Append(new Frame(Encoding.Unicode.GetBytes(message)));
                            hasMore = frontend.ReceiveMore;
                        }
                        i++;
                        backend.SendMessage(zmqMessage);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
                catch (Exception)
                {
                    //cancelled gracefully exit
                }
            }
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
                int i = 0;
                while (true)
                {
                    bool hasMore = true;
                    var zmqMessage = new ZmqMessage();
                    while (hasMore)
                    {
                        string message = frontend.Receive(Encoding.Unicode);
                        
                        Console.WriteLine(i.ToString());
                        zmqMessage.Append(new Frame(Encoding.Unicode.GetBytes(message)));
                        hasMore = frontend.ReceiveMore;       
                    }

                    backend.SendMessage(zmqMessage);
                }
            }
        }
    }
}
