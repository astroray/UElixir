using System;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UElixir
{
    [Serializable]
    public class NetworkTransform : NetworkComponent
    {
        [SerializeField]
        private float m_positionThreshold = 0.01f;
        [SerializeField]
        private float m_rotationThreshold = 0.01f;
        [SerializeField]
        private float m_scaleThreshold = 0.01f;

        [Replicable, JsonConverter(typeof(VectorConverter))]
        public Vector3 Position { get => transform.position; set => transform.position = value; }
        [Replicable, JsonConverter(typeof(QuaternionConverter))]
        public Quaternion Rotation { get => transform.rotation; set => transform.rotation = value; }
        [Replicable, JsonConverter(typeof(VectorConverter))]
        public Vector3 Scale { get => transform.localScale; set => transform.localScale = value; }

        private float m_sqrPositionThreshold;

        private Vector3    m_currentPosition;
        private Quaternion m_currentRotation;
        private Vector3    m_currentScale;

        private Vector3    m_nextPosition;
        private Quaternion m_nextRotation;
        private Vector3    m_nextScale;

        private float m_timer;

        private void Start()
        {
            m_currentPosition = Position;
            m_currentRotation = Rotation;
            m_currentScale    = Scale;
            m_nextPosition    = Position;
            m_nextRotation    = Rotation;
            m_nextScale       = Scale;

            m_sqrPositionThreshold = m_positionThreshold * m_positionThreshold;
        }

        private void FixedUpdate()
        {
            if (Entity.HasLocalAuthority)
            {
                CheckThreshold();
            }
            else
            {
                UpdateTransform();
            }
        }

        private void UpdateTransform()
        {
            if (Mathf.Approximately(DeltaTime, 0.0f))
            {
                return;
            }

            var t = m_timer / DeltaTime;

            Position = Vector3.Lerp(m_currentPosition, m_nextPosition, t);
            Rotation = Quaternion.Lerp(m_currentRotation, m_nextRotation, t);
            Scale    = Vector3.Lerp(m_currentScale, m_nextScale, t);

            m_timer += Time.fixedDeltaTime;
        }

        private void CheckThreshold()
        {
            if ((Position - m_currentPosition).sqrMagnitude > m_sqrPositionThreshold)
            {
                ShouldUpdate = true;
            }
        }

        protected override NetworkComponentState OnGetState()
        {
            if (Entity.HasLocalAuthority)
            {
                m_currentPosition = Position;
            }

            return base.OnGetState();
        }

        protected override void OnSetState(NetworkComponentState componentState)
        {
            if (Entity.HasLocalAuthority)
            {
                base.OnSetState(componentState);
            }
            else
            {
                m_currentPosition = m_nextPosition;
                m_currentRotation = m_nextRotation;
                m_currentScale    = m_nextScale;

                foreach (var property in componentState.Properties)
                {
                    if (property.Name == "Position")
                    {
                        m_nextPosition = JsonSerializer.Deserialize<Vector3>(property.Value);
                    }
                    else if (property.Name == "Rotation")
                    {
                        m_nextRotation = JsonSerializer.Deserialize<Quaternion>(property.Value);
                    }
                    else if (property.Name == "Scale")
                    {
                        m_nextScale = JsonSerializer.Deserialize<Vector3>(property.Value);
                    }
                }

                base.OnSetState(componentState);

                m_timer = 0.0f;
            }
        }
    }
}