using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleReaderWriter
{
    using System.Runtime.Remoting.Contexts;

    using Daytona;

    using Newtonsoft.Json;

    public interface IConsoleReaderActor
    {
        void Read();

        void ReadAgain();
    }

    [Serializable]
    public class ConsoleReaderActor : ActorFactory, IConsoleReaderActor
    {
        public const string ExitCommand = "exit"; 
        
        private IConsoleWriterActor writer = null;

        public void Read()
        {
            if (this.writer == null)
            {
                this.writer = this.Factory.CreateInstance<IConsoleWriterActor>(typeof(ConsoleWriterActor));
            }

            GetAndValidateInput(this.writer);

            //var read = Console.ReadLine();

            //if (!string.IsNullOrEmpty(read) && String.Equals("exit", read, StringComparison.OrdinalIgnoreCase))
            //{
            //    // shut down the system (acquire handle to system via
            //    // this actors context)
            //    Program.DontBreak = false;
            //}
            //else
            //{
            //    var writer = this.Factory.CreateInstance<IConsoleWriterActor>(typeof(ConsoleWriterActor));
            //    writer.WriteOutput(read);
            //}
        }

        private void GetAndValidateInput(IConsoleWriterActor writer)
        {
            var message = Console.ReadLine();
            if (string.IsNullOrEmpty(message))
            {
                // signal that the user needs to supply an input, as previously
                // received input was blank
                writer.WriteOutput(new Messages.NullInputError("No input received."));
            }
            else if (String.Equals(message, ExitCommand, StringComparison.OrdinalIgnoreCase))
            {
                // shut down the entire actor system (allows the process to exit)
                Program.DontBreak = false;
            }
            else
            {
                var valid = IsValid(message);
                if (valid)
                {
                    writer.WriteOutput(new Messages.InputSuccess("Thank you! Message was valid."));

                    // continue reading messages from console
                    //writer.WriteOutput(new Messages.ContinueProcessing());
                }
                else
                {
                    writer.WriteOutput(new Messages.ValidationError("Invalid: input had odd number of characters."));
                }
            }
        }

        /// <summary>
        /// Validates <see cref="message"/>.
        /// Currently says messages are valid if contain even number of characters.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private static bool IsValid(string message)
        {
            var valid = message.Length % 2 == 0;
            return valid;
        }

        [JsonIgnore]
        public override Actor Factory { get; set; }

        public string dummy { get; set; }

        public void ReadAgain()
        {
            this.Read();
        }
    }
}
