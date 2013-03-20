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
        public ZmqSocket MonitorChannel = null;
        private bool disposed;
        public static string PublishAddressClient = "tcp://localhost:5550";
        public static string PublishAddressServer = "tcp://*:5550";
        public static string SubscribeAddressServer =  "tcp://*:5553";//"inproc://back";
        public static string SubscribeAddressClient = "tcp://localhost:5553"; //"inproc://back";

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

        public void Start(ZmqContext context)
        {
            SetUpMonitorChannel(context);

            ////this should work but the forwarder device appears to be broken - it does not use XSUP and XPUB sockets
            ////ForwarderDevice = new ForwarderDevice(context, PublishAddressServer, SubscribeAddressServer, DeviceMode.Threaded);
            ////ForwarderDevice.Start();
            ////while (!ForwarderDevice.IsRunning)
            ////{ }

            QueueDevce = new QueueDevice(context, PubSubControlBackAddressServer, PubSubControlFrontAddress, DeviceMode.Threaded);
            QueueDevce.Start();
            while (!QueueDevce.IsRunning)
            { }

            Writeline("Control channel started");

            this.Context = context;
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            Task.Run(() =>
            {
                using (frontend = context.CreateSocket(SocketType.XSUB))
                {
                    using (backend = context.CreateSocket(SocketType.XPUB))
                    {
                        frontend.Bind(Pipe.PublishAddressServer); //"tcp://*:5550");
                        backend.Bind(Pipe.SubscribeAddressServer); //"tcp://*:5553");
                        frontend.ReceiveReady += new EventHandler<SocketEventArgs>(frontend_ReceiveReady);
                        backend.ReceiveReady += new EventHandler<SocketEventArgs>(backend_ReceiveReady);
                        Poller poller = new Poller(new ZmqSocket[] { frontend, backend });
                        
                        Writeline("About to start polling");
                        
                        while (true)
                        {
                            poller.Poll();

                            Writeline("polling");
                            if (token.IsCancellationRequested)
                                break;
                        }
                    }
                }
            }, token);
        }

        private bool Writeline(string line)
        {
            MonitorChannel.Send(line, Encoding.Unicode);
            var signal = MonitorChannel.Receive(Encoding.Unicode, new TimeSpan(0,0,5));
            if(string.IsNullOrEmpty(signal))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        void SetUpMonitorChannel(ZmqContext context)
        {
            MonitorChannel = context.CreateSocket(SocketType.REQ);
            MonitorChannel.Connect(Pipe.MonitorAddressClient);
        }

        public void Exit()
        {
            CleanUpDevices();
            cancellationTokenSource.Cancel();
        }

        static void backend_ReceiveReady(object sender, SocketEventArgs e)
        {
            WritelineToLogFile("frontend_ReceiveReady");
            e.Socket.Forward(frontend);
        }

        static void frontend_ReceiveReady(object sender, SocketEventArgs e)
        {
            WritelineToLogFile("frontend_ReceiveReady");
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
