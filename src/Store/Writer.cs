namespace Daytona.Store
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.Isam.Esent.Interop;
    using System.Diagnostics;
    using System.Globalization;
    using System.Threading;

    public class Writer
    {
        private Microsoft.Isam.Esent.Interop.Instance instance;

        public Writer(Instance instance)
        {
            this.instance = instance;
       
        }

        public Writer()
        {
            // TODO: Complete member initialization
        }

        public int Save<T>(DBPayload<T> dBPayload)
        {
            Writeline(dBPayload.ToString());
            return 1;
        }

        public int? Save<T>(byte[] messageAsBytes)
        {
            using (var store = new EsentStore<T>(this.instance))
            {
                var repository = new Repository<T>((IEsentStore<T>)store);

                var result = repository.SavePayload(messageAsBytes, Writer.CleanupName(typeof(T).ToString()));
                return result;
            }     
            //Writeline("Got message as bytes: " + messageAsBytes.ToString());
            //return 1;
        }

        private static string CleanupName(string dirtyname)
        {
            return dirtyname.ToString().Replace("{", string.Empty).Replace("}", string.Empty).Replace("_", string.Empty).Replace(".", string.Empty);
        }  

        public int Save(byte[] messageAsBytes, ISerializer serializer)
        {
            Writeline(serializer.GetString(messageAsBytes));
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

        internal int Save<T>(byte[] messageAsBytes, ISerializer serializer)
        {
            var result = Save<T>(messageAsBytes);
            int ret = -1;
            if (result != null)
            {
                ret = result.Value;
            }
            return ret;
        }

        public string Name { get; set; }
    }
}
