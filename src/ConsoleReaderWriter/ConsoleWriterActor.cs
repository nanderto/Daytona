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
        void WriteOutput(object input);
    }

    public class ConsoleWriterActor : IConsoleWriterActor
    {
        private IConsoleReaderActor reader = null;


        public void WriteOutput(object message)
        {
            if (message is Messages.InputError)
            {
                var msg = message as Messages.InputError;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(msg.Reason);
            }
            else if (message is Messages.InputSuccess)
            {
                var msg = message as Messages.InputSuccess;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(msg.Reason);
            }
            else
            {
                Console.WriteLine(message);
            }

            Console.ResetColor();

            if (reader == null)
            {
                ///reader = this.Factory.CreateInstance<IConsoleReaderActor>(typeof(ConsoleReaderActor));
            }

            reader.ReadAgain();
        }
    }
}
