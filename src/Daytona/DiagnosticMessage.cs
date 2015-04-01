using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Daytona
{
    public interface IDiagnosticMessage
    {
        void WriteToConsole(Exception exception, string message);

        void WriteToConsole(string message);
    }

    public class DiagnosticMessage : IDiagnosticMessage
    {
        public void WriteToConsole(Exception exception, string message)
        {
            this.WriteToConsole(message);
            Console.WriteLine(@"Exception:: {0} ", exception.Message);
        }

        public void WriteToConsole(string message)
        {
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine(@"Message:: {0} ", message);
        }
    }
}
