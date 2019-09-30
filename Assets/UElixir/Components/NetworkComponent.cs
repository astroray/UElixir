using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using JsonPropertyAttribute = Newtonsoft.Json.JsonPropertyAttribute;

namespace UElixir
{
    /// <summary>
    /// Represents the state of <see cref="NetworkComponent"/>
    /// </summary>
    /// <example>
    /// This will be serialized as json like
    /// {
    ///     "name": "NetworkTransform"
    ///     "properties": [
    ///         {
    ///             "name": "position",
    ///             "value": { "x": 0.3, "y": 83.5, "z": -33.0 }
    ///         },
    ///         {
    ///             "name": "rotation",
    ///             "value": { "x": 0.3, "y": 83.5, "z": -33.0, "w": 2.0 }
    ///         }
    ///     ]
    /// }
    /// </example>
    [Serializable]
    public struct NetworkComponentState
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("properties")]
        public List<NetworkComponentProperty> Properties { get; set; }
    }

    /// <summary>
    /// Represents the property of <see cref="NetworkComponent"/>
    /// </summary>
    /// <example>
    /// This will be serialized as json like
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

    /// <summary>
    /// Base class for all network components.
    /// </summary>
    [RequireComponent(typeof(NetworkEntity))]
    [DisallowMultipleComponent]
    public abstract class NetworkComponent : MonoBehaviour
    {
        private IDictionary<string, PropertyInfo> m_properties;

        internal bool          ShouldUpdate { get; private set; }
        public   NetworkEntity Entity       { get; private set; }

        /// <summary>
        /// Unity message <see cref="Awake"/>. If you override <see cref="Awake"/>, then should call base.<see cref="Awake"/>!
        /// </summary>
        protected virtual void Awake()
        {
            Entity = GetComponent<NetworkEntity>();

            m_properties = GetType().GetProperties()
                                    .Where(property => property.CustomAttributes.Any(attr => attr.AttributeType == typeof(ReplicableAttribute)))
                                    .ToDictionary(property => property.Name);
        }

        internal NetworkComponentState GetState()
        {
            if (Entity.HasLocalAuthority)
            {
                return OnGetState();
            }

            throw new InvalidOperationException("Remote authoritative entity should not send the state to server.");
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
            OnSetState(componentState, timeStamp);
        }

        protected virtual void OnSetState(NetworkComponentState componentState, int timeStamp)
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

        /// <summary>
        /// Unity message <see cref="Update"/>. If you override <see cref="Update"/>, then should call base.<see cref="Update"/>!
        /// </summary>
        protected virtual void Update()
        {
            if (Entity.HasLocalAuthority)
            {
                ShouldUpdate = ShouldUpdateProperty();
            }
        }

        /// <summary>
        /// Checks whether this component should report the state to server. Default is always true.
        /// </summary>
        protected virtual bool ShouldUpdateProperty()
        {
            return true;
        }
    }
}