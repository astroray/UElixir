using System;
using System.Text;
using Newtonsoft.Json;

namespace UElixir
{
    [Serializable]
    public class Message
    {
        [JsonProperty("id")]
        public int UserId { get; set; }
        [JsonProperty("request")]
        public string Request { get; set; }
        [JsonProperty("arg")]
        public string Arg { get; set; }

        [JsonIgnore]
        public bool RequireResponse { get; set; } = true;
        [JsonIgnore]
        public string Response { get; set; }

        public override string ToString()
        {
            return $"{JsonSerializer.SerializeToString(this)}\r\n";
        }

        public byte[] ToByteArray()
        {
            return Encoding.UTF8.GetBytes(ToString());
        }
    }

}