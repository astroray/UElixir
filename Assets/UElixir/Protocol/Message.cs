using System;
using System.Text;
using Newtonsoft.Json;

namespace UElixir
{
    /// <summary>
    /// Represents the message from clients to server.
    /// </summary>
    [Serializable]
    public class Message
    {
        [JsonProperty("id")]
        public int UserId { get; set; }
        [JsonProperty("request")]
        public string Request { get; set; }
        [JsonProperty("arg")]
        public string Arg { get; set; }

        public override string ToString()
        {
            return $"{JsonSerializer.Serialize(this)}\r\n";
        }

        public byte[] ToByteArray()
        {
            return Encoding.UTF8.GetBytes(ToString());
        }
    }

}