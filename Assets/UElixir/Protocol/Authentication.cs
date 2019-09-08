using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UElixir
{
    public static class Authentication
    {
        public static int ClientId { get; private set; } = -1;
        public static bool IsAuthenticated => ClientId != -1;

        public static void Authenticate(string userName, string password, ResponseCallback responseCallback)
        {
            var userInfo = new JObject { { "user_name", userName }, { "password", password } };

            Message message = new Message
            {
                UserId  = ClientId,
                Request = "authenticate",
                Arg     = userInfo.ToString(Newtonsoft.Json.Formatting.None, null),
            };

            NetworkManager.Instance.SendToServer(message, response => OnResponse(response, responseCallback));
        }

        private static void OnResponse(Response response, ResponseCallback responseCallback)
        {
            switch (response.Result)
            {
                case ERPCResult.Ok:

                    Debug.Log($"Login succeed : {response.Args}");
                    ClientId = int.Parse(response.Args);

                    break;
                case ERPCResult.Error:
                    Debug.LogError($"Login failed : {response.Args}");

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            responseCallback?.Invoke(response);
        }
    }
}