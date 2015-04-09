using Daytona;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PipeRunner
{
    using NetMQ;

    class Program
    {
        static long msgCptr = 0;
        static int msgIndex = 0;
        static bool interrupted = false;
        private static int nbSubscribersConnected;
        static NetMQSocket frontend, backend;

        static void ConsoleCancelHandler(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            interrupted = true;
        }

        static void Main(string[] args)
        {
            var input = string.Empty;
            Console.CancelKeyPress += new ConsoleCancelEventHandler(ConsoleCancelHandler);


            using (var silo = Silo.Create())
            {
                silo.Spawn("Johnny", actor =>
                    {
                        Console.WriteLine("here");
                        Console.WriteLine("hey is ther enything there##{0}##", actor.Name);
                        for (int i = 0; i < 5; i++)
                        {
                            Console.WriteLine("hey is ther enything there##{0}##{1}", actor.Name, i);
                        }
                    });
            }
            //using (var context = NetMQContext.Create())
            //{
            //    using (var pipe = new Pipe())
            //    {
            //        pipe.Start(context);
            //        Console.WriteLine("enter to exit=>");
            //        input = Console.ReadLine();
            //    }
            //}
        }

        //static void backend_ReceiveReady(object sender, SocketEventArgs e)
        //{
        //    e.Socket.Forward(frontend);
        //}

        //static void frontend_ReceiveReady(object sender, SocketEventArgs e)
        //{
        //    e.Socket.Forward(backend);
        //}

        //static string BuildDataToPublish()
        //{
        //    if (msgCptr == long.MaxValue)
        //        msgCptr = 0;
        //    msgCptr++;
        //    if (12 >= 0)
        //        if (msgCptr > 12)
        //            return "";
        //    if (msgIndex == altMessages.Count())
        //        msgIndex = 0;
        //    return altMessages[msgIndex++].Replace("#nb#", msgCptr.ToString("d2"));
        //} 
        
        //static string[] altMessages = "Orange #nb#;Apple  #nb#".Split(';');
        
        //static void DisplayRepMsg(string msg)
        //{
        //    var oldColor = Console.ForegroundColor;
        //    Console.ForegroundColor = ConsoleColor.White;
        //    Console.WriteLine(msg);
        //    Console.ForegroundColor = oldColor;
        //}

        //static void DisplayReqMsg(string msg)
        //{
        //    var oldColor = Console.ForegroundColor;
        //    Console.ForegroundColor = ConsoleColor.Yellow;
        //    Console.WriteLine(msg);
        //    Console.ForegroundColor = oldColor;
        //}
    

        //private static void RelayMessagex(ZmqSocket source, ZmqSocket destination)
        //{
        //    bool hasMore = true;
        //    while (hasMore)
        //    {
        //        // side effect warning!
        //        // note! that this uses Recv mode that gets a byte[], the router c# implementation
        //        // doesnt work if you get a string message instead of the byte[] i would prefer the solution thats commented.
        //        // but the router doesnt seem to be able to handle the response back to the client
        //        //string message = source.Recv(Encoding.Unicode);
        //        //hasMore = source.RcvMore;
        //        //destination.Send(message, Encoding.Unicode, hasMore ? SendRecvOpt.SNDMORE : SendRecvOpt.NONE);

        //        byte[] message = source.ReceiveFrame();
        //        hasMore = source.ReceiveMore;
        //        destination.Send(message, message.Length, hasMore ? SocketFlags.SendMore : SocketFlags.None);
        //    }
        //}

        //private static EventHandler<SocketEventArgs> RelayMessage(ZmqSocket source, ZmqSocket destination)
        //{
        //    bool hasMore = true;
        //    while (hasMore)
        //    {
        //        // side effect warning!
        //        // note! that this uses Recv mode that gets a byte[], the router c# implementation
        //        // doesnt work if you get a string message instead of the byte[] i would prefer the solution thats commented.
        //        // but the router doesnt seem to be able to handle the response back to the client
        //        //string message = source.Recv(Encoding.Unicode);
        //        //hasMore = source.RcvMore;
        //        //destination.Send(message, Encoding.Unicode, hasMore ? SendRecvOpt.SNDMORE : SendRecvOpt.NONE);

        //        byte[] message = source.ReceiveFrame();
        //        hasMore = source.ReceiveMore;
        //        destination.Send(message, message.Length, hasMore ? SocketFlags.SendMore : SocketFlags.None);
        //    }
        //    return null;
        //}


    }
}
