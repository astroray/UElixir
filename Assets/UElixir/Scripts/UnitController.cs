using UnityEngine;
using System.Collections;

namespace UElixir
{
    public class UnitController : MonoBehaviour
    {
        [SerializeField]
        private NetworkUnit m_controlledUnit;
        [SerializeField]
        private string m_horizontalAxis = "Horizontal";
        [SerializeField]
        private string m_verticalAxis = "Vertical";

        private float m_horizontalInput;
        private float m_verticalInput;

        private void Update()
        {
            m_horizontalInput = Input.GetAxis(m_horizontalAxis);
            m_verticalInput = Input.GetAxis(m_verticalAxis);
        }

        private void FixedUpdate()
        {
            var movable = m_controlledUnit.GetComponent<Movable>();

            movable.Move(m_verticalInput);
            movable.Rotate(m_horizontalInput);
        }
    }
}