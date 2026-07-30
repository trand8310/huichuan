using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CefClient.Common
{
    public static class ExpandoObjectExtensions
    {
        public static JObject ToJson(this ExpandoObject expando)
        {
            return JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(expando));
        }
    }
}
