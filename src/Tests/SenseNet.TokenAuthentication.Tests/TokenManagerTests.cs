using System;
using System.Text;
using SenseNet.TokenAuthentication;
using Xunit;

namespace SenseNet.TokenAuthentication.Tests
{
    public class TokenManagerTests : IDisposable
    {
        private TokenManager _manager;

        private TokenParameters _tokenParameters;
        private string _name = "MyName";
        private string _role = "MyRole";

        private void Init(string encription)
        {
            var tokenHandler = new JwsSecurityTokenHandler();
            _tokenParameters = new TokenParameters
            {
                Audience = "audience",
                Issuer = "issuer",
                Subject = "subject",
                EncryptionAlgorithm = encription,
                ValidFrom = DateTime.Now.AddMinutes(5),
                ValidateLifeTime = true
            };

            _manager = new TokenManager(EncryptionHelper.CreateKey(encription), tokenHandler, _tokenParameters);
        }

        private string Base64UrlDecode(string encoded)
        {
            //encoded = encoded + new string('=', (4 - encoded.Length%4)%4);
            return
                Encoding.UTF8.GetString(
                    Convert.FromBase64String(encoded.Replace("_", "/").Replace‌​("-", "+") +
                                             new string('=', (4 - encoded.Length%4)%4)));
        }

        [Theory
         , InlineData("HS256")
         , InlineData("HS512")
         , InlineData("RS256")
         , InlineData("RS512")]
        private void GenerateAccessTokenTest(string encryption)
        {
            Init(encryption);

            string refreshToken;
            var token = _manager.GenerateToken(_name, _role, out refreshToken);

            Assert.NotNull(token);
            Assert.Null(refreshToken);

            var parts = token.Split('.');
            Assert.True(parts.Length == 3);

            var claims = Base64UrlDecode(parts[1]);
            Assert.Matches("\"name\":\"" + _name + "\"", claims);
            Assert.Matches("\"role\":\"" + _role + "\"", claims);
        }

        [Theory
         , InlineData("HS256")
         , InlineData("HS512")
         , InlineData("RS256")
         , InlineData("RS512")]
        private void GenerateAccessTokenWithEmptyNameTest(string encryption)
        {
            Init(encryption);

            try
            {
                string refreshToken;
                _manager.GenerateToken(" ", _role, out refreshToken);
                Assert.True(false);
            }
            catch (Exception ex)
            {
                Assert.True(ex is ArgumentOutOfRangeException);
            }
        }

        [Theory
         , InlineData("HS256")
         , InlineData("HS512")
         , InlineData("RS256")
         , InlineData("RS512")]
        private void GenerateAccessTokenWithNullRoleTest(string encryption)
        {
            Init(encryption);

            try
            {
                string refreshToken;
                var token = _manager.GenerateToken(_name, null, out refreshToken);
                Assert.NotNull(token);
                Assert.Null(refreshToken);
                var parts = token.Split('.');
                Assert.True(parts.Length == 3);
                var claims = Base64UrlDecode(parts[1]);
                Assert.Matches("\"name\":\"" + _name + "\"", claims);
                Assert.DoesNotMatch("\"role\":\"" + _role + "\"", claims);
            }
            catch (Exception ex)
            {
                Assert.True(ex is ArgumentOutOfRangeException);
            }
        }

        [Theory
         , InlineData("HS256")
         , InlineData("HS512")
         , InlineData("RS256")
         , InlineData("RS512")]
        private void GenerateAccessAndRefreshTokenTest(string encryption)
        {
            Init(encryption);

            string refreshToken;
            var token = _manager.GenerateToken(_name, _role, out refreshToken, true);

            Assert.NotNull(token);
            var accessParts = token.Split('.');
            Assert.True(accessParts.Length == 3);
            var accessClaims = Base64UrlDecode(accessParts[1]);
            Assert.Matches("\"name\":\"" + _name + "\"", accessClaims);
            Assert.Matches("\"role\":\"" + _role + "\"", accessClaims);
            Assert.NotNull(refreshToken);
            var refreshParts = refreshToken.Split('.');
            Assert.True(refreshParts.Length == 3);
            var refreshClaims = Base64UrlDecode(refreshParts[1]);
            Assert.Matches("\"name\":\"" + _name + "\"", refreshClaims);
            Assert.DoesNotMatch("\"role\":\"" + _role + "\"", refreshClaims);
        }

        [Theory
         , InlineData("HS256")
         , InlineData("HS512")
         , InlineData("RS256")
         , InlineData("RS512")]
        private void ValidateTokenTest(string encryption)
        {
            Init(encryption);

            string refreshToken;
            var token = _manager.GenerateToken(_name, _role, out refreshToken);

            var principal = _manager.ValidateToken(token, false);

            Assert.True(principal.IsInRole(_role));
            Assert.Equal(_name, principal.Identity.Name);
        }


        [Theory
         , InlineData("HS256")
         , InlineData("HS512")
         , InlineData("RS256")
         , InlineData("RS512")]
        private void ValidateTokenWithInvalidTokenTest(string encryption)
        {
            Init(encryption);

            string refreshToken;
            var token = _manager.GenerateToken(_name, _role, out refreshToken);
            token.Replace(".", "_");

            try
            {
                _manager.ValidateToken(token, false);
                Assert.True(false);
            }
            catch 
            {
            }
        }

        public void Dispose()
        {

        }
    }
}