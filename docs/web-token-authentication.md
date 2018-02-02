---
title:  "Configuration of Web Token Authentication"
source_url: 'https://github.com/SenseNet/sensenet/blob/jwt/docs/web-token-authentication.md'
category: Development
version: v7.0.0
tags: webtoken jwt authentication sn7
description: Configuration of Web Token Authentication
---

# Configuration of Web Token Authentication #

In a sensenet ECM web application (on all instances) you need to configure the token authentication in the `web.config` file.
Find the `SymmetricKeySecret` parameter in the `tokenAuthentication` section of the sensenet section group. Give it a value of random string (16 - 64 in length) in order to make the authentication work.

All your instances in the NLB should have the same value as their SymmetricKeySecret. Without this your authentication wouldn't work. Also very important to keep this random string a secret, otherwise someone can exploit it as a security breach. It is a good practice to encrypt the whole tokenAuthentication section in the web.config file.

Example of token authentication configuration settings:
```xml
<sensenet>
...
   <tokenAuthentication>
     <add key="SymmetricKeySecret" value="<random secret string>" />
   </tokenAuthentication>
...
</sensenet>
```
There are some other parameters in the tokenAuthentication and security section that you can update to alter the behaviour of the token authentication:
```xml
<tokenAuthentication>
  <add key="SymmetricKeySecret" value="<random secret string>" />
  <add key="Audience" value="client" />
  <add key="Issuer" value="sensenet" />
  <add key="Subject" value="auth" />
  <add key="AccessLifeTimeInMinutes" value="5" />
  <add key="RefreshLifeTimeInMinutes" value="1440" />
  <add key="ClockSkewInMinutes" value="1"/>
</tokenAuthentication>
<security>
...
  <add key="DefaultUltimateLogout" value="true"/>
...
</security>
```
**_Audience, Issuer, Subject_**: environment constants to include in the token (further information at the end of the Token Authentication Protocol part)  
**_AccessLifeTimeInMinutes_**: the time span within the access token is valid from its creation  
**_RefreshLifeTimeInMinutes_**: the time span within the refresh token is valid from its creation  
**_ClockSkewInMinutes_**: the possible maximum difference in actual times between servers
**_DefaultUltimateLogout_**: the default behaviour at logout if ultimateLogout argument (see later) is not provided (_true_: executes an ultimate logout, _false_(default): simple logout). Note that if you provide 'true' here, the system will *always* perform an ultimate logout, *regardless of the parameter provided by the client*.

## Web Token Authentication Protocol ##
### Protocol overview ### 

The token authentication needs a username and password pair for its first move. After it was given and the user was successfully identified, the service generates an access token and a refresh token and sends it to the client. The client can use the access token to get to the content allowed only for authenticated users. Every token has its expiration time, so when the access token is expired the client cannot access protected content. The client has to use the refresh token to obtain a new access token. When it is received, the client can use it to access content again. The refresh token could be expired too. In that case the client has to re-authenticate with a username and password and regain access to protected content. After a user finished his work, logs out. This deletes the obtained credentials. There are two ways of logging out: simple, when the user's logout deletes the local credentials only and ultimate, when the user's logout prevents the user to access protected content from any session on any device until he is logged in again.

### Protocol use cases in detail ###

_Steps of an authentication process from the clients' point of view:_
1. Login with username and password by basic authentication on the login endpoint
2. Receive a new access token and refresh token
3. Access content using the access token
4. Logout using the access token

_Steps of a token refresh process from the clients' point of view:_
1. Send the refresh token to the refresh endpoint
2. Receive a new access token
3. Access content using the access token
4. Logout using the access token

All the communication are sent through SSL (https). The used cookies are all HtmlOnly and Secure. There are two types of communication: header marked and uri marked (without header mark). Either of them can be choosen freely by a client developer. However the two could be mixed, but we advice to choose one and stick to it.

![web token authentication protocol](images/SensenetTokenAuthentication.png)
_figure 1:web token authentication protocol_

**LoginRequest with header mark:**  
_uri:_  
`https://<yourhost>/<indifferentpath>`  
_headers:_  
X-Authentication-Action: TokenLogin  
Authorization: Basic `<base64CodedCredentials>`

**LoginRequest with uri mark:**   
_uri:_  
`https://<yourhost>/sn-token/login`   
_headers:_  
Authorization: Basic `<base64CodedCredentials>`

**LoginResponse:**  
_cookies:_  
Set-Cookie: rs=`<refreshSignature>`  
Set-Cookie: as=`<accessSignature>`  
_body:_  
```json 
{"access":"<accessHeadAndPayload>", "refresh":"<refreshHeadAndPayload>"}
```

**Simple LogoutRequest with header mark:**  
_uri:_  
`https://<yourhost>/<indifferentpath>`  
_headers:_  
X-Authentication-Action: TokenLogout  
X-Access-Data: `<accessHeadAndPayload>`  
_cookies:_  
Cookie: as=`<accessSignature>`  
Cookie: ahp=`<accessHeadAndPayload>`  
Cookie: rs=`<refreshSignature>`

**Simple LogoutRequest with uri mark:**   
_uri:_  
`https://<yourhost>/sn-token/logout`  
_headers:_  
_cookies:_  
Cookie: as=`<accessSignature>`  
Cookie: ahp=`<accessHeadAndPayload>`  
Cookie: rs=`<refreshSignature>`

**Ultimate LogoutRequest with header mark:**  
_verb (mandatory):_ POST  
_uri:_  
`https://<yourhost>/<indifferentpath>`  
_headers:_  
Content-Type: `application/x-www-form-urlencoded`  
X-Authentication-Action: `TokenLogout`  
X-Access-Data: `<accessHeadAndPayload>`  
_cookies:_  
Cookie: as=`<accessSignature>`  
Cookie: ahp=`<accessHeadAndPayload>`  
Cookie: rs=`<refreshSignature>`  
_body:_  
`ultimateLogout=true`  

**Ultimate LogoutRequest with uri mark:**   
_uri:_  
`https://<yourhost>/sn-token/logout?ultimateLogout=true`  
_headers:_  
_cookies:_  
Cookie: as=`<accessSignature>`  
Cookie: ahp=`<accessHeadAndPayload>`  
Cookie: rs=`<refreshSignature>`

**AuthenticatedServiceRequest with header mark:**  
_uri:_  
`https://<yourhost>/<contentpath>`  
headers:  
`X-Authentication-Action: TokenAccess`  
`X-Access-Data: <accessHeadAndPayload>`  
_cookies:_  
Cookie: as=`<accessSignature>`
Cookie: ahp=`<accessHeadAndPayload>`  
Cookie: rs=`<refreshSignature>`  

**AuthenticatedServiceRequest without header mark:**  
_uri:_  
`https://<yourhost>/<contentpath>`  
_headers:_  
`X-Access-Data: <accessHeadAndPayload>`  
_cookies:_  
Cookie: rs=`<refreshSignature>`  
Cookie: ahp=`<accessHeadAndPayload>`  
Cookie: as=`<accessSignature>`

**UnauthenticatedServiceRequest with header mark:**  
_uri:_   
`https://<yourhost>/<contentpath>`  
_headers:_  
`X-Authentication-Action: TokenAccess`  
`X-Access-Data: <expiredAccessHeadAndPayload>`  
_cookies:_  
Cookie: as=`<expiredAccessSignature>`  
Cookie: ahp=`<expiredAccessHeadAndPayload>`  
Cookie: rs=`<refreshSignature>`  

**UnauthenticatedServiceRequest without header mark:**  
_uri:_   
`https://<yourhost>/<contentpath>`  
_headers:_  
_cookies:_  
Cookie: as=`<expiredAccessSignature>`  
Cookie: ahp=`<expiredAccessHeadAndPayload>`  
Cookie: rs=`<refreshSignature>`  

**ServiceResponse:**  
_body:_  
`<contentData>`

**RefreshRequest with header mark:**  
_uri:_  
`https://<yourhost>/<indifferentpath>`  
_headers:_  
X-Authentication-Action: TokenRefresh    
X-Refresh-Data: `<refreshHeadAndPayload>`  
_cookies:_  
Cookie: as=`<expiredAccessSignature>`  
Cookie: ahp=`<expiredAccessHeadAndPayload>`  
Cookie: rs=`<refreshSignature>`  

**RefreshRequest with uri mark:**  
_uri:_  
`https://<yourhost>/sn-token/refresh`  
_headers:_  
X-Refresh-Data: `<refreshHeadAndPayload>`  
_cookies:_  
Cookie: as=`<expiredAccessSignature>`  
Cookie: ahp=`<expiredAccessHeadAndPayload>`  
Cookie: rs=`<refreshSignature>`  

**RefreshResponse:**  
_cookies:_  
Set-Cookie: as=`<accessSignature>`  
Set-Cookie: ahp=`<accessHeadAndPayload>`  
_body:_  
```json 
{"access":"<accessHeadAndPayload>"}
```

**<200>:**  
HTTP response with status 200 (OK). On the diagram it is used to sign an empty response in case of a not authenticated request. It is important that sensenet ECM does not throw an exception here.

**<401>:**  
HTTP response with status 401 (Unauthorized). On the diagram it is used to sign a response to an unsuccessful login, logout or refresh request.

### The used headers in detail ###  
**_Authorization_**: this header is a standard HTTP header and tells the service, that a client would like to authenticate. Its value always begins with "Basic ", that signes a basic type authentication requires a valid username and password.  
**_X-Access-Data_**: this header tells the service, that a client tries to access a content with a token. Its value is an access token head and payload.  
**_X-Authentication-Action_**: this header tells the service in case of header marked communication, that a token authentication action is requested. Its value can be `TokenLogin`, `TokenLogout`, `TokenAccess`, `TokenRefresh`.  
**_X-Refresh-Data_**: this header tells the service, that a client tries to refresh its expired access token. Its value is a refresh token head and payload.

### The used cookies ###  
**_as, ahp, rs_**: technical HttpOnly and Secure cookies for token authentication. They are emitted by token authentication service. The client does not need them and they are not subjects of change.

`<refreshSignature>, <accessSignature>`: signature strings used by the authentication service.  
`<accessHeadAndPayload>, <refreshHeadAndPayload>`: base64 and URL encoded strings.

The access head and payload are the public part of a token, that consists of two parts separated by a full stop.
The first one is a technical like header that you do not have to care about. The second one - the payload - contains claims about the authenticated user and about some authentication concerning data. Once the payload has been decoded from base64 it will be a string representation of a JSON object, so it can be easily use in Javascript.

**Example of a typical payload:**  
```json
{
  "iss":"sensenet-token-service",
  "sub":"sensenet",
  "aud":"client",
  "exp":1490577801,
  "iat":1490577501,
  "nbf":1490577501,
  "name":"Joe"
}
```

### The used claims in the sensenet ECM tokens:
**_iss_**: `issuer` identifies the principal that issued the token  
**_sub_**: `subject` identifies the principal that is the subject of the token  
**_aud_**: `audience` identifies the recipients that the token is intended for  
**_exp_**: `expiration time` identifies the time whereupon the token will not be accepted  
**_iat_**: `issued at` identifies the time when the token was issued  
**_nbf_**: `not before` identifies the time before that the token can not be accepted  
**_name_**: `name` identifies the name of the user whom the token was issued to

The `iss, sub, aud` claims can be configured and remains the same unless you change them in the web.config. The other claims dinamically change on new token creation.

## Considerations for client developers ##

Once the client application has got the access token and the refresh token, it has to persist them preferably in some local browser storage for later usage. However the refresh token also contains the same claims as the access token, its claims - at least `iat, nbf` and `exp` - have different values. It happens because of their different use. An access token will be immediately valid and accepted after its creation, but the refresh token is not. The refresh token will be valid and accepted by the service only when the access token is expired. Therefore the client should extract the expiration time of the tokens into an application lifetime variable and constantly check it when the client try to access a content. Content access request have to include the access token into the according HTTP header (specified as `AuthenticatedServiceRequest` earlier). In case when the access tokens expiration check results true the client must check the refresh token's expiration. If this results false, the client have to send a `RefreshRequest` (specified earlier) to the service. A `RefreshRequest` will reply with a new access token, that must replace the old one. If the check results true, the client cannot access protected content unless sending a new `LoginRequest` to the service with the username and password of the user. Because of the sensitive nature of user's credentials, we do not recommend the client to persist them. When the client wants to log out, a `LogoutRequest` must be sent and if it responds in a status 200 the client stored credentials should be deleted. As the lifetime of both the access and refresh tokens can be changed in the service's configuration, it is very important to choose them wisely to support the fluent communication between the two part. Wrong settings can disrupt efficiency of turn arounds and slow down the whole system.

You can learn more about how you can use Web Token Authentication in sn-client-js from the [following tutorial](/docs/tutorials/how-to-use-jwt-in-sn-client-js.md).