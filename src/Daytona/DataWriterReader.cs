using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Daytona
{
    using System.IO;

    public class DataWriterReader
    {
        public virtual void PersistSelf(Type typeToBePersisted, object toBePersisted, ISerializer serializer)
        {
            if (serializer == null)
            {
                serializer = new DefaultSerializer(Pipe.ControlChannelEncoding);
            }

            var pathSegment = typeToBePersisted.FullName;

            this.WriteLineToSelf(serializer.GetString(serializer.GetBuffer(toBePersisted)), pathSegment);
        }

        public void WriteLineToSelf(string line, string PathSegment)
        {
            var fi = new FileInfo(string.Format(@"c:\dev\persistence\{0}.log", PathSegment));
            var stream = fi.AppendText();
            stream.WriteLine("{0}~{1}", line, DateTime.Now);
            stream.Flush();
            stream.Close();
        }
    }
}
