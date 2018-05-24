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
If you want to set the default domain of your portal login write this section to the webcofig.:

```xml
<identityManagement>
    <add key="DefaultDomain" value="domain" />
</identityManagement>
```
Default setting is the "builtin" domain.

## Group

