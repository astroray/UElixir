using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using JsonPropertyAttribute = Newtonsoft.Json.JsonPropertyAttribute;

namespace UElixir
{
    [Serializable]
    public struct NetworkComponentState
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("properties")]
        public List<NetworkComponentProperty> Properties { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <example>
    /// {
    ///     "name": "position"
    ///     "value": "{ "x": 0.3, "y": 83.5, "z": -33.0 }"
    /// }
    /// </example>
    [Serializable]
    public struct NetworkComponentProperty
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
    }

    [RequireComponent(typeof(NetworkEntity))]
    public abstract class NetworkComponent : MonoBehaviour
    {
        /// <summary>
        /// Set this value to true if this component should send message to server and replicate its properties.
        /// </summary>
        public bool ShouldUpdate { get;    set; }
        public NetworkEntity Entity { get; set; }

        private int m_lastTimeStamp = -1;
        public int LastTimeStamp
        {
            get => m_lastTimeStamp;
            private set
            {
                if (m_lastTimeStamp == -1)
                {
                    m_lastTimeStamp = value;
                }
                else
                {
                    DeltaTime       = value - m_lastTimeStamp;
                    m_lastTimeStamp = value;
                }
            }
        }
        public float DeltaTime { get; private set; }

        private IDictionary<string, PropertyInfo> m_properties;

        private void Awake()
        {
            Entity = GetComponent<NetworkEntity>();

            m_properties = GetType().GetProperties()
                                    .Where(property => property.CustomAttributes.Any(attr => attr.AttributeType == typeof(ReplicableAttribute)))
                                    .ToDictionary(property => property.Name);
        }

        internal NetworkComponentState GetState()
        {
            return OnGetState();
        }

        protected virtual NetworkComponentState OnGetState()
        {
            var componentState = new NetworkComponentState
            {
                Name       = GetType().Name,
                Properties = new List<NetworkComponentProperty>()
            };

            foreach (var propertyInfo in m_properties.Values)
            {
                componentState.Properties.Add(new NetworkComponentProperty
                {
                    Name  = propertyInfo.Name,
                    Value = JsonSerializer.Serialize(propertyInfo.GetValue(this, null), propertyInfo.PropertyType)
                });
            }

            ShouldUpdate = false;

            return componentState;
        }

        internal void SetState(NetworkComponentState componentState, int timeStamp)
        {
            Assert.AreEqual(GetType().Name, componentState.Name);

            LastTimeStamp = timeStamp;
            OnSetState(componentState);
        }

        protected virtual void OnSetState(NetworkComponentState componentState)
        {
            foreach (var componentProperty in componentState.Properties)
            {
                if (m_properties.TryGetValue(componentProperty.Name, out var propertyInfo))
                {
                    propertyInfo.SetValue(this, JsonSerializer.Deserialize(componentProperty.Value, propertyInfo.PropertyType));
                }
                else
                {
                    Debug.LogError($"Property not exists : {componentProperty.Name}");
                }
            }
        }
    }
}