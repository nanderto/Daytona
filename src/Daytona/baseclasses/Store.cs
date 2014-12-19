using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daytona
{
    using System.IO;

    public class Store
    {
        private readonly ISerializer serializer;

        public Store(ISerializer serializer)
        {
            this.serializer = serializer;
        }

        public void Persist(Type type, object target, string filename)
        {
            var pathSegment = type.FullName;
            var line = this.serializer.GetString(serializer.GetBuffer(target));
            var fi = new FileInfo(string.Format(@"c:\dev\persistence\{0}.log", filename));
            var stream = fi.AppendText();
            stream.WriteLine("{0}~{1}", line, DateTime.Now);
            stream.Flush();
            stream.Close();
        }
    }
}
