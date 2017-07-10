---
title: "Cross-origin resource sharing"
source_url: 'https://github.com/SenseNet/sensenet/docs/cors.md'
category: Development
version: v7.0
tags: [CORS, authentication, jwt, login, origin, http headers, preflight, OData, REST]
---

# Cross-origin resource sharing
[Cross-origin resource sharing](http://en.wikipedia.org/wiki/Cross-origin_resource_sharing) (CORS) is a technique that allows client-side web developers to access resources from a *different domain*. Shared JavaScript files or images are good examples for this. However cross-origin requests can also be used by hackers and malicious sites to access confidential information if a site is not protected against [Cross Site Request Forgery](http://hu.wikipedia.org/wiki/Cross-site_request_forgery) (CSRF) attacks. This is why browsers apply strict rules for these operations to prevent hackers from accessing the portal from external sites.

> **sensenet ECM** supports CORS OData requests from **version 6.4** and CORS file download requests from **version 6.5.3**. In this article operators and developers may learn about CORS settings and how we prevent cross-domain attacks.}}

All the information on this page refers to our [OData REST API](/docs/odata-rest-api.md), as that is the most important entry point for client developers to the repository.

## CORS basics
The CORS specification defines two kinds of cross-origin request protocols:
- simple CORS, using a single GET or POST request 
- advanced CORS, using a *preflight request* (supported from sensenet ECM **version 6.5.4 patch 8**)

For details see the following article:
- [CORS on Mozilla Developer Network](https://developer.mozilla.org/en-US/docs/Web/HTTP/Access_control_CORS)

## Simple cross-domain requests
All OData AJAX requests sent to the portal will receive a response that contains the following *http header*:

```txt
Access-Control-Allow-Origin: <domain>
```

The *domain* placeholder above is filled dynamically with the requested origin domain, *if it is allowed to access the portal* (see next section for details). In case of non-CORS requests it will be the domain that *the request was sent to*. For example if you send a GET request to the following URL:

```txt
http://example.com/odata.svc/workspaces('myworkspace')
```

...than the response will contain the following header:

```txt
Access-Control-Allow-Origin: http://example.com
```

When a browser receives the response above, it will allow the JavaScript runtime to *access the results* only if it was sent from an html page that came from the *same domain*. E.g. if this AJAX request was made on an html page that was downloaded from a different, malicious website, the request would fail.

> Please note that in this case the request was already executed successfully on the server, it is just the client-side JavaScript code that is not allowed to access the results. See the next section about how we use origin check to protect our portals against requests that would cause harm even if the client-side code does not receive the result.

### Origin check 
In case of cross-domain requests all modern browsers send the *Origin* header to the server, containing the domain of the original page. Sensenet ECM always checks for the Origin header and if it is different than the requested domain and it is not included in a **whitelist**, the request will fail on the server without being able to do any harm. 

> Unlike the old *Referer* header that contains the whole url, the *Origin* header contains only the domain and **it cannot be modified after the browser has sent the request**, meaning it is reliable.

## Settings
You can manage CORS-related settings in the following [Settings](http://wiki.sensenet.com/Settings) content in the [Content Repository](/docs/content-repository.md)
- */Root/System/Settings/Portal.settings*

### Allowed origin domains
There is a **whitelist** that contains the trusted 3rd party domains that may send CORS requests to the portal. If a CORS request arrives with an origin that is in this whitelist, the request will execute - otherwise the client will receive a *Forbidden* status code. You can manage this whitelist the following way:

The list may contain internal or external domains:

```javascript
{
   AllowedOriginDomains: [ "example.com", "www.example.com", "trustedsite.com", "localhost" ]
}
```

It is also possible to open the Content Repository to *everyone* by providing a *wildcard* as the only allowed origin.

```javascript
{
   AllowedOriginDomains: [ "*" ]
}
```

### Allowed methods and headers
*from sensenet ECM version 7.0*

If you need to, you may customize the list of allowed methods (http verbs) for CORS requests on your sensenet ECM instance. If something is missing from the default list, or you want to restrict the allowed request methods for security reasons, you can do so by providing the following line in the setting:

```javascript
{
   AllowedMethods: [ "GET", "POST", "PATCH", "DELETE", "MERGE", "PUT" ]
}
```

You can also customize the list of allowed http headers for CORS requests (for example add your custom headers):

```javascript
{
   AllowedHeaders: [ "X-Authentication-Type",
            "X-Refresh-Data", "X-Access-Data",
            "X-Requested-With", "Authorization", "Content-Type" ]
}
```

## Preflight request
If the client-side JavaScript code tries to make a cross-domain AJAX request with any http method *other than GET* or *POST* (e.g. DELETE or PATCH), a *preflight request* is made to the server using the OPTIONS method to check whether it is allowed to send a CORS request for that particular resource.

## Authentication
Of course cross-domain requests are still need to be authenticated. CSRF attacks are designed to make cross-domain calls in the name of a user who is already logged in to the targeted site (e.g. on a different browser tab). Otherwise the whole mechanism described above does not apply because the malicious request will not even reach the point when it would make some damage.

Currently the portal always allows authenticated requests, except if the allowed origin is a wildcard ("*"). This means that the credentials header is always set to true and browsers will allow ajax requests to send cookies to the server.

```txt
Access-Control-Allow-Credentials: true
```

### Logging in using JWT authentication
*from sensenet ECM version 7.0*

To log in to a portal on a different domain than the current one (the target portal is where you want to send CORS requests), you can use [JWT authentication](/docs/web-token-authentication). The easiest way to do that is using the [Client JS API](/docs/tutorials/how-to-use-jwt-in-sn-client-js) instead of implementing the protocol by yourself.

## Command line tools
All the protection and protocol above is related to browsers. Web requests made by *command line tools* do not contain these http headers and the response is not checked by the tool in any way. This means the whole cross-origin protection does not apply to command line tools.

## Examples
To send a cross-origin request from JavaScript, you simply have to provide the absolute url of the resource you want to query (e.g. an OData request to a sensenet ECM portal) and tell the browser that you want it to send user credentials with the request:

```txt
withCredentials: true
```

The following examples send a CORS request to a sensenet ECM portal to get memos and create a new one. Of course you will have to be authenticated on the target site to make this work (see authentication section above for details).

```javascript
// GET request: load memos from the Memos list
$.ajax({
    url: "http://example.com/OData.svc/workspaces/Project/madridprojectworkspace/Memos",
    xhrFields: {
        withCredentials: true
    },
    dataType: "json",
    type: 'GET',
    success: function () {
        console.log('hello');
    },
    error: function () {
        console.log('error!');
    }
});

// POST request: create a memo
$.ajax({
    url: "http://example.com/OData.svc/workspaces/Project/madridprojectworkspace('Memos')",
    xhrFields: {
        withCredentials: true
    },
    dataType: "json",
    type: 'POST',
    data: "models=[" + JSON.stringify({ '__ContentType': 'Memo', 'Index': 123 }) + "]",
    success: function () {
        console.log('hello');
    },
    error: function () {
        console.log('error!');
    }
});		
```