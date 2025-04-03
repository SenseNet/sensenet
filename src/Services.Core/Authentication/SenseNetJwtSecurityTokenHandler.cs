using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SenseNet.Configuration;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SenseNet.Services.Core.Authentication;

public class SenseNetJwtSecurityTokenHandler : TokenHandler
{
    private readonly string _validateTokenUrl;
    private readonly JwtSecurityTokenHandler _defaultHandler;
    private readonly IHttpClientFactory _httpClientFactory;

    public SenseNetJwtSecurityTokenHandler(string validateTokenUrl)
    {
        _httpClientFactory = Providers.Instance.Services.GetRequiredService<IHttpClientFactory>();
        _validateTokenUrl = validateTokenUrl ?? throw new ArgumentNullException(nameof(validateTokenUrl));
        _defaultHandler = new JwtSecurityTokenHandler();
    }

    public override int MaximumTokenSizeInBytes
    {
        get => base.MaximumTokenSizeInBytes;
        set => base.MaximumTokenSizeInBytes = value;
    }

    public override SecurityToken ReadToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentNullException(nameof(token));

        return _defaultHandler.ReadJwtToken(token);
    }

    public override async Task<TokenValidationResult> ValidateTokenAsync(string token, TokenValidationParameters validationParameters)
    {
        await ValidateTokenAsync(token);

        try
        {
            var jwtToken = ReadToken(token) as JwtSecurityToken;

            return new TokenValidationResult
            {
                ClaimsIdentity = new ClaimsIdentity(jwtToken.Claims, "Custom"),
                SecurityToken = jwtToken,
                IsValid = true
            };
        }
        catch (Exception ex)
        {
            return new TokenValidationResult
            {
                Exception = ex
            };
        }
    }

    public override async Task<TokenValidationResult> ValidateTokenAsync(SecurityToken token, TokenValidationParameters validationParameters)
    {
        ArgumentNullException.ThrowIfNull(token);

        var jwtToken = token as JwtSecurityToken ?? throw new ArgumentException("The token must be of type JwtSecurityToken.");

        await ValidateTokenAsync(jwtToken.RawData);

        return new TokenValidationResult
        {
            ClaimsIdentity = new ClaimsIdentity(jwtToken.Claims, "Custom"),
            SecurityToken = jwtToken,
            IsValid = true
        };
    }

    private async Task ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return;

        using var httpClient = _httpClientFactory.CreateClient();

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
}
