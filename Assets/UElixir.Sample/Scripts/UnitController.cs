using UnityEngine;
using System.Collections;

namespace UElixir.Sample
{
    public class UnitController : MonoBehaviour
    {
        [SerializeField]
        private Movable m_controlledUnit;
        [SerializeField]
        private string m_horizontalAxis = "Horizontal";
        [SerializeField]
        private string m_verticalAxis = "Vertical";

        private float m_horizontalInput;
        private float m_verticalInput;

        public Movable ControlledUnit { get => m_controlledUnit; set => m_controlledUnit = value; }

        private void Update()
        {
            m_horizontalInput = Input.GetAxis(m_horizontalAxis);
            m_verticalInput   = Input.GetAxis(m_verticalAxis);
        }

        private void FixedUpdate()
        {
            if (!ControlledUnit)
            {
                return;
            }

            ControlledUnit.Move(m_verticalInput);
            ControlledUnit.Rotate(m_horizontalInput);
        }
    }
}