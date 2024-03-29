﻿using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace UElixir.Tests
{
    public class NetworkTransformTests
    {
        private Vector3          m_expectedPosition = new Vector3(42.0f, 42.0f, 42.0f);
        private Quaternion       m_expectedRotation = Quaternion.Euler(30.0f, 25.0f, -24.0f);
        private NetworkTransform m_networkTransform;

        [UnitySetUp]
        public IEnumerator SetupComponent()
        {
            var gameObject = new GameObject();
            m_networkTransform = gameObject.AddComponent<NetworkTransform>();
            m_networkTransform.Entity.HasLocalAuthority = true;

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator CleanUpComponent()
        {
            Object.Destroy(m_networkTransform.gameObject);

            yield return null;
        }

        [UnityTest]
        public IEnumerator GetStateTest()
        {
            m_networkTransform.Position = m_expectedPosition;
            m_networkTransform.Rotation = m_expectedRotation;

            yield return null;

            var state = m_networkTransform.GetState();

            foreach (var property in state.Properties)
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
            var state = new NetworkComponentState
            {
                Name = nameof(NetworkTransform),
                Properties = new List<NetworkComponentProperty>
                {
                    new NetworkComponentProperty { Name = nameof(NetworkTransform.Position), Value = JsonSerializer.Serialize(m_expectedPosition) },
                    new NetworkComponentProperty { Name = nameof(NetworkTransform.Rotation), Value = JsonSerializer.Serialize(m_expectedRotation) },
                }
            };

            yield return null;

            m_networkTransform.SetState(state, 0);

            yield return null;

            Assert.AreEqual(m_expectedPosition, m_networkTransform.Position);
            Assert.AreEqual(m_expectedRotation, m_networkTransform.Rotation);
        }
    }
}