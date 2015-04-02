using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daytona
{
    using System.IO;

    public class Store
    {
        private readonly ISerializer persistanceSerializer;

        public Store(ISerializer serializer)
        {
            this.persistanceSerializer = serializer;
        }

        public void Persist(Type type, object target, string filename)
        {
            var pathSegment = type.FullName;
            var line = this.persistanceSerializer.GetString(target);
            var fi = new FileInfo(string.Format(@"c:\dev\persistence\{0}.log", filename));
            var stream = fi.AppendText();
            stream.WriteLine("{0}~{1}", line, DateTime.Now);
            stream.Flush();
            stream.Close();
        }

        public T ReadfromPersistence<T>(string returnedAddress)
        {
            //string line = string.Empty;
            var line = File.ReadLines(string.Format(@"c:\Dev\Persistence\{0}.log", returnedAddress)).LastOrDefault();
            //using (var sr = new StreamReader(string.Format(@"c:\Dev\Persistence\{0}.log", returnedAddress)))
            //{
            //    line = sr.ReadLine();
            //}
            if (line != null)
            {
                var returnedRecord = line.Split('~');

                var target = this.persistanceSerializer.Deserializer<T>(returnedRecord[0]);
                return target;
            }


            return default(T);
        }
    }
}
