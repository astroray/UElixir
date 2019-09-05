using System.Collections.Generic;
using NUnit.Framework;
using UElixir;
using UnityEngine;

namespace Tests
{
    public class JsonTests
    {
        private static Dictionary<int, NetworkTransform> m_testDictionary = new Dictionary<int, NetworkTransform>
        {
            { 32, new NetworkTransform { position = new Vector3(2.0f, 3.0f, 4.0f) } },
            { 22, new NetworkTransform { position = new Vector3(3.0f, 3.0f, 4.0f) } },
        };

        private const string m_serializedDictionary = @"{""32"":{""position"":{""x"":2.0,""y"":3.0,""z"":4.0}},""22"":{""position"":{""x"":3.0,""y"":3.0,""z"":4.0}}}";

        [Test]
        public void CanSerializeDictionary()
        {
            var json = JsonSerializer.SerializeToString(m_testDictionary);

            Debug.Log(json);

            Assert.That(json, Is.EqualTo(m_serializedDictionary));
        }

        [Test]
        public void DeserializeDictionary()
        {
            Dictionary<int, NetworkTransform> deserializedDictionary;
            deserializedDictionary = JsonSerializer.DeserializeFromString<Dictionary<int, NetworkTransform>>(m_serializedDictionary);

            Assert.That(deserializedDictionary, Is.EquivalentTo(m_testDictionary));
        }
    }
}