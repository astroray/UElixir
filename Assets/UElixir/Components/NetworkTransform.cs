using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine.Assertions;
using QuaternionConverter = UElixir.Serialization.QuaternionConverter;

namespace UElixir
{
    /// <summary>
    /// Network aware transform.
    /// Scale is ignored.
    /// </summary>
    public sealed class NetworkTransform : NetworkComponent
    {
        [SerializeField, Tooltip("Minimum difference position to send a message to server.")]
        private float m_positionThreshold = 0.01f;
        [SerializeField, Tooltip("Minimum difference angle to send a message to server.")]
        private float m_rotationThreshold = 0.01f;

        [Replicable, JsonConverter(typeof(VectorConverter))]
        public Vector3 Position { get => transform.position; set => transform.position = value; }
        [Replicable, JsonConverter(typeof(QuaternionConverter))]
        public Quaternion Rotation { get => transform.rotation; set => transform.rotation = value; }

        private float m_sqrPositionThreshold;

        private int        m_prevTimeStamp;
        private Vector3    m_prePosition;
        private Quaternion m_prevRotation;

        private int        m_nextTimeStamp;
        private Vector3    m_nextPosition;
        private Quaternion m_nextRotation;

        private float m_timer;

        private void Start()
        {
            m_prePosition  = Position;
            m_prevRotation = Rotation;
            m_nextPosition = Position;
            m_nextRotation = Rotation;

            m_sqrPositionThreshold = m_positionThreshold * m_positionThreshold;

            StartCoroutine(UpdateTransform());
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        private IEnumerator UpdateTransform()
        {
            while (true)
            {
                yield return new WaitForFixedUpdate();

                if (Entity.HasLocalAuthority
                    || m_nextTimeStamp <= m_prevTimeStamp)
                {
                    continue;
                }

                var duration = (m_nextTimeStamp - m_prevTimeStamp) * NetworkManager.Instance.TimeStep;

                if (m_timer > duration)
                {
                    Position = m_nextPosition;
                    Rotation = m_nextRotation;

                    continue;
                }

                var t = m_timer / duration;

                Position = Vector3.Lerp(m_prePosition, m_nextPosition, t);
                Rotation = Quaternion.Lerp(m_prevRotation, m_nextRotation, t);

                m_timer += Time.fixedDeltaTime;
            }
        }

        #region NetworkComponent interfaces
        protected override bool ShouldUpdateProperty()
        {
            if ((Position - m_prePosition).sqrMagnitude > m_sqrPositionThreshold)
            {
                return true;
            }

            if (Math.Abs(Quaternion.Angle(Rotation, m_prevRotation)) > m_rotationThreshold)
            {
                return true;
            }

            return false;
        }

        protected override void OnSetState(NetworkComponentState componentState, int timeStamp)
        {
            if (m_nextTimeStamp == 0)
            {
                // Incoming state is initial state
                m_prevTimeStamp = m_nextTimeStamp = timeStamp;

                SetNextProperties(componentState.Properties);
                Position = m_nextPosition;
                Rotation = m_nextRotation;

                return;
            }

            m_prevTimeStamp = m_nextTimeStamp;
            m_prePosition   = m_nextPosition;
            m_prevRotation  = m_nextRotation;

            m_nextTimeStamp = timeStamp;

            Assert.IsTrue(m_nextTimeStamp > m_prevTimeStamp, $"Prev : {m_prevTimeStamp}, Next : {m_nextTimeStamp}");

            SetNextProperties(componentState.Properties);

            m_timer = 0.0f;
        }

        private void SetNextProperties(IEnumerable<NetworkComponentProperty> properties)
        {
            foreach (var property in properties)
            {
                if (property.Name == "Position")
                {
                    m_nextPosition = JsonSerializer.Deserialize<Vector3>(property.Value);
                }
                else if (property.Name == "Rotation")
                {
                    m_nextRotation = JsonSerializer.Deserialize<Quaternion>(property.Value);
                }
            }
        }
        #endregion
    }
}