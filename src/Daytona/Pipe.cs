//-----------------------------------------------------------------------
// <copyright file="Pipe.cs" company="The Phantom Coder">
//     Copyright The Phantom Coder. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Daytona
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using NetMQ;
    using NetMQ.Devices;


    public class Pipe : IDisposable
    {
        public static ForwarderDevice forwarderDevice = null;

        public static string MonitorAddressClient = "tcp://localhost:5560";  ////"inproc://pubsubcontrol";//

        public static string MonitorAddressServer = "tcp://*:5560";

        public static string PublishAddressClient = "tcp://localhost:5550";

        public static string PublishAddressServer = "tcp://*:5550";

        public static string PubSubControlBackAddressClient = "tcp://localhost:5552"; ////"inproc://pubsubcontrol";//

        public static string PubSubControlBackAddressServer = "tcp://*:5552"; ////"inproc://pubsubcontrol";//

        public static string PubSubControlFrontAddress = "tcp://*:5551";

        public static string PubSubControlFrontAddressClient = "tcp://localhost:5551";

        public static QueueDevice QueueDevce = null;

        public static string SubscribeAddressClient = "tcp://localhost:5553"; ////"inproc://back";

        public static string SubscribeAddressServer = "tcp://*:5553"; ////"inproc://back";

        public static string SubscriberCountAddress = "SubscriberCountAddress";

        public static Encoding ControlChannelEncoding = Encoding.Unicode;

        public NetMQSocket MonitorChannel = null;

        private NetMQSocket AddSubscriberCountChannel = null;

        private static NetMQSocket frontend, backend;

        private CancellationTokenSource cancellationTokenSource;

        private bool disposed;

        private Poller poller;

        public Pipe()
        {
        }

        public Pipe(NetMQContext context)
        {
            this.Start(context);
        }

        //public ZmqContext Context { get; set; }

        public static void WritelineToLogFile(string line)
        {
            FileInfo fi = new FileInfo(@"c:\dev\Pipe.log");
            var stream = fi.AppendText();
            stream.WriteLine(line);
            stream.Flush();
            stream.Close();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Exit()
        {
            ////DONT SHUT DOWN UNTILL THE SUBSCRIBERS HAVE ALL SHUT DOWN
            //while (SubscriberCount > 0)
            //{

            //}
            //this.cancellationTokenSource.Cancel();
            this.CleanUpDevices();
        }

        public void Start(NetMQContext context)
        {
            this.SetUpMonitorChannel(context);
            this.SetUpAddSubscriberCountChannel(context);

            //this should work but the forwarder device appears to be broken - it does not use XSUb and XPUB sockets
            //forwarderDevice = new ForwarderDevice(context, PublishAddressServer, SubscribeAddressServer, DeviceMode.Threaded);
            //forwarderDevice.FrontendSetup.Subscribe(string.Empty);
            //forwarderDevice.Start();
            //while (!forwarderDevice.IsRunning)
            //{ }

            QueueDevce = new QueueDevice(context, PubSubControlBackAddressServer, PubSubControlFrontAddress, DeviceMode.Threaded);
            QueueDevce.Start();
            //while (!QueueDevce.IsRunning)
            //{
            //}

            this.Writeline("Control channel started");

            long count = 0;
            this.cancellationTokenSource = new CancellationTokenSource();
            var token = this.cancellationTokenSource.Token;
            Task.Run(() =>
            {
                using (frontend = context.CreateXSubscriberSocket())
                {
                    using (backend = context.CreateXPublisherSocket())
                    {
                        frontend.Bind(Pipe.PublishAddressServer); ////"tcp://*:5550");
                        backend.Bind(Pipe.SubscribeAddressServer); ////"tcp://*:5553");
                        // frontend.ReceiveReady += frontend_ReceiveReady;
                        frontend.ReceiveReady += new EventHandler<NetMQSocketEventArgs>(FrontendReceiveReady);
                        backend.ReceiveReady += new EventHandler<NetMQSocketEventArgs>(BackendReceiveReady);
                        // this.AddSubscriberCountChannel.ReceiveReady += new EventHandler<NetMQSocketEventArgs>(AddSubscriberCountChannelReceiveReady);
                        using (this.poller = new Poller(new NetMQSocket[] { frontend, backend, this.AddSubscriberCountChannel }))
                        {
                            Writeline("About to start polling");

                            while (true)
                            {
                                poller.Start(); // .PollOnce(); .Poll(new TimeSpan(0,0,0,0,5));
                                Writeline("polling" + count);
                                count++;
                                if (token.IsCancellationRequested)
                                {
                                    Writeline("break");
                                    break;
                                }
                            }
                        }

                        Writeline("stopped polling and exiting");
                    }
                }
            },
            token);
        }

        //private static void AddSubscriberCountChannelReceiveReady(object sender, NetMQSocketEventArgs e)
        //{
        //    WritelineToLogFile("AddSubscriberCountChannelReceiveReady");
        //    var messageReceiver = new MessageReceiver();
        //    SubscriberCount = SubscriberCount + messageReceiver.ReceiveMessage((NetMQSocket)sender);
        //}

        private static void BackendReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            WritelineToLogFile("BackendReceiveReady");
            // e.Socket.Send(frontend,);

            bool more;

            do
            {
                var data = e.Socket.Receive(out more);

                if (more)
                    frontend.SendMore(data);
                else
                {
                    frontend.Send(data);
                }

            } while (more);
        }

        private static void FrontendReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            WritelineToLogFile("FrontendReceiveReady");
            //e.Socket.Forward(backend);

            bool more;

            do
            {
                var data = e.Socket.Receive(out more);

                if (more)
                    backend.SendMore(data);
                else
                {
                    backend.Send(data);
                }

            } while (more);
        }

        private void CleanUpDevices()
        {
            if (QueueDevce != null)
            {
                if (QueueDevce.IsRunning)
                {
                    QueueDevce.Stop(true);
                    //QueueDevce. .Close();
                }

                //QueueDevce.Dispose();
            }

            //if (ForwarderDevice != null)
            //{
            //    if (ForwarderDevice.IsRunning)
            //    {
            //        ForwarderDevice.Stop(true);
            //        //ForwarderDevice. .Close();
            //    }

            //    //ForwarderDevice. .Dispose();
            //}

            if (this.MonitorChannel != null)
            {
                this.MonitorChannel.Dispose();
            }
            ////if (this.poller != null)
            ////{
            ////    this.poller.Dispose();
            ////}
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.CleanUpDevices();
                }

                //// There are no unmanaged resources to release, but
                //// if we add them, they need to be released here.
            }

            this.disposed = true;
        }

        private void SetUpMonitorChannel(NetMQContext context)
        {
            this.MonitorChannel = context.CreateRequestSocket();
            this.MonitorChannel.Connect(Pipe.MonitorAddressClient);
        }

        private void SetUpAddSubscriberCountChannel(NetMQContext zmqContext)
        {
            this.AddSubscriberCountChannel = zmqContext.CreateSubscriberSocket();
            this.AddSubscriberCountChannel.Connect(Pipe.SubscribeAddressClient);
            this.AddSubscriberCountChannel.Subscribe(Pipe.ControlChannelEncoding.GetBytes(Pipe.SubscriberCountAddress));
        }

        private bool Writeline(string line)
        {
            try
            {
                if (this.MonitorChannel != null)
                {
                    this.MonitorChannel.Send(line, Pipe.ControlChannelEncoding);
                    return ReadSignal();
                }
            }
            catch (Exception ex)
            {
                //Writeline(ex.ToString());
                if (ex.ToString() == "")
                {
                    return ReadSignal();
                }
            }

            return false;
        }

        private bool ReadSignal()
        {
            bool more = false;
            var signal = this.MonitorChannel.Receive(out more);
            if (signal == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static int SubscriberCount { get; set; }
    }
}