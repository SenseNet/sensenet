# OAuth in sensenet ECM
[OAuth 2.0](https://oauth.net/2/) is the industry-standard protocol for authorization. In [sensenet ECM](https://github.com/SenseNet/sensenet) we use it as an extension to our [web token authentication](https://community.sensenet.com/docs/web-token-authentication) to let users **authenticate** using well-known services (such as *Google* or *Facebook*).

The benefit is that users are able to sign in to a sensenet ECM application with a single click, **without manual registration**.

## How it works?
When new users come to the site, they will be able to sign in by clicking the Google or Facebook button (or a similar custom experience implemented by the developer). The workflow is the following:

- User signs in to the 3rd party service.
- User authorizes the application with the service (e.g. let the application access basic user data like name and email). This is usually a click of a button in the Google or Facebook popup window.
- The client **receives a token from the service**. 
- The client sends the token to the sensenet ECM server, where the appropriate **OAuth provider verifies the token**.
- If the token has been verified, we load or create the corresponding *User* content in the Content Repository. User content items are connected to the 3rd party service by storing the unique user identifier in a provider-specific separate field (e.g. *GoogleUserId*).
- sensenet ECM assembles a [JWT token](https://community.sensenet.com/docs/web-token-authentication) for the client and considers the user as correctly signed in.

From that point on the user will be able to use the application as a regular user.

### Configuration
You can specify where new users are created and their content type using the *OAuth* settings content in the usual global *Settings* folder.

```json
{
   UserType: "User",
   Domain: "Public"
}
```

New users are created under the domain above separated into organizational units named by the provider.

## OAuth providers
A sensenet ECM OAuth provider is a small plugin that is designed to verify a token using a particular service. Out of the box we offer the following OAuth provider for sensenet ECM:

- Google [![NuGet](https://img.shields.io/nuget/v/SenseNet.OAuth.Google.svg)](https://www.nuget.org/packages/SenseNet.OAuth.Google)

These providers are available as nuget packages on the server side and npm packages on the client. Please follow the instructions in the nuget readme, these packages usually involve executing an install command before you can use them.

## Custom OAuth provider
The OAuth provider feature is extendable by design, so developers may create a custom provider for any 3rd party service by implementing a simple api. For detailed explanation of the api elements to implement please refer to the source code documentation.

```csharp
public class CustomOAuthProvider : OAuthProvider
{
    public override string IdentifierFieldName { get; } = "CustomUserId";
    public override string ProviderName { get; } = "myservicename";

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
            throw new InvalidOperationException("OAuth error: cannot parse user data from the request.");
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

        //TODO: verify token and extract basic user data
        // return userData        
    }
}
```

The example above assumes that there is a field on the User content type called *CustomUserId*. Registering this field is the responsibility of the provider install process.

To start using your custom provider you only have to add a reference to your provider library and sensenet ECM will automatically discover and register your class.

## Client api
If you are using the [JavaScript client SDK](https://github.com/SenseNet/sn-client-js) (as it is recommended), you do not have to deal with sending OAuth tokens to the server, it will do it for you.

## REST api
As an alternative, you can use the native REST api when authenticating with a 3rd party OAuth service. After receiving the service-specific token, that token has to be sent to the server for verification. The api is the following:

```text
/sn-oauth/login?provider=providername
```

For example:

```javascript
$.ajax({
    url: "/sn-oauth/login?provider=google",
    dataType: "json",
    type: 'POST',
    data: JSON.stringify({ 'token':id_token }),
    success: function () {
        console.log('Success');
    }
});
```
