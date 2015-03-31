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
        public void Read()
        {
            var read = Console.ReadLine();
            if (!string.IsNullOrEmpty(read) && String.Equals("exit", read, StringComparison.OrdinalIgnoreCase))
            {
                // shut down the system (acquire handle to system via
                // this actors context)
                Program.DontBreak = false;
            }
            else
            {
                var writer = this.Factory.CreateInstance<IConsoleWriterActor>(typeof(ConsoleWriterActor));
                writer.WriteOutput(read);
            }
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
