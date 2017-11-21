using System;
using System.IO;
using System.Net;
using System.Web;
using Newtonsoft.Json;
using SenseNet.ContentRepository.Security;

namespace SenseNet.OAuth.Google
{
    public class GoogleOAuthProvider : OAuthProvider
    {
        private const string GoogleApiTokenInfoUrl = "https://www.googleapis.com/oauth2/v3/tokeninfo?id_token=";

        public override string IdentifierFieldName { get; } = "GoogleUserId";
        public override string ProviderName { get; } = "google";

        public override IOAuthIdentity GetUserData(object tokenData)
        {
            return tokenData as OAuthIdentity;
        }

        public override string VerifyToken(HttpRequestBase request, out object tokenData)
        {
            dynamic userData;

            try
            {
                userData = GetUserDataFromToken(request);
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Google OAuth error: cannot parse user data from the request.");
            }

            tokenData = new OAuthIdentity
            {
                Identifier = userData.sub,
                Email = userData.email,
                Username = userData.sub,
                FullName = userData.name
            };

            return userData.sub;
        }

        private static dynamic GetUserDataFromToken(HttpRequestBase request)
        {
            string body;
            using (var reader = new StreamReader(request.InputStream))
            {
                body = reader.ReadToEnd();
            }

            dynamic requestBody = JsonConvert.DeserializeObject(body);
            string token = requestBody.token;

            //TODO: verify and extract token data locally, not using the rest api
            var url = GoogleApiTokenInfoUrl + token;
            var gRequest = (HttpWebRequest)WebRequest.Create(url);
            gRequest.Method = "GET";
            gRequest.KeepAlive = true;
            gRequest.ContentType = "appication/json";

            var response = (HttpWebResponse)gRequest.GetResponse();
            string myResponse;
            using (var sr = new StreamReader(response.GetResponseStream() ?? throw new InvalidOperationException("Empty response from Google API.")))
            {
                myResponse = sr.ReadToEnd();
            }

            return JsonConvert.DeserializeObject(myResponse);
        }
    }
}
