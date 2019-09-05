using System;
using Newtonsoft.Json;

namespace UElixir
{
    public enum ERPCResult
    {
        Ok,
        Error
    }

    [Serializable]
    public class Response
    {
        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("result")]
        public ERPCResult Result { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}