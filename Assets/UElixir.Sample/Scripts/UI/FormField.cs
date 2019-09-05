using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UElixir.Sample.UI
{
    public class FormField : MonoBehaviour
    {
        [SerializeField]
        private Text m_label;
        [SerializeField]
        private InputField m_inputField;

        public string GetInput()
        {
            return m_inputField?.text;
        }
    }
}