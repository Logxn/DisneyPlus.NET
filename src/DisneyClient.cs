using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using Newtonsoft.Json;

namespace Disney_.NET
{
    public class DisneyClient
    {
        private readonly RequestClient _reqClient;
        public DisneyClient()
        {
            _reqClient = new RequestClient();
        }

        public void Login(string userEmail, string userPassword)
        {
            if (CheckEmail(userEmail) == false)
                throw new Exception("This email is not tied to a Disney+ account");

            var data = new {email = userEmail, password = userPassword};
            var response = _reqClient.PostJsonAuthorized("idp/login", data);

            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception($"Error while logging in. Message {response.Content}");

            var parsed = JsonConvert.DeserializeObject<LoginResponse>(response.Content);

            if (Grant(parsed.AccessToken))
            {
                Console.WriteLine("Access Token updated!");
            }
        }

        private bool Grant(string accessToken)
        {
            var data = new {id_token = accessToken};
            var response = _reqClient.PostJsonAuthorized("accounts/grant", data);

            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception($"Error during account grant. Message {response.Content}");

            return _reqClient.UpdateAccessTokenAfterLogin(JsonConvert.DeserializeObject<GrantResponse>(response.Content));
        }

        private bool CheckEmail(string userEmail)
        {
            var data = new {email = userEmail};
            var response = _reqClient.PostJsonAuthorized("idp/check", data);

            if (response.StatusCode == HttpStatusCode.BadRequest)
                throw new Exception("Received malformed email address");

            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception($"Error while checking for email address. Message {response.Content}");

            var parsed = JsonConvert.DeserializeObject<EmailCheckResponse>(response.Content);

            return parsed.Operations.All(x => x != "Register");
        }
    }

    internal class EmailCheckResponse
    {
        [JsonProperty("operations")]
        public List<string> Operations { get; set; }
    }

    internal class LoginResponse
    {
        [JsonProperty("token_type")]
        public string TokenType { get; set; }
        [JsonProperty("id_token")]
        public string AccessToken { get; set; }
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
    }

    internal class GrantResponse
    {
        [JsonProperty("grant_type")]
        public string GrantType { get; set; }
        [JsonProperty("assertion")]
        public string AccessToken { get; set; }
    }
}
