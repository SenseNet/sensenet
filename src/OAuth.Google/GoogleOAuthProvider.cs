using System.IO;
using System.Net;
using SenseNet.Services.Virtualization;
using System.Web;
using Newtonsoft.Json;

namespace SenseNet.OAuth.Google
{
    public class GoogleOAuthProvider : OAuthProvider
    {
        public override string IdentifierFieldName { get; } = "GoogleUserId";
        public override IOAuthIdentity GetUserData(object tokenData)
        {
            return tokenData as OAuthIdentity;
        }

        public override string VerifyToken(HttpRequestBase request, out object tokenData)
        {
            var userData = GetUserDataFromToken(request);

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

            //UNDONE: verify and extract token data locally, not using the rest api
            var url = "https://www.googleapis.com/oauth2/v3/tokeninfo?id_token=" + token;
            var gRequest = (HttpWebRequest)WebRequest.Create(url);
            gRequest.Method = "GET";
            gRequest.KeepAlive = true;
            gRequest.ContentType = "appication/json";

            var response = (HttpWebResponse)gRequest.GetResponse();
            string myResponse;
            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                myResponse = sr.ReadToEnd();
            }

            return JsonConvert.DeserializeObject(myResponse);
        }
    }
}
