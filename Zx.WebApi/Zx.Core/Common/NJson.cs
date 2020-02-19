using Newtonsoft.Json;

namespace Zx.Core.Common
{
    public class NJson
    {
        public static string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static T Deserialize<T>(object obj)
        {
            var json = Serialize(obj);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}

