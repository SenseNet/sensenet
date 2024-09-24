using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SnWebApplication.Api.Sql.TokenAuth.TokenValidator
{  
    public class CustomJwtSecurityTokenHandler : ISecurityTokenValidator
    {
        private readonly string _validateTokenUrl;
        private readonly JwtSecurityTokenHandler _defaultHandler;

        public bool CanValidateToken => true;
        public int MaximumTokenSizeInBytes { get; set; } = TokenValidationParameters.DefaultMaximumTokenSizeInBytes;

        public CustomJwtSecurityTokenHandler(string validateTokenUrl)
        {
            _validateTokenUrl = validateTokenUrl ?? throw new ArgumentNullException(nameof(validateTokenUrl));
            _defaultHandler = new JwtSecurityTokenHandler();
        }

        public bool CanReadToken(string securityToken) => 
            _defaultHandler.CanReadToken(securityToken);

        public async Task ValidateTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return;

            var requestBody = new { token };
            using var httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders
                .Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await httpClient.GetAsync(_validateTokenUrl);
            if (!response.IsSuccessStatusCode)
                throw new SecurityTokenValidationException("Invalid token.");

            var result = bool.Parse(await response.Content.ReadAsStringAsync());
            if (!result)
                throw new SecurityTokenValidationException("Invalid token.");
        }

        public ClaimsPrincipal ValidateToken(string securityToken, TokenValidationParameters validationParameters, out SecurityToken validatedToken)
        {
            ValidateTokenAsync(securityToken).GetAwaiter().GetResult();

            var jwtToken = _defaultHandler.ReadJwtToken(securityToken);

            var identity = new ClaimsIdentity(jwtToken.Claims, "Custom");
            var principal = new ClaimsPrincipal(identity);

            validatedToken = jwtToken;

            return principal;
        }
    }
}
