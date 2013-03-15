using Daytona;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroMQ;
using ZeroMQ.Devices;
using ZeroMQ.Interop;

namespace PipeRunner
{
    class Program
    {
        static bool interrupted = false;
        private static object nbSubscribersConnected;

        static void ConsoleCancelHandler(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            interrupted = true;
        }

        static void Main(string[] args)
        {
            var input = string.Empty;
            Console.CancelKeyPress += new ConsoleCancelEventHandler(ConsoleCancelHandler);

            using (var context = ZmqContext.Create())
            {

                //var ForwarderDevice = new ForwarderDevice(context, Pipe.PublishAddressServer, Pipe.SubscribeAddressServer, DeviceMode.Threaded);
                //ForwarderDevice.Start();
                //while (!ForwarderDevice.IsRunning)
                //{ }

                using (ZmqSocket frontend = context.CreateSocket(SocketType.XSUB), backend = context.CreateSocket(SocketType.XPUB))
                {
                    frontend.Bind(Pipe.PublishAddressServer); //"tcp://*:5559");
                    backend.Bind(Pipe.SubscribeAddressServer); //"tcp://*:5560");


                    frontend.ReceiveReady += new EventHandler<SocketEventArgs>(subSocket_ReceiveReady);
                    
                    var poller = new Poller(new ZmqSocket[] {frontend});

                    poller.AddSocket(frontend);
                    while (true)
                    {
                        poller.Poll();
                    }
                    //var pollItems = new PollItem[2];
                    //pollItems[0] = frontend.CreatePollItem(IOMultiPlex.POLLIN);
                    //pollItems[0].PollInHandler += (socket, revents) => FrontendPollInHandler(socket, backend);
                    //pollItems[1] = backend.CreatePollItem(IOMultiPlex.POLLIN);
                    //pollItems[1].PollInHandler += (socket, revents) => BackendPollInHandler(socket, frontend);

                    //while (true)
                    //{
                    //    context.Poll(pollItems, -1);
                    //}
                }

                //var pipe = new Pipe();
                //pipe.Start(context);
                Console.WriteLine("enter to exit=>");
                input = Console.ReadLine();
                //pipe.Exit();

            }
        }

        static void pubSocket_ReceiveReady(object sender, SocketEventArgs e)
        {
            
            var reqMsg = e.Socket.Receive(Encoding.UTF8);
            Console.WriteLine("REP, received: " + reqMsg);

                   
        } 


        static void repSocket_SendReady(object sender, SocketEventArgs e)
        {
            Console.WriteLine("REP, sending: Sync OK");
            e.Socket.Send(Encoding.UTF8.GetBytes("Sync OK"));
            //nbSubscribersConnected++;
        } 


        private static void FrontendPollInHandler(ZmqSocket frontend, ZmqSocket backend)
        {
            RelayMessage(frontend, backend);
        }

        //private static void BackendPollInHandler(ZmqSocket backend, ZmqSocket frontend)
        //{
        //    RelayMessage(backend, frontend);
        //}

        private static void RelayMessagex(ZmqSocket source, ZmqSocket destination)
        {
            bool hasMore = true;
            while (hasMore)
            {
                // side effect warning!
                // note! that this uses Recv mode that gets a byte[], the router c# implementation
                // doesnt work if you get a string message instead of the byte[] i would prefer the solution thats commented.
                // but the router doesnt seem to be able to handle the response back to the client
                //string message = source.Recv(Encoding.Unicode);
                //hasMore = source.RcvMore;
                //destination.Send(message, Encoding.Unicode, hasMore ? SendRecvOpt.SNDMORE : SendRecvOpt.NONE);

                byte[] message = source.ReceiveFrame();
                hasMore = source.ReceiveMore;
                destination.Send(message, message.Length, hasMore ? SocketFlags.SendMore : SocketFlags.None);
            }
        }

        private static EventHandler<SocketEventArgs> RelayMessage(ZmqSocket source, ZmqSocket destination)
        {
            bool hasMore = true;
            while (hasMore)
            {
                // side effect warning!
                // note! that this uses Recv mode that gets a byte[], the router c# implementation
                // doesnt work if you get a string message instead of the byte[] i would prefer the solution thats commented.
                // but the router doesnt seem to be able to handle the response back to the client
                //string message = source.Recv(Encoding.Unicode);
                //hasMore = source.RcvMore;
                //destination.Send(message, Encoding.Unicode, hasMore ? SendRecvOpt.SNDMORE : SendRecvOpt.NONE);

                byte[] message = source.ReceiveFrame();
                hasMore = source.ReceiveMore;
                destination.Send(message, message.Length, hasMore ? SocketFlags.SendMore : SocketFlags.None);
            }
            return null;
        }


    }
}
