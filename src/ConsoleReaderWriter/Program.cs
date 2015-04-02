namespace ConsoleReaderWriter
{
    using System;
    using System.Threading;

    using Daytona;

    using NetMQ;

    internal class Program
    {
        public static bool DontBreak = true;

        private static void Main(string[] args)
        {
            using (var silo = Silo.Create())
            {
                PrintInstructions();

                silo.RegisterEntity(typeof(ConsoleReaderActor));
                silo.RegisterEntity(typeof(ConsoleWriterActor));
                silo.Start();
                var reader = silo.ActorFactory.CreateInstance<IConsoleReaderActor>(typeof(ConsoleReaderActor));

                reader.Read();
                do
                {
                    Thread.Sleep(1000);
                }
                while (DontBreak);
                silo.Stop();
            }
            

            //using (var context = NetMQContext.Create())
            //{
            //    PrintInstructions();

            //    var exchange = new Exchange(context);
            //    exchange.Start();
            //    using (var silo = new Silo(context, new BinarySerializer()))
            //    {
            //        silo.RegisterEntity(typeof(ConsoleReaderActor));
            //        silo.RegisterEntity(typeof(ConsoleWriterActor));
            //        silo.Start();
            //        var reader = silo.ActorFactory.CreateInstance<IConsoleReaderActor>(typeof(ConsoleReaderActor));

            //        reader.Read();
            //        do
            //        {
            //            Thread.Sleep(1000);
            //        }
            //        while (DontBreak);
            //        silo.Stop();
            //    }

            //    exchange.Stop(true);
            //}
        }

        private static void PrintInstructions()
        {
            Console.WriteLine("Write whatever you want into the console!");
            Console.Write("Some lines will appear as");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write(" red ");
            Console.ResetColor();
            Console.Write(" and others will appear as");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(" green! ");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Type 'exit' to quit this application at any time.\n");
        }
    }
}