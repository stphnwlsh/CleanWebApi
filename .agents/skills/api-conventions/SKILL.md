---
name: api-conventions
description: The REST conventions for this API - plural lowercase routes, status codes per operation, problem details for errors, JSON casing and paging parameters. Use when choosing a route, a status code or a response shape.
metadata:
  scope: presentation
---

# API conventions

## The rule

| Topic | Convention |
| --- | --- |
| Routes | `[Route("api/<plural-lowercase-resource>")]` on the controller, and `{id:guid}` for one item: `/api/movies`, `/api/reviews/{id}` |
| Lists | `GET` with `offset` and `limit` (and `query` to search) in the query string, returning `Page<T>` (`paging`) |
| Read one | `GET {id}` returns `200` with the entity, `404` when it doesn't exist |
| Create | `POST` returns `201` with the entity and a `Location` header from `CreatedAtAction` |
| Update | `PUT {id}` with only the fields that may change, returns `204` |
| Delete | `DELETE {id}` returns `204` |
| Invalid input | `400` with validation problem details (errors grouped by property, or by JSON path when the body can't be read) |
| Not found | `404` problem details with a readable `detail` |
| Unexpected | `500` problem details with no internals. `ApplicationExceptionHandler` logs the exception as `Error` (`serilog`) |
| JSON | camelCase names, enums as camelCase strings, nulls left out, numbers strict (no numbers as strings) |
| Auth | `ApiControllerBase` has `[Authorize]`; only health, the OpenAPI document and Scalar are anonymous (`optional-token-auth`) |
| Tags | One OpenAPI tag per resource, taken from the controller name |

Errors are RFC 9457 problem details. `ApplicationExceptionHandler` (`src/Presentation/Errors/`) writes them for exceptions from Application, `[ApiController]` writes them when a body or route value can't be bound, and `AddProblemDetails()` with `UseStatusCodePages()` covers the rest (an unknown route, an unsupported media type).

## Steps

1. Pick the route and verb from the table. A new resource gets a new controller, which gives it a new tag.
2. Declare every status the action can return with `[ProducesResponseType]` (`expose-a-use-case`).
3. If an update can't change some fields, leave them out of the request class and say so in the remarks.
4. Add an integration test for each status code the action documents.

## Don't

- Use verbs or singular nouns in routes (`/api/getMovie`, `/api/movie`), or `[controller]` tokens, which give `Movies` instead of `movies`.
- Return `200` with an error in the body, or `404` for an empty list.
- Add a separate `/search` route. Search is the `query` parameter on the list.
- Version the API until there's a second version to show.

## Check

``` bash
dotnet test tests/Presentation.Tests.Integration
```
