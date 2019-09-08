using UnityEngine;
using System.Collections;

namespace UElixir.Sample.UI
{
    public class LoginForm : UIElement
    {
        [SerializeField]
        private FormField m_userNameField;
        [SerializeField]
        private FormField m_passwordField;

        public FormField UserName => m_userNameField;
        public FormField Password => m_passwordField;
    }
}