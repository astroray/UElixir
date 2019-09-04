using UnityEngine;
using System.Collections;

namespace UElixir
{
    public class MarkAsPersistent : MonoBehaviour
    {
        private static MarkAsPersistent m_instance;

        private void Awake()
        {
            if (m_instance == null)
            {
                m_instance = this;
                DontDestroyOnLoad(m_instance);
            }
            else if (m_instance != this)
            {
                Destroy(this);
            }
        }
    }
}