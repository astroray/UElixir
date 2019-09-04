using UnityEngine;
using System.Collections;

namespace UElixir
{
    [RequireComponent(typeof(Rigidbody))]
    public class Movable : MonoBehaviour
    {
        [SerializeField]
        private float m_moveSpeed = 1.5f;
        [SerializeField]
        private float m_rotateSpeed = 1.0f;

        private Rigidbody m_rigidbody;

        private void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody>();
        }

        public void Move(float value)
        {
            var nextVelocity = Time.fixedDeltaTime * m_moveSpeed * value * transform.forward;
            m_rigidbody.velocity = new Vector3(nextVelocity.x, m_rigidbody.velocity.y, nextVelocity.z);
        }

        public void Rotate(float value)
        {
            var nextRotation = Quaternion.Euler(0.0f, value, 0.0f) * m_rigidbody.rotation;
            m_rigidbody.rotation = nextRotation;
        }
    }
}