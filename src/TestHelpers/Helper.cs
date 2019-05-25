using Daytona;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestHelpers
{
    using System.Net.Sockets;

    using NetMQ;

    public class Helper
    {
        public NetMQContext context { get; set; }
        ISerializer serializer;

        public Helper()
        {
            context = NetMQContext.Create();
        }

        public static void SendOneMessageOfType<T>(string Address, T message, ISerializer serializer, NetMQSocket publisher)
        {
            NetMQMessage NetMQMessage = new NetMQMessage();
            NetMQMessage.Append(new NetMQFrame(serializer.GetBuffer(Address)));
            NetMQMessage.Append(new NetMQFrame(serializer.GetBuffer(message)));
            publisher.SendMessage(NetMQMessage);
        }

        public static void SendOneSimpleMessage(string address, string message, NetMQSocket publisher)
        {
            {
                NetMQMessage NetMQMessage = new NetMQMessage();
                NetMQMessage.Append(new NetMQFrame(Encoding.Unicode.GetBytes(address)));
                NetMQMessage.Append(new NetMQFrame(Encoding.Unicode.GetBytes(message)));
                publisher.SendMessage(NetMQMessage);
                //publisher.Send("hello", Encoding.Unicode);
            }
        }

        public static T ReceiveMessageofType<T>(NetMQSocket sub)
        {
            string address = string.Empty;
            NetMQMessage message = null;
            return ReceiveMessage<T>(sub, out message, out address);
        }

        //public void SendOneMessageOfType<T>(string Address, T message, ISerializer serializer, NetMQSocket publisher)
        //{
        //    NetMQMessage NetMQMessage = new NetMQMessage();
        //    NetMQMessage.Append(new NetMQFrame(Encoding.Unicode.GetBytes(Address)));
        //    NetMQMessage.Append(new NetMQFrame(serializer.GetBuffer(message)));
        //    publisher.SendMessage(NetMQMessage);
        //}


        public static T ReceiveMessage<T>(NetMQSocket Subscriber, out NetMQMessage NetMQMessage, out string address)
        {
            T result = default(T);
            NetMQMessage zmqOut = new NetMQMessage();
            bool hasMore = true;
            string message = "";
            address = string.Empty;
            int i = 0;
            
            var buffer = Subscriber.Receive(out hasMore);

            while (hasMore)
            {
                
                if (i == 0)
                {
                    address = Encoding.Unicode.GetString(buffer);
                }
                if (i == 1)
                {
                    result = (T)JsonConvert.DeserializeObject<T>(Encoding.Unicode.GetString(buffer));
                }

                i++;
                zmqOut.Append(new NetMQFrame(Encoding.Unicode.GetBytes(message)));
                buffer = Subscriber.Receive(out hasMore);
            }

            NetMQMessage = zmqOut;
            return result;
        }

        public static NetMQMessage ReceiveMessage(NetMQSocket Subscriber)
        {
            var NetMQMessage = new NetMQMessage();
            bool hasMore = true;
            int i =0;
            string address = string.Empty;

            byte[] buffer = null; 

            while (hasMore)
            {
                buffer = Subscriber.Receive(out hasMore);

                if (buffer.Count() > 0)
                {
                    if (i == 0)
                    {
                        address = Encoding.Unicode.GetString(buffer);
                    }
                    if (i == 1)
                    {
                        string stop = Encoding.Unicode.GetString(buffer);
                        //result = serializer.Deserializer<T>(stop);    
                    }
                    i++;
                    NetMQMessage.Append(new NetMQFrame(buffer));

                    
                    //message = Subscriber.Receive(Encoding.Unicode,);

                    //NetMQMessage.Append(new NetMQFrame(Encoding.Unicode.GetBytes(message)));
                    //hasMore = Subscriber.ReceiveMore;
                }
                else
                {
                    NetMQMessage = null;
                }
            }

            return NetMQMessage;
        }

        public static NetMQSocket GetConnectedPublishSocket(NetMQContext context)
        {
            return GetConnectedPublishSocket(context, Pipe.PublishAddressClient);
        }

        public static NetMQSocket GetConnectedPublishSocket(NetMQContext context, string address)
        {
            NetMQSocket publisher = context.CreatePublisherSocket();

            try
            {
                publisher.Connect(address);
            }
            catch (Exception)
            {
                publisher.Close();
                publisher.Dispose();
                publisher = null;
            }
            return publisher;
        }

        public static NetMQSocket GetConnectedSubscribeSocket(NetMQContext context)
        {
            string address = Pipe.SubscribeAddressClient;
            return GetConnectedSubscribeSocket(context, address);
        }

        public static NetMQSocket GetConnectedSubscribeSocket(NetMQContext context, string address)
        {
            return GetConnectedSubscribeSocket(context, address, string.Empty);//string.Empty subscribes to all messages on the address
        }

        public static NetMQSocket GetConnectedSubscribeSocket(NetMQContext context, string address, string Subcription)
        {
            NetMQSocket subscriber = context.CreateSubscriberSocket();
            try
            {
                subscriber.Connect(address);
                subscriber.Subscribe(Subcription);
            }
            catch
            {
                subscriber.Close();
                subscriber.Dispose();
                subscriber = null;
            }
            return subscriber;
        }

        public static NetMQSocket GetBoundSubscribeSocket(NetMQContext context, string address)
        {
            NetMQSocket subscriber = context.CreateSubscriberSocket();
            subscriber.Bind(address);
            subscriber.Subscribe(string.Empty);
            return subscriber;
        }

        public static NetMQSocket GetBoundSubscribeSocket(NetMQContext context)
        {
            string address = "tcp://*:5555";
            return GetBoundSubscribeSocket(context, address);
        }
        //public static void SendOneSimpleMessage(string address, string message, NetMQSocket publisher)
        //{
        //    {
        //        NetMQMessage NetMQMessage = new NetMQMessage();
        //        NetMQMessage.Append(new NetMQFrame(Encoding.Unicode.GetBytes(address)));
        //        NetMQMessage.Append(new NetMQFrame(Encoding.Unicode.GetBytes(message)));
        //        publisher.SendMessage(NetMQMessage);
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
