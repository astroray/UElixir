using System;
using UnityEngine;
using System.Collections;
using UElixir.Sample.UI;

namespace UElixir.Sample
{
    public class LoginScene : MonoBehaviour
    {
        [SerializeField]
        private ConnectionForm m_connectionForm;
        [SerializeField]
        private LoginForm m_loginForm;
        [SerializeField]
        private float m_timeout;

        private bool m_waitForResponse = false;

        private NetworkManager m_networkManager;

        private void Start()
        {
            m_networkManager = NetworkManager.Instance;
            m_connectionForm.SetVisibility(true);
            m_loginForm.SetVisibility(false);

            InvokeRepeating(nameof(TryConnect), 0.0f, 1.0f);
        }

        private async void TryConnect()
        {
            if (!m_networkManager.IsConnected)
            {
                await m_networkManager.Connect(OnConnectedToServer);
            }
        }

        private void OnConnectedToServer(bool isConnected)
        {
            m_connectionForm.SetVisibility(!isConnected);
            m_loginForm.SetVisibility(isConnected);
        }

        public void OnLoginButtonClicked()
        {
            if (m_waitForResponse)
            {
                return;
            }

            var userName = m_loginForm.UserName.GetInput();
            var password = m_loginForm.Password.GetInput();

            Authentication.Authenticator.Authenticate(userName, password, OnAuthenticationCompleted);
            m_waitForResponse = true;
        }

        private void OnAuthenticationCompleted(Response response)
        {
            switch (response.Result)
            {
                case ERPCResult.Ok:

                    Debug.Log($"Login succeed : {response.Value}");

                    break;
                case ERPCResult.Error:
                    Debug.LogError($"Login failed : {response.Value}");

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            m_waitForResponse = false;
        }
    }
}