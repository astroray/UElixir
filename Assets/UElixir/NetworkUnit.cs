using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace UElixir
{
    [Serializable]
    public struct NetworkTransform
    {
        [JsonConverter(typeof(VectorConverter))]
        public Vector3 position;

        public override string ToString()
        {
            return $"{JsonSerializer.SerializeToString(this)}";
        }

        public byte[] ToByteArray()
        {
            return Encoding.UTF8.GetBytes(ToString());
        }
    }

    public class NetworkUnit : MonoBehaviour
    {
        [SerializeField]
        private int m_networkId;
        [SerializeField]
        private bool m_owned;

        public int  NetworkId { get => m_networkId; set => m_networkId = value; }
        public bool Owned     { get => m_owned;     set => m_owned = value; }
        public NetworkTransform NetworkTransform
        {
            get
            {
                return new NetworkTransform
                {
                    position = transform.position
                };
            }
            set { transform.position = value.position; }
        }

    }
}