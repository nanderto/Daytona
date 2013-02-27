using Daytona;
using System;
using System.Collections.Generic;
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
    }
}
