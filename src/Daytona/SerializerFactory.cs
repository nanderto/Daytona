using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Daytona
{
    public class SerializerFactory : ISerializerFactory
    {
        private readonly Func<ISerializer> returnSerializer = () => new DefaultSerializer(Pipe.ControlChannelEncoding);

        public SerializerFactory(Func<ISerializer> returnSerializerFunc )
        {
            returnSerializer = returnSerializerFunc;
        }

        public ISerializer GetSerializer()
        {
            return returnSerializer.Invoke();
        }
    }
}
