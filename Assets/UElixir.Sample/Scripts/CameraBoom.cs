using UnityEngine;
using System.Collections;

namespace UElixir
{
    [ExecuteInEditMode]
    public class CameraBoom : MonoBehaviour
    {
        [SerializeField]
        private Transform m_target;
        [SerializeField]
        private float m_length = 10.0f;
        [SerializeField]
        private Vector3 m_angle = Vector3.zero;

        private void Update()
        {
            if (!m_target)
            {
                return;
            }

            transform.position = CalculatePosition();
            transform.rotation = Quaternion.LookRotation(m_target.position - transform.position, Vector3.up);
        }

        private Vector3 CalculatePosition()
        {
            var delta = m_length * (m_target.rotation * Quaternion.Euler(m_angle) * Vector3.back);

            return m_target.position + delta;
        }

        private void OnDrawGizmos()
        {
            if (!m_target)
            {
                return;
            }

            Gizmos.color = Color.red;
            Gizmos.DrawLine(m_target.position, CalculatePosition());
        }
    }
}