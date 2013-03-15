using Daytona;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQ;
using ZeroMQ.Devices;
using ZeroMQ.Interop;

namespace PipeRunner
{
    class Program
    {
        static long msgCptr = 0;
        static int msgIndex = 0;
        static bool interrupted = false;
        private static int nbSubscribersConnected;
        static ZmqSocket frontend, backend;

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

                //frontend = context.CreateSocket(SocketType.XSUB);
                //backend = context.CreateSocket(SocketType.XPUB);
                    
                //frontend.Bind(Pipe.SubscribeAddressServer); //"tcp://*:5559");
                ////frontend.SubscribeAll();
                //backend.Bind(Pipe.PublishAddressServer); //"tcp://*:5560");
                //frontend.ReceiveReady += new EventHandler<SocketEventArgs>(frontend_ReceiveReady);
                //backend.ReceiveReady += new EventHandler<SocketEventArgs>(backend_ReceiveReady);
                //Poller poller = new Poller(new ZmqSocket[] { frontend, backend });
                //while (true)
                //{
                //    poller.Poll();
                //}



                var pipe = new Pipe();
                pipe.Start(context);
                Console.WriteLine("enter to exit=>");
                input = Console.ReadLine();
                //pipe.Exit();

            }
        }

        static void backend_ReceiveReady(object sender, SocketEventArgs e)
        {
            e.Socket.Forward(frontend);
        }

        static void frontend_ReceiveReady(object sender, SocketEventArgs e)
        {
            e.Socket.Forward(backend);
        }

        static string BuildDataToPublish()
        {
            if (msgCptr == long.MaxValue)
                msgCptr = 0;
            msgCptr++;
            if (12 >= 0)
                if (msgCptr > 12)
                    return "";
            if (msgIndex == altMessages.Count())
                msgIndex = 0;
            return altMessages[msgIndex++].Replace("#nb#", msgCptr.ToString("d2"));
        } 
        
        static string[] altMessages = "Orange #nb#;Apple  #nb#".Split(';');
        
        static void DisplayRepMsg(string msg)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(msg);
            Console.ForegroundColor = oldColor;
        }

        static void DisplayReqMsg(string msg)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg);
            Console.ForegroundColor = oldColor;
        }
    

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
