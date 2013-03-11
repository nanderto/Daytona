using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Daytona.Store
{
    public class DBPayloadConverter : CustomCreationConverter<IPayload>
    {
        public override IPayload Create(Type objectType)
        {
            throw new NotImplementedException();
        }
    }
}
