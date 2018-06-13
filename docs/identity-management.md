---
title: "Identity management"
source_url: 'https://github.com/SenseNet/sensenet/blob/master/docs/identity-management.md'
category: Concepts
version: v7.0
tags: [user, profile, permission, domain, security]
---

# Identity management

## User

## User profile

## Domain

### Default domain
When a user tries to log in without a domain name (only providing a username), we use the configured default domain. If you want to set the default domain of your portal, add this value to your config file:

```xml
<sensenet>
   <identityManagement>
       <add key="DefaultDomain" value="domain" />
   </identityManagement>
</sensenet>
```
Default value is the _BuiltIn_ domain.

## Group

