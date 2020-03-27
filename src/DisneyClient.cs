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

            if (!Grant(parsed.AccessToken))
                throw new Exception("Internal error");
        }

        public void LoadProfile(string name)
        {
            var profiles = GetProfiles();
            var profileResponses = profiles as ProfileResponse[] ?? profiles.ToArray();
            var profile = profileResponses.FirstOrDefault(x => x.ProfileName == name);

            if (profile == null)
                throw new Exception("No user found with this name!");

            var response = _reqClient.PutAuthorized($"accounts/me/profiles/{profile.ProfileId}");
        }

        private IEnumerable<ProfileResponse> GetProfiles()
        {
            var response = _reqClient.GetAuthorized("accounts/me/profiles");

            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception($"Error while getting profiles. Message {response.Content}");

            return JsonConvert.DeserializeObject<List<ProfileResponse>>(response.Content);
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

    internal class SubtitleAppearance
    {
        [JsonProperty("backgroundColor")]
        public string BackgroundColor { get; set; }
        [JsonProperty("backgroundOpacity")]
        public int BackgroundOpacity { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("font")]
        public string Font { get; set; }
        [JsonProperty("size")]
        public string Size { get; set; }
        [JsonProperty("textColor")]
        public string TextColor { get; set; }
    }

    internal class LanguagePreferences
    {
        [JsonProperty("appLanguage")]
        public string AppLanguage { get; set; }
        [JsonProperty("playbackLanguage")]
        public string PlaybackLanguage { get; set; }
        [JsonProperty("preferAudioDescription")]
        public bool PreferAudioDescription { get; set; }
        [JsonProperty("preferSDH")]
        public bool PreferSdh { get; set; }
        [JsonProperty("subtitleAppearance")]
        public SubtitleAppearance SubtitleAppearance { get; set; }
        [JsonProperty("subtitleLanguage")]
        public string SubtitleLanguage { get; set; }
        [JsonProperty("subtitlesEnabled")]
        public bool SubtitlesEnabled { get; set; }

    }

    internal class Avatar
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("userSelected")]
        public bool UserSelected { get; set; }
    }

    internal class PlaybackSettings
    {
        [JsonProperty("autoplay")]
        public bool Autoplay { get; set; }
        [JsonProperty("backgroundVideo")]
        public bool BackgroundVideo { get; set; }
        [JsonProperty("previewAudioOnHome")]
        public bool PreviewAudioOnHome { get; set; }
        [JsonProperty("previewVideoOnHome")]
        public bool PreviewVideoOnHome { get; set; }
    }

    internal class Attributes
    {
        [JsonProperty("isDefault")]
        public bool IsDefaultProfile { get; set; }
        [JsonProperty("kidsModeEnabled")]
        public bool KidModeEnabled { get; set; }
        [JsonProperty("languagePreferences")]
        public LanguagePreferences LanguagePreferences { get; set; }
        [JsonProperty("avatar")]
        public Avatar Avatar { get; set; }
        [JsonProperty("playbackSettings")]
        public PlaybackSettings PlaybackSettings { get; set; }
    }

    internal class ProfileResponse
    {
        [JsonProperty("profileName")]
        public string ProfileName { get; set; }
        [JsonProperty("partner")]
        public string Partner { get; set; }
        [JsonProperty("profileId")]
        public string ProfileId { get; set; }
        [JsonProperty("attributes")]
        public Attributes Attributes { get; set; }
    }
}
