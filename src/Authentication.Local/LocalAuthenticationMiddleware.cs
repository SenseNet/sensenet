using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SenseNet.Authentication.Local;

internal sealed class LocalAuthenticationMiddleware(
    RequestDelegate next, LocalAuthenticationOptions options, LocalAuthenticationPolicy policy,
    ILogger<LocalAuthenticationMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context, LocalAuthenticationSessions sessions, ILocalAuthenticationUsers users)
    {
        var path = context.Request.Path.Value;
        if (path != "/authentication/capabilities" &&
            !(path?.StartsWith(LocalAuthenticationOptions.EndpointPrefix + "/", StringComparison.Ordinal) ?? false))
        {
            await next(context).ConfigureAwait(false);
            return;
        }
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";
        if (path == "/authentication/capabilities")
        {
            if (!HttpMethods.IsGet(context.Request.Method))
            {
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                return;
            }
            await context.Response.WriteAsJsonAsync(new
            {
                mode = options.Mode.ToString(),
                local = policy.Allows(context) ? new
                {
                    issuer = options.Issuer,
                    login = LocalAuthenticationOptions.EndpointPrefix + "/login",
                    refresh = LocalAuthenticationOptions.EndpointPrefix + "/refresh",
                    logout = LocalAuthenticationOptions.EndpointPrefix + "/logout",
                    revoke = LocalAuthenticationOptions.EndpointPrefix + "/revoke"
                } : null,
                external = options.Mode == LocalAuthenticationMode.Secondary
                    ? new { authority = options.ExternalAuthority } : null
            }, context.RequestAborted).ConfigureAwait(false);
            return;
        }
        var operation = path![LocalAuthenticationOptions.EndpointPrefix.Length..];
        if (operation is not ("/login" or "/refresh" or "/logout" or "/revoke"))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            return;
        }
        if (!policy.Allows(context))
        {
            await FailAsync(context, operation, StatusCodes.Status403Forbidden).ConfigureAwait(false);
            return;
        }
        if (!policy.TryAttempt(context))
        {
            await FailAsync(context, operation, StatusCodes.Status429TooManyRequests).ConfigureAwait(false);
            return;
        }
        if (!context.Request.HasJsonContentType())
        {
            await FailAsync(context, operation, StatusCodes.Status415UnsupportedMediaType).ConfigureAwait(false);
            return;
        }
        LocalRequest? request;
        try
        {
            // Bound chunked bodies too. Never buffer an unbounded credential request.
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, false, 1024, leaveOpen: true);
            var buffer = new char[8193];
            var length = await reader.ReadBlockAsync(buffer.AsMemory(), context.RequestAborted).ConfigureAwait(false);
            if (length > 8192)
            {
                await FailAsync(context, operation, StatusCodes.Status413PayloadTooLarge).ConfigureAwait(false);
                return;
            }
            request = JsonSerializer.Deserialize<LocalRequest>(buffer.AsSpan(0, length), JsonOptions);
        }
        catch (JsonException)
        {
            await FailAsync(context, operation, StatusCodes.Status400BadRequest).ConfigureAwait(false);
            return;
        }
        LocalTokenResponse? response = null;
        if (operation == "/login")
        {
            if (request?.Username is not { Length: > 0 and <= 256 } ||
                request.Password is not { Length: > 0 and <= 4096 } || request.TwoFactorCode?.Length > 64)
            {
                await FailAsync(context, operation, StatusCodes.Status401Unauthorized).ConfigureAwait(false);
                return;
            }
            if (!policy.TryAttempt(context, request.Username))
            {
                await FailAsync(context, operation, StatusCodes.Status429TooManyRequests).ConfigureAwait(false);
                return;
            }
            var userId = await users.ValidateAsync(context, request.Username, request.Password, request.TwoFactorCode)
                .ConfigureAwait(false);
            if (userId.HasValue)
                response = await sessions.CreateAsync(userId.Value, context.RequestAborted).ConfigureAwait(false);
        }
        else if (request?.RefreshToken is { Length: 64 })
        {
            if (operation is "/logout" or "/revoke")
            {
                await sessions.RevokeAsync(request.RefreshToken, context.RequestAborted).ConfigureAwait(false);
                logger.LogInformation("Local authentication {Operation} succeeded", operation);
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return;
            }
            response = await sessions.RefreshAsync(request.RefreshToken, context.RequestAborted).ConfigureAwait(false);
        }
        if (response == null)
        {
            await FailAsync(context, operation, StatusCodes.Status401Unauthorized).ConfigureAwait(false);
            return;
        }
        logger.LogInformation("Local authentication {Operation} succeeded", operation);
        await context.Response.WriteAsJsonAsync(response, context.RequestAborted).ConfigureAwait(false);
    }

    private Task FailAsync(HttpContext context, string operation, int status)
    {
        logger.LogWarning("Local authentication {Operation} denied ({Status})", operation, status);
        context.Response.StatusCode = status;
        if (status == StatusCodes.Status429TooManyRequests)
            context.Response.Headers.RetryAfter = "60";
        return context.Response.WriteAsJsonAsync(new { error = "Authentication failed." }, context.RequestAborted);
    }

    private sealed record LocalRequest(string? Username, string? Password, string? TwoFactorCode, string? RefreshToken);
}
