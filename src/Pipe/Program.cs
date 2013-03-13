﻿using Daytona;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroMQ;

namespace PipeRunner
{
    class Program
    {
        static bool interrupted = false;

        static void ConsoleCancelHandler(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            interrupted = true;
        }

        static void Main(string[] args)
        {
            var input = string.Empty;
            Console.CancelKeyPress += new ConsoleCancelEventHandler(ConsoleCancelHandler);

            using (var context = ZmqContext.Create())
            {
                var pipe = new Pipe();
                pipe.Start(context);

                Console.WriteLine("enter to exit=>");
                input = Console.ReadLine();
                pipe.Exit();

            }
        }
    }
}