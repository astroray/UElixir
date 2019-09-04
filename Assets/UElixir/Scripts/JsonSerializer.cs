using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using Newtonsoft.Json.Serialization;

namespace UElixir
{
    public static class JsonSerializer
    {
        public static string SerializeToString(object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.None);
        }

        public static T DeserializeFromString<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}