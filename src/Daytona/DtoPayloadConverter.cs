using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Daytona
{
    using Newtonsoft.Json.Converters;

    public class DtoPayloadConverter : CustomCreationConverter<IPayload>
    {
        public override IPayload Create(Type objectType)
        {
            throw new NotImplementedException();
        }
    }
}
