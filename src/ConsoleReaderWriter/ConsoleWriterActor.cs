using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleReaderWriter
{
    using Daytona;

    using Newtonsoft.Json;

    public interface IConsoleWriterActor
    {
        void WriteOutput(string input);
    }

    public class ConsoleWriterActor : ActorFactory, IConsoleWriterActor
    {
        [JsonIgnore]
        public override Actor Factory { get; set; }

        public void WriteOutput(string input)
        {
            var msg = input as string;

            // make sure we got a message
            if (string.IsNullOrEmpty(msg))
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("Please provide an input.\n");
                Console.ResetColor();
                return;
            }

            // if message has even # characters, display in red; else, green
            var even = msg.Length % 2 == 0;
            var color = even ? ConsoleColor.Red : ConsoleColor.Green;
            var alert = even ? "Your string had an even # of characters.\n" : "Your string had an odd # of characters.\n";
            Console.ForegroundColor = color;
            Console.WriteLine(alert);
            Console.ResetColor();
            Console.WriteLine("Go ahead and enter something new, and remember 'exit' to exit");
            var reader = this.Factory.CreateInstance<IConsoleReaderActor>(typeof(ConsoleReaderActor));
            reader.ReadAgain();
        }
    }
}
