using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UElixir.Authentication
{
    public static class Authenticator
    {
        public static void Authenticate(string userName, string password, ResponseCallback responseCallback)
        {
            var userInfo = new JObject();
            userInfo.Add("user_name", userName);
            userInfo.Add("password",  password);

            Message message = new Message
            {
                UserId  = -1,
                Request = "authenticate",
                Arg     = userInfo.ToString(Newtonsoft.Json.Formatting.None, null),
            };

            NetworkManager.Instance.SendToServer(message, responseCallback);
        }
    }
}