using System;
using UnityEngine;
using System.Collections;
using UElixir.Sample.UI;
using UnityEngine.SceneManagement;

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

        private bool m_complete = false;

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

            Authentication.Authenticate(userName, password, OnAuthenticationCompleted);
            m_waitForResponse = true;
        }

        private void OnAuthenticationCompleted(Response response)
        {
            m_waitForResponse = false;

            if (response.Result == ERPCResult.Ok)
            {
                m_complete = true;
            }
        }

        private void Update()
        {
            if (m_complete)
            {
                LoadWorldScene();
            }
        }
        private void LoadWorldScene()
        {
            SceneManager.LoadScene("World", LoadSceneMode.Single);
        }
    }
}