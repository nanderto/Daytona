using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Daytona.Store
{
    public class Writer
    {
        public int Save<T>(DBPayload<T> dBPayload)
        {
            Writeline(dBPayload.ToString());
            return 1;
        }

        public static void Writeline(string line)
        {
            FileInfo fi = new FileInfo(@"c:\dev\Store.log");
            var stream = fi.AppendText();
            stream.WriteLine(line);
            stream.Flush();
            stream.Close();
        }

    }
}
