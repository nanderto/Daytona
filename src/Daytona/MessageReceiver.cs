using System.Text;
using ZeroMQ;

namespace Daytona
{
    public class MessageReceiver
    {
        public int ReceiveMessage(ZmqSocket subscriber)
        {
            bool hasMore = true;
            var address = string.Empty;
            int i = 0;
            int retValue = 0;
            while (hasMore)
            {
                Frame frame = subscriber.ReceiveFrame();
                if (i == 0)
                {
                    address = Encoding.Unicode.GetString(frame.Buffer);
                }

                if (i == 1)
                {
                    byte[] messageAsBytes = frame.Buffer;
                    string message = Encoding.Unicode.GetString(messageAsBytes);
                    if (message == "ADDSUBSCRIBER")
                    {
                        retValue = 1;
                    }
                    else
                    {
                        retValue = -1;
                    }

                }

                i++;
                hasMore = subscriber.ReceiveMore;                
            }

            //zmqMessage = zmqOut;
            return retValue;
        }
    }
}