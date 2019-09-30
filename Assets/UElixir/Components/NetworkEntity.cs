using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using JsonPropertyAttribute = Newtonsoft.Json.JsonPropertyAttribute;

namespace UElixir
{
    /// <summary>
    /// Represents the state of <see cref="NetworkEntity"/>
    /// </summary>
    /// <example>
    /// This will be serialized to json like
    /// {
    ///     "entity_id": "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
    ///     "component_states": [
    ///         {
    ///             "name": "NetworkTransform"
    ///             "properties": [
    ///                 {
    ///                     "name": "position",
    ///                     "value": { "x": 0.3, "y": 83.5, "z": -33.0 }
    ///                 },
    ///                 {
    ///                     "name": "rotation",
    ///                     "value": { "x": 0.3, "y": 83.5, "z": -33.0, "w": 2.0 }
    ///                 }
    ///             ]
    ///         }
    ///     ]
    /// }
    /// </example>
    [Serializable]
    public struct NetworkEntityState
    {
        [JsonProperty("entity_id")]
        public string EntityId { get; set; }
        [JsonProperty("component_states")]
        public List<NetworkComponentState> ComponentStates { get; set; }
    }

    /// <summary>
    /// Represents unique entity on network.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkEntity : MonoBehaviour
    {
        [SerializeField]
        private bool m_hasLocalAuthority;

        private IDictionary<string, NetworkComponent> m_networkComponents;

        public bool HasLocalAuthority { get => m_hasLocalAuthority; internal set => m_hasLocalAuthority = value; }
        public Guid NetworkId         { get;                        internal set; }

        internal bool ShouldUpdate { get { return NetworkId != Guid.Empty && m_networkComponents.Any(component => component.Value.ShouldUpdate); } }

        private void Awake()
        {
            m_networkComponents = GetComponentsInChildren<NetworkComponent>().ToDictionary(component => component.GetType().Name);
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance)
            {
                NetworkManager.Instance.UnregisterEntity(this);
            }
        }

        internal NetworkEntityState GetState()
        {
            var entityState = new NetworkEntityState
            {
                EntityId        = NetworkId.ToString(),
                ComponentStates = new List<NetworkComponentState>()
            };

            foreach (var networkComponent in m_networkComponents.Values)
            {
                entityState.ComponentStates.Add(networkComponent.GetState());
            }

            return entityState;
        }

        internal void SetState(NetworkEntityState entityState, int timeStamp)
        {
            Assert.AreEqual(NetworkId, new Guid(entityState.EntityId));

            if (HasLocalAuthority)
            {
                return;
            }

            foreach (var componentState in entityState.ComponentStates)
            {
                if (m_networkComponents.TryGetValue(componentState.Name, out var networkComponent))
                {
                    networkComponent.SetState(componentState, timeStamp);
                }
                else
                {
                    Debug.LogError($"{componentState.Name} doesn't exist.");
                }
            }
        }
    }
}