using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Assertions;

namespace UElixir
{
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
    public class NetworkEntity : MonoBehaviour
    {
        [SerializeField]
        private bool m_hasLocalAuthority;

        public bool HasLocalAuthority { get => m_hasLocalAuthority; internal set => m_hasLocalAuthority = value; }
        public Guid NetworkId         { get;                        internal set; }
        public bool ShouldUpdate
        {
            get
            {
                if (NetworkId == Guid.Empty)
                {
                    return false;
                }

                return m_networkComponents.Any(component => component.Value.ShouldUpdate);
            }
        }

        private Dictionary<string, NetworkComponent> m_networkComponents = new Dictionary<string, NetworkComponent>();

        private void Awake()
        {
            m_networkComponents = GetComponentsInChildren<NetworkComponent>().ToDictionary(component => component.GetType().Name);
        }

        private void OnDestroy()
        {
            NetworkManager.Instance.UnregisterEntity(this);
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