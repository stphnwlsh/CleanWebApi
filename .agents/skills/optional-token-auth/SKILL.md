---
name: optional-token-auth
description: How optional JWT bearer authentication works - off unless Authentication:Authority is set, every endpoint asks for authorization, and the few that stay anonymous. Use when protecting a new endpoint, making one public, or changing authentication settings.
metadata:
  scope: shared
---

# Optional token authentication

## The rule

Authentication is off until someone sets `Authentication:Authority` (and usually `Authentication:Audience`). Then every request needs a valid JWT bearer token from that authority. The `dotnet new` options `--authority` and `--audience` fill those settings in.

Endpoints always ask for authorization, whether or not authentication is on. When it's off, the default policy lets every request through, so turning it on is a settings change, not a code change. Only health checks and the API reference (where a template has them) are anonymous. The version endpoint is protected like everything else; telemetry carries `service.version` for anyone who needs to know what's deployed. The Lambda template has no token authentication; API Gateway or the caller's IAM role does that job.

Settings are read through `AuthenticationSettings.FromConfiguration` (`Enabled` is true when the authority isn't empty).

## Steps

1. A new endpoint or group gets `.RequireAuthorization()` (or the presentation layer's equivalent). That's the default; don't skip it.
2. Only make something anonymous if it has to work without a token: health and the API reference. Say why in a comment.
3. When authentication is on, the template tells clients how to get a token: a bearer scheme in the OpenAPI document where a template has one, and the protected resource metadata in MCP.
4. Test both modes: one test with no authority (anonymous works), one with an authority and no token (401).

## Don't

- Check `Enabled` inside an endpoint. The policy setup in startup handles both modes.
- Put a real authority, client id or secret in `appsettings.json`.
- Use `[AllowAnonymous]` to get a test to pass.

## Check

``` bash
dotnet test --filter "FullyQualifiedName~Authentication"
```
