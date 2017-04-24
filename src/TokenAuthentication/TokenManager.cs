using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.IdentityModel.Tokens;
using SecurityKey = Microsoft.IdentityModel.Tokens.SecurityKey;
using SecurityAlgorithms = Microsoft.IdentityModel.Tokens.SecurityAlgorithms;
using SigningCredentials = Microsoft.IdentityModel.Tokens.SigningCredentials;
using SymmetricSecurityKey = Microsoft.IdentityModel.Tokens.SymmetricSecurityKey;
using RsaSecurityKey = Microsoft.IdentityModel.Tokens.RsaSecurityKey;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace SenseNet.TokenAuthentication
{
    public class TokenManager
    {
        private readonly ITokenParameters _tokenParameters;

        private readonly string _tokenTypeCode = "JWT";

        private ISecurityKey _securityKey;
        private ISecurityTokenHandler _handler;

        public TokenManager(ISecurityKey securityKey, ISecurityTokenHandler tokenHandler, ITokenParameters tokenParameters)
        {
            _handler = tokenHandler;
            _tokenParameters = tokenParameters;
            _securityKey = securityKey;
        }

        private int GetNumericDate(DateTime date)
        {
            return (int)(date - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        public string GenerateToken(string name, string role, out string refreshTokenString, bool refreshTokenAsWell = false)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentOutOfRangeException(nameof(name));
            }
            string tokenString = null;
            refreshTokenString = null;
            if (_handler.CanWriteToken)
            {
                var signingCredentials = new SigningCredentials(_securityKey.SecurityKey, _tokenParameters.EncryptionAlgorithm);
                var now = _tokenParameters.ValidFrom ?? DateTime.UtcNow;
                var numericNow = GetNumericDate(now);
                var notBefore = now;
                var numericNotBefore = GetNumericDate(notBefore);
                var expiration = now.AddMinutes(_tokenParameters.AccessLifeTimeInMinutes);
                var numericExpiration = GetNumericDate(expiration);
                var header = new JwtHeader(signingCredentials);
                var payload = new JwtPayload
                {
                    { "iss", _tokenParameters.Issuer}
                    , {"sub", _tokenParameters.Subject }
                    , { "aud", _tokenParameters.Audience}
                    , { "exp", numericExpiration}
                    , { "iat", numericNow}
                    , { "nbf", numericNotBefore}
                    , { "name", name}
                };
                if (!string.IsNullOrWhiteSpace(role))
                {
                    payload.AddClaim(new Claim("role", role));
                }

                var accessToken = new JwtSecurityToken(header, payload);
                tokenString = _handler.WriteToken(accessToken);

                if (refreshTokenAsWell)
                {
                    var refreshExpiration = expiration.AddMinutes(_tokenParameters.RefreshLifeTimeInMinutes);
                    numericExpiration = GetNumericDate(refreshExpiration);
                    notBefore = expiration;
                    numericNotBefore = GetNumericDate(notBefore);
                    payload = new JwtPayload
                    {
                        { "iss", _tokenParameters.Issuer}
                        , {"sub", _tokenParameters.Subject }
                        , { "aud", _tokenParameters.Audience}
                        , { "exp", numericExpiration}
                        , { "iat", numericNow}
                        , { "nbf", numericNotBefore }
                        , { "name", name}
                    };
                    var refreshToken = new JwtSecurityToken(header, payload);
                    refreshTokenString = _handler.WriteToken(refreshToken);
                }
            }
            return tokenString;
        }

        public  IPrincipal ValidateToken(string token, bool validateLifeTime = true)
        {
            var validationParameters = new TokenValidationParameters
            {
                AuthenticationType = _tokenTypeCode
                , ValidIssuer = _tokenParameters.Issuer
                , ValidAudience = _tokenParameters.Audience
                , IssuerSigningKey = _securityKey.SecurityKey
                , TokenDecryptionKey = _securityKey.SecurityKey
                , ClockSkew = new TimeSpan(0, _tokenParameters.ClockSkewInMinutes, 0)
                , ValidateIssuerSigningKey = true
                , RequireExpirationTime = true
                , RequireSignedTokens = true
                , ValidateAudience = true
                , ValidateIssuer = true
                , ValidateLifetime = validateLifeTime
            };

                Microsoft.IdentityModel.Tokens.SecurityToken validatedToken;
                var principal = _handler.ValidateToken(token, validationParameters, out validatedToken);
                return principal;
        }
    }
}