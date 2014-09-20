// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Phantom Coder">
//   
// </copyright>
// <summary>
//   Defines the Program type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace nProxySample
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

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

            RunNProxyTest(input);
        }

        private static string RunNProxyTest(string input)
        {
            //todo register an actor and start program

            Console.WriteLine("enter to exit=>");
            input = Console.ReadLine();

            return input;
        }
    }
}
