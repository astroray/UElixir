using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace UElixir.Tests
{
    public class NetworkEntityTests
    {
        private readonly Vector3       m_expectedPosition = new Vector3(42.0f, 42.0f, 42.0f);
        private readonly Quaternion    m_expectedRotation = Quaternion.Euler(30.0f, 25.0f, -24.0f);
        private          NetworkEntity m_networkEntity;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            var gameObject = new GameObject();
            gameObject.AddComponent<NetworkTransform>();

            m_networkEntity                   = gameObject.GetComponent<NetworkEntity>();
            m_networkEntity.NetworkId         = Guid.NewGuid();
            m_networkEntity.HasLocalAuthority = true;

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            Object.Destroy(m_networkEntity.gameObject);

            yield return null;
        }

        [UnityTest]
        public IEnumerator GetStateTest()
        {
            m_networkEntity.transform.position = m_expectedPosition;
            m_networkEntity.transform.rotation = m_expectedRotation;

            yield return null;

            var state = m_networkEntity.GetState();

            yield return null;

            Assert.AreEqual(m_networkEntity.NetworkId, new Guid(state.EntityId));
            Assert.AreEqual(nameof(NetworkTransform),  state.ComponentStates[0].Name);

            var transformState = state.ComponentStates[0];

            foreach (var property in transformState.Properties)
            {
                Debug.Log($"{property.Name} : {property.Value}");

                switch (property.Name)
                {
                    case "Position":
                        Assert.AreEqual(m_expectedPosition, JsonSerializer.Deserialize<Vector3>(property.Value));

                        break;
                    case "Rotation":
                        Assert.AreEqual(m_expectedRotation, JsonSerializer.Deserialize<Quaternion>(property.Value));

                        break;
                    default:
                        Assert.Fail();

                        break;
                }
            }
        }

        [UnityTest]
        public IEnumerator SetStateTest()
        {
            var state = new NetworkEntityState
            {
                EntityId = m_networkEntity.NetworkId.ToString(),
                ComponentStates = new List<NetworkComponentState>
                {
                    new NetworkComponentState
                    {
                        Name = nameof(NetworkTransform),
                        Properties = new List<NetworkComponentProperty>
                        {
                            new NetworkComponentProperty { Name = nameof(NetworkTransform.Position), Value = JsonSerializer.Serialize(m_expectedPosition) },
                            new NetworkComponentProperty { Name = nameof(NetworkTransform.Rotation), Value = JsonSerializer.Serialize(m_expectedRotation) },
                        }
                    }
                }
            };

            yield return null;

            m_networkEntity.HasLocalAuthority = false;
            m_networkEntity.SetState(state, 0);

            yield return null;

            Assert.AreEqual(m_expectedPosition, m_networkEntity.transform.position);
            Assert.AreEqual(m_expectedRotation, m_networkEntity.transform.rotation);
        }
    }
}