namespace Daytona
{
    using System;
    using System.Text;

    using NetMQ;
    using NetMQ.Devices;

    public class Exchange : IDisposable
    {
        public XForwarder XForwarder = null;

        public static string MonitorAddressClient = "tcp://localhost:5560";  ////"inproc://pubsubcontrol";//

        public static string MonitorAddressServer = "tcp://*:5560";

        public static string PublishAddress = "inproc://PublishAddress";

        public static string PublishAddressClient = "tcp://localhost:5550";

        public static string PublishAddressServer = "tcp://*:5550";

        public static string PubSubControlBackAddress = "inproc://PubSubControlBackAddress";

        public static string PubSubControlBackAddressClient = "tcp://localhost:5552"; ////"inproc://pubsubcontrol";//

        public static string PubSubControlBackAddressServer = "tcp://*:5552"; ////"inproc://pubsubcontrol";//

        public static string PubSubControlFrontAddress = "inproc://PubSubControlFrontAddress";

        public static string PubSubControlFrontAddressServer = "tcp://*:5551";

        public static string PubSubControlFrontAddressClient = "tcp://localhost:5551";

        public static string SubscribeAddress = "inproc://SubscribeAddress"; ////"inproc://back";

        public static string SubscribeAddressClient = "tcp://localhost:5553"; ////"inproc://back";

        public static string SubscribeAddressServer = "tcp://*:5553"; ////"inproc://back";

        public static string SubscriberCountAddress = "SubscriberCountAddress";

        public static Encoding ControlChannelEncoding = Encoding.Unicode;

        private bool disposed;

        public Exchange(NetMQContext context)
        {
            this.XForwarder = new XForwarder(context, PublishAddress, SubscribeAddress, DeviceMode.Threaded);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.XForwarder.Stop(true);
                }

                //// There are no unmanaged resources to release, but
                //// if we add them, they need to be released here.
            }

            this.disposed = true;
        }

        public void Start()
        {
            this.XForwarder.Start();
        }

        public void Stop(bool waitForCloseToComplete)
        {
            this.XForwarder.Stop(waitForCloseToComplete);
        }
    }
}