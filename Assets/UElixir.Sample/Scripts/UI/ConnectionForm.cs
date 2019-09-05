using System;
using UnityEngine;
using System.Collections;
using System.Text;
using UnityEngine.UI;

namespace UElixir.Sample.UI
{
    public class ConnectionForm : UIElement
    {
        [SerializeField]
        private Text m_text;

        private const string m_defaultMessage = "Connect to server...";
        private       int    m_current        = 1;

        private void Start()
        {
            StartCoroutine(UpdateText());
        }

        private IEnumerator UpdateText()
        {
            while (true)
            {
                if (++m_current == 4)
                {
                    m_current = 1;
                }

                m_text.text = m_defaultMessage.Substring(0, m_defaultMessage.Length - 3 + m_current);

                yield return new WaitForSeconds(0.5f);
            }
        }
    }
}