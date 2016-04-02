using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DeliveryBoy
{
    public class JsonByteArraySerializer
    {
        private readonly JsonSerializerSettings settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };

        public byte[] Serialize<T>(T toSerialize)
        {
            var jsonString = JsonConvert.SerializeObject(toSerialize, settings);
            return Encoding.UTF8.GetBytes(jsonString);
        }

        public T Deserialize<T>(byte[] bytes)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes), settings);
        }
    }
}
