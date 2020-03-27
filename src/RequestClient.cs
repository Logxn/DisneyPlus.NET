using System;
using System.Threading;
using System.Net;

using RestSharp;
using Newtonsoft.Json;

namespace Disney_.NET
{
    internal class RequestClient
    {
        private static string _accessToken;
        private static string _tokenType;
        private static string _refreshToken;

        // Standard App Constants
        private const string BaseUrl = "https://global.edge.bamgrid.com/";
        private const string BaseAuthToken = "Bearer ZGlzbmV5JmFwcGxlJjEuMC4w.H9L7eJvc2oPYwDgmkoar6HzhBJRuUUzt_PcaC3utBI4";

        private static RestClient _client;
        public RequestClient()
        {
            // The following headers are required, but not checked if valid
            
            _client = new RestClient(new Uri(BaseUrl));
            _client.AddDefaultHeader("X-BAMSDK-Platform", "iPhone 10,6");
            _client.AddDefaultHeader("X-BAMSDK-Client-ID", "disney-svod-187187");
            _client.AddDefaultHeader("X-BAMSDK-Transaction-ID", "1337");
            _client.AddDefaultHeader("X-BAMSDK-Version", "9.6.1");
            _client.AddDefaultHeader("User-Agent", "Disney+/20696 CFNetwork/1121.2.2 Darwin/19.3.0");

            var response = RequestJwtToken();

            _accessToken = response.AccessToken;
            _tokenType = response.TokenType;
            _refreshToken = response.RefreshToken;
            var expiresIn = response.ExpiresIn;

            var unused = new Timer(RefreshCallback, null, TimeSpan.FromMinutes((double)expiresIn / 60 / 60), TimeSpan.FromMinutes((double)expiresIn / 60 / 60));
        }

        public IRestResponse PostJsonAuthorized(string endpoint, object data)
        {
            var request = new RestRequest(endpoint, Method.POST);
            request.AddHeader("Authorization", $"{_tokenType} {_accessToken}");
            request.AddJsonBody(data);

            return _client.Execute(request);
        }

        /// <summary>
        /// This endpoint creates a JWT token if the standard authentication is correct
        /// </summary>
        /// <returns></returns>
        private static TokenResponse RequestJwtToken()
        {
            var request = new RestRequest("devices", Method.POST, DataFormat.Json);
            request.AddHeader("Authorization", BaseAuthToken);
            request.AddJsonBody(new
            { applicationRuntime = "ios", deviceProfile = "iphone", deviceFamily = "apple", attributes = new { } });

            var response = _client.Execute(request, Method.POST);

            if (response.StatusCode != HttpStatusCode.Created)
                throw new Exception($"Error while requesting JWT Bearer. Message: {response.Content}");

            var parsed = JsonConvert.DeserializeObject<DevicesResponse>(response.Content);
            
            return RequestAccessToken(parsed.Token);
        }

        /// <summary>
        /// Requests an access token pre-login
        /// </summary>
        /// <param name="token">The previously requested JWT token</param>
        /// <returns>AccessToken, RefreshToken, Expiry Information</returns>
        private static TokenResponse RequestAccessToken( string token)
        {
            var request = new RestRequest("token", Method.POST);
            request.AddHeader("Authorization", BaseAuthToken);
            request.AddParameter("application/x-www-form-urlencoded", $"platform=iphone&grant_type=urn:ietf:params:oauth:grant-type:token-exchange&subject_token={token}&subject_token_type=urn:bamtech:params:oauth:token-type:device",ParameterType.RequestBody);

            var response = _client.Execute(request, Method.POST);

            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception($"Error while requesting Access Token. Message {response.Content}");

            return JsonConvert.DeserializeObject<TokenResponse>(response.Content);
        }

        /// <summary>
        /// Used to refresh the current access token every 4 minutes
        /// </summary>
        private static void RefreshCallback(object o)
        {
            // Handle refresh token here
            // Probably under /token/

            var request = new RestRequest("token", Method.POST);
            request.AddHeader("Authorization", BaseAuthToken);
            request.AddParameter("application/x-www-form-urlencoded", $"platform=iphone&grant_type=refresh_token&refresh_token={_refreshToken}", ParameterType.RequestBody);

            var response = _client.Execute(request, Method.POST);

            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception($"Error while requesting Access Token. Message {response.Content}");

            var parsed = JsonConvert.DeserializeObject<TokenResponse>(response.Content);
            _accessToken = parsed.AccessToken;
            _refreshToken = parsed.RefreshToken;

            Console.WriteLine($"<{DateTime.Now}> - Token Refreshed!");
        }

        /// <summary>
        /// After login we get a new JWT token and need to request a new access token.
        /// </summary>
        /// <param name="tokenInfo">JWT token info</param>
        /// <returns></returns>
        public bool UpdateAccessTokenAfterLogin(GrantResponse tokenInfo)
        {
            var request = new RestRequest("token", Method.POST);
            request.AddHeader("Authorization", BaseAuthToken);
            request.AddParameter("application/x-www-form-urlencoded", $"subject_token={tokenInfo.AccessToken}&platform=iphone&grant_type=urn:ietf:params:oauth:grant-type:token-exchange&subject_token_type=urn:bamtech:params:oauth:token-type:account", ParameterType.RequestBody);

            var response = _client.Execute(request, Method.POST);

            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception($"Error while updating access token after login. Message {response.Content}");

            var parsed = JsonConvert.DeserializeObject<TokenResponse>(response.Content);

            _accessToken = parsed.AccessToken;
            _tokenType = parsed.TokenType;
            _refreshToken = parsed.RefreshToken;

            return true;
        }
    }

    internal class DevicesResponse
    {
        [JsonProperty("grant_type")]
        public string GrantType { get; set; }
        [JsonProperty("assertion")]
        public string Token { get; set; }
    }

    internal class TokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
        [JsonProperty("token_type")]
        public string TokenType { get; set; }
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
    }
}
