using System.Text;

namespace Daytona
{
    using NetMQ;

    public class MessageReceiver
    {
        public int ReceiveMessage(NetMQSocket subscriber)
        {
            bool hasMore = true;
            var address = string.Empty;
            int i = 0;
            int retValue = 0;

            var buffer = subscriber.Receive(out hasMore);

            while (hasMore)
            {
                
                if (i == 0)
                {
                    address = Encoding.Unicode.GetString(buffer);
                }

                if (i == 1)
                {
                    byte[] messageAsBytes = buffer;
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
                buffer = subscriber.Receive(out hasMore);                
            }

            //zmqMessage = zmqOut;
            return retValue;
        }
    }
}