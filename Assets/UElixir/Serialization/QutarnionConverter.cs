using System;
using UnityEngine;
using Newtonsoft.Json;

namespace UElixir.Serialization
{
    public class QuaternionConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Quaternion);
        }

        public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            var quaternion      = (Quaternion) value;
            var quaternionValue = new Vector4(quaternion.x, quaternion.y, quaternion.z, quaternion.w);

            serializer.Serialize(writer, quaternionValue, typeof(Vector4));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            var quaternionValue = (Vector4) serializer.Deserialize(reader, typeof(Vector4));

            return new Quaternion(quaternionValue.x, quaternionValue.y, quaternionValue.z, quaternionValue.w);
        }

        public override bool CanRead => true;
    }
}