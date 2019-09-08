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
        [JsonProperty("request")]
        public string Request { get; set; }
        [JsonProperty("result")]
        public ERPCResult Result { get; set; }
        [JsonProperty("args")]
        public string Args { get; set; }
        [JsonProperty("time_stamp")]
        public int TimeStamp { get; set; }
    }
}