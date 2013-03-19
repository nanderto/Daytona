using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQ;
using ZeroMQ.Devices;

namespace Daytona
{
    public class Pipe : IDisposable
    {
        public ZmqContext Context { get; set; }
        private CancellationTokenSource cancellationTokenSource;
        public static ForwarderDevice ForwarderDevice = null;
        public static QueueDevice QueueDevce = null;
        private bool disposed;
        public static string PublishAddressClient = "tcp://localhost:5550";
        public static string  PublishAddressServer = "tcp://*:5550";
        public static string SubscribeAddressServer =  "tcp://*:5553";//"inproc://back";
        public static string SubscribeAddressClient = "tcp://localhost:5553";

        public static string PubSubControlFrontAddress = "tcp://*:5551";
        public static string PubSubControlFrontAddressClient = "tcp://localhost:5551";
        public static string PubSubControlBackAddressServer = "tcp://*:5552";//"inproc://pubsubcontrol";//
        public static string PubSubControlBackAddressClient = "tcp://localhost:5552";//"inproc://pubsubcontrol";//

        public static string MonitorAddressClient = "tcp://localhost:5560";//"inproc://pubsubcontrol";//
        public static string MonitorAddressServer = "tcp://*:5560";

        static ZmqSocket frontend, backend;

        public Pipe()
        {

        }

        public Pipe(ZmqContext context)
        {
            this.Context = context;
            Start(context);
        }

        //public Pipe(ZmqContext context)
        //{
        //    this.Context = context;
        //    Setup();
        //}

        public void Start(ZmqContext context)
        {
            //ForwarderDevice = new ForwarderDevice(context, PublishAddressServer, SubscribeAddressServer, DeviceMode.Threaded);
            //ForwarderDevice.Start();
            //while (!ForwarderDevice.IsRunning)
            //{ }

            QueueDevce = new QueueDevice(context, PubSubControlBackAddressServer, PubSubControlFrontAddress, DeviceMode.Threaded);
            QueueDevce.Start();
            while (!QueueDevce.IsRunning)
            { }

            this.Context = context;
            cancellationTokenSource = new CancellationTokenSource();

            Task.Run(() =>
            {
                //Setup(this.cancellationTokenSource.Token);
           
                using (frontend = context.CreateSocket(SocketType.XSUB))
                {
                    using (backend = context.CreateSocket(SocketType.XPUB))
                    {
                        frontend.Bind(Pipe.SubscribeAddressServer); //"tcp://*:5559");
                        backend.Bind(Pipe.PublishAddressServer); //"tcp://*:5560");
                        frontend.ReceiveReady += new EventHandler<SocketEventArgs>(frontend_ReceiveReady);
                        backend.ReceiveReady += new EventHandler<SocketEventArgs>(backend_ReceiveReady);
                        Poller poller = new Poller(new ZmqSocket[] { frontend, backend });
                        while (true)
                        {
                            poller.Poll();
                        }
                    }
                }
            });
        }

        public void Exit()
        {
            CleanUpDevices();

            cancellationTokenSource.Cancel();
        }

        static void backend_ReceiveReady(object sender, SocketEventArgs e)
        {
            e.Socket.Forward(frontend);
        }

        static void frontend_ReceiveReady(object sender, SocketEventArgs e)
        {
            e.Socket.Forward(backend);
        }


        private void CleanUpDevices()
        {
            if (QueueDevce != null)
            {
                if (QueueDevce.IsRunning)
                {
                    QueueDevce.Stop();
                    QueueDevce.Close();
                }
                QueueDevce.Dispose();
            }

            if (ForwarderDevice != null)
            {
                if (ForwarderDevice.IsRunning)
                {
                    ForwarderDevice.Stop();
                    ForwarderDevice.Close();
                }
                ForwarderDevice.Dispose();
            }
        }

        private void Setup(CancellationToken cancellationToken)
        {
            using (ZmqSocket frontend = this.Context.CreateSocket(SocketType.SUB), backend = this.Context.CreateSocket(SocketType.PUB))
            {
                frontend.Bind(PublishAddressServer);
                frontend.SubscribeAll();

                //  This is our public endpoint for subscribers
                backend.Bind(SubscribeAddressServer); 

                //  Shunt messages out to our own subscribers
                int i = 0;
                try
                {
                    while (true)
                    {
                        bool hasMore = true;
                        var zmqMessage = new ZmqMessage();
                        while (hasMore)
                        {
                            Writeline("waiting on receive frame");
                            Frame frame = frontend.ReceiveFrame();
 
                            zmqMessage.Append(new Frame(frame.Buffer));
                            hasMore = frontend.ReceiveMore;
                        }
                        i++;
                        Writeline(i.ToString());
                        backend.SendMessage(zmqMessage);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
                catch (Exception ex)
                {
                    Writeline("cancelled gracefully exit " + ex.Message);
                }
            }
        }

        public static void Writeline(string line)
        {
            FileInfo fi = new FileInfo(@"c:\dev\Pipe.log");
            var stream = fi.AppendText();
            stream.WriteLine(line);
            stream.Flush();
            stream.Close();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    CleanUpDevices();
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }
            disposed = true;
        }
    }
}
