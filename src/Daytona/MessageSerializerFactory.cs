using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daytona
{
    public class MessageSerializerFactory
    {
        private readonly Func<ISerializer> func;

        public MessageSerializerFactory(Func<ISerializer> func)
        {
            this.func = func;
        }

        internal ISerializer GetNewSerializer()
        {
            return this.func.DynamicInvoke() as ISerializer;
        }
    }
}
