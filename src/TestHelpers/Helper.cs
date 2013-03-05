using Daytona;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroMQ;

namespace TestHelpers
{
    public class Helper
    {
        public ZmqContext context { get; set; }
        ISerializer serializer;

        public Helper()
        {
            context = ZmqContext.Create();
        }

        public static void SendOneMessageOfType<T>(string Address, T message, ISerializer serializer, ZmqSocket publisher)
        {
            ZmqMessage zmqMessage = new ZmqMessage();
            zmqMessage.Append(new Frame(Encoding.Unicode.GetBytes(Address)));
            zmqMessage.Append(new Frame(serializer.GetBuffer(message)));
            publisher.SendMessage(zmqMessage);
        }

        public static void SendOneSimpleMessage(string address, string message, ZmqSocket publisher)
        {
            {
                ZmqMessage zmqMessage = new ZmqMessage();
                zmqMessage.Append(new Frame(Encoding.Unicode.GetBytes(address)));
                zmqMessage.Append(new Frame(Encoding.Unicode.GetBytes(message)));
                publisher.SendMessage(zmqMessage);
            }
        }

        public static T ReceiveMessageofType<T>(ZmqSocket sub)
        {
            string address = string.Empty;
            ZmqMessage message = null;
            return ReceiveMessage<T>(sub, out message, out address);
        }

        //public void SendOneMessageOfType<T>(string Address, T message, ISerializer serializer, ZmqSocket publisher)
        //{
        //    ZmqMessage zmqMessage = new ZmqMessage();
        //    zmqMessage.Append(new Frame(Encoding.Unicode.GetBytes(Address)));
        //    zmqMessage.Append(new Frame(serializer.GetBuffer(message)));
        //    publisher.SendMessage(zmqMessage);
        //}


        public static T ReceiveMessage<T>(ZmqSocket Subscriber, out ZmqMessage zmqMessage, out string address)
        {
            T result = default(T);
            ZmqMessage zmqOut = new ZmqMessage();
            bool hasMore = true;
            string message = "";
            address = string.Empty;
            int i = 0;
            while (hasMore)
            {
                Frame frame = Subscriber.ReceiveFrame();
                if (i == 0)
                {
                    address = Encoding.Unicode.GetString(frame.Buffer);
                }
                if (i == 1)
                {
                    result = (T)JsonConvert.DeserializeObject<T>(Encoding.Unicode.GetString(frame.Buffer));
                }

                i++;
                zmqOut.Append(new Frame(Encoding.Unicode.GetBytes(message)));
                hasMore = Subscriber.ReceiveMore;
            }

            zmqMessage = zmqOut;
            return result;
        }

        public static ZmqMessage ReceiveMessage(ZmqSocket Subscriber)
        {
            var zmqMessage = new ZmqMessage();
            bool hasMore = true;
            string message = "";

            while (hasMore)
            {
                message = Subscriber.Receive(Encoding.Unicode);

                zmqMessage.Append(new Frame(Encoding.Unicode.GetBytes(message)));
                hasMore = Subscriber.ReceiveMore;
            }

            return zmqMessage;
        }

        public static ZmqSocket GetConnectedPublishSocket(ZmqContext context)
        {
            return GetConnectedPublishSocket(context, "tcp://localhost:5556");
        }

        public static ZmqSocket GetConnectedPublishSocket(ZmqContext context, string address)
        {
            ZmqSocket publisher = context.CreateSocket(SocketType.PUB);
            publisher.Connect(address);
            return publisher;
        }

        public static ZmqSocket GetConnectedSubscribeSocket(ZmqContext context)
        {
            string address = "tcp://localhost:5555";
            return GetConnectedSubscribeSocket(context, address);
        }

        public static ZmqSocket GetConnectedSubscribeSocket(ZmqContext context, string address)
        {
            ZmqSocket Subscriber = context.CreateSocket(SocketType.SUB);
            Subscriber.Connect(address);
            Subscriber.SubscribeAll();
            return Subscriber;
        }

        public static ZmqSocket GetBoundSubscribeSocket(ZmqContext context, string address)
        {
            ZmqSocket Subscriber = context.CreateSocket(SocketType.SUB);
            Subscriber.Bind(address);
            Subscriber.SubscribeAll();
            return Subscriber;
        }

        //public static void SendOneSimpleMessage(string address, string message, ZmqSocket publisher)
        //{
        //    {
        //        ZmqMessage zmqMessage = new ZmqMessage();
        //        zmqMessage.Append(new Frame(Encoding.Unicode.GetBytes(address)));
        //        zmqMessage.Append(new Frame(Encoding.Unicode.GetBytes(message)));
        //        publisher.SendMessage(zmqMessage);
        //    }
        //}

        public static void Writeline(string line)
        {
            Writeline(line, @"c:\dev\log.log");
        }

        public static void Writeline(string line, string path)
        {
            FileInfo fi = new FileInfo(path);
            var stream = fi.AppendText();
            stream.WriteLine(line);
            stream.Flush();
            stream.Close();
        }
    }
}
