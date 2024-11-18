//序列化Helper类
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Scripts_AOT.Utility
{
    public static class JsonHelper
    {
        public static string ObjectToJson(object obj)
        {
            return obj.ToString();
        }
        // 反序列化
        public static T JsonToObject<T>(string strJson)
        {
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(strJson)))
            {
                DataContractJsonSerializer jsonSerialize = new DataContractJsonSerializer(typeof(T));
                return (T)jsonSerialize.ReadObject(stream);
            }
        }
    }
}