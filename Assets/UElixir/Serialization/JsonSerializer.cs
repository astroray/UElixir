using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using QuaternionConverter = UElixir.Serialization.QuaternionConverter;

namespace UElixir
{
    public static class JsonSerializer
    {
        public static string Serialize<T>(T value)
        {
            return Serialize(value, typeof(T));
        }

        public static string Serialize(object value, Type type)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.None
            };

            if (type == typeof(Vector3))
            {
                settings.Converters.Add(new VectorConverter());
            }

            if (type == typeof(Quaternion))
            {
                settings.Converters.Add(new QuaternionConverter());
            }

            return JsonConvert.SerializeObject(value, type, settings);
        }

        public static T Deserialize<T>(string json)
        {
            var type = typeof(T);

            return (T) Deserialize(json, type);
        }

        public static object Deserialize(string json, Type type)
        {
            if (type == typeof(Vector3))
            {
                return JsonConvert.DeserializeObject(json, type, new VectorConverter());
            }

            if (type == typeof(Quaternion))
            {
                return JsonConvert.DeserializeObject(json, type, new QuaternionConverter());
            }

            return JsonConvert.DeserializeObject(json, type);
        }
    }
}