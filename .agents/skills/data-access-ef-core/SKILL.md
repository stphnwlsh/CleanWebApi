---
name: data-access-ef-core
description: How the repository reads and writes the SQLite database with EF Core - scoped DbContext, no-tracking reads, SQL projections for lists, set-based updates and deletes, and safe LIKE searches. Use when adding or changing a repository method or a query.
metadata:
  scope: shared
---

# Data access with EF Core

## The rule

One repository, `EntityFrameworkMovieReviewsRepository` in `src/Infrastructure/Databases/MovieReviews/`, implements every repository interface from Application. It and `MovieReviewsDbContext` are scoped: one per request, never shared between threads. Reads don't track. Lists are projected in SQL. Updates and deletes are set-based. The repository returns Application entities, never database models.

## Steps

1. Add the method to the Application interface first (`IMoviesRepository` and friends), taking a `CancellationToken` last.
2. Implement it in the matching `#region` of the repository, as `virtual async Task<...>` like its neighbours.
3. Reads: start from `this.context.<Set>.AsNoTracking()`, filter, then either
   - **a list**: count, order (ending with the id), skip, take, and `Select` the summary fields so SQL does the counting and averaging (`paging`), or
   - **one entity**: `Include` what the entity needs, `FirstOrDefaultAsync`, then map with `this.mapper.ToDomain(...)` (`mapping-with-mapperly`).
4. Writes:
   - **create**: build the model, set `DateCreated` and `DateModified` from `this.timeProvider.GetUtcNow().UtcDateTime`, `Add`, `SaveChangesAsync`, then read it back with its includes.
   - **update**: `Where(...).ExecuteUpdateAsync(setters => ...)`, always setting `DateModified`.
   - **delete**: `Where(...).ExecuteDeleteAsync(...)`.
5. Text search uses `EF.Functions.Like` with the `ContainsPattern` helper, which escapes `%`, `_` and `\`.
6. Add repository tests (`integration-tests`).

## Example

```csharp
// src/Infrastructure/Databases/MovieReviews/EntityFrameworkMovieReviewsRepository.cs
public async Task<bool> UpdateReview(Guid id, int stars, CancellationToken cancellationToken)
{
    var now = this.timeProvider.GetUtcNow().UtcDateTime;

    var updated = await this.context.Reviews
        .Where(r => r.Id == id)
        .ExecuteUpdateAsync(
            setters => setters
                .SetProperty(r => r.Stars, stars)
                .SetProperty(r => r.DateModified, now),
            cancellationToken);

    return updated > 0;
}
```

```csharp
if (!string.IsNullOrWhiteSpace(query))
{
    var pattern = ContainsPattern(query);

    movies = movies.Where(m => EF.Functions.Like(m.Title, pattern, EscapeCharacter));
}
```

## Don't

- Register the `DbContext` or repository as a singleton. A shared context fails under concurrent requests.
- Use `string.Contains(value, StringComparison.OrdinalIgnoreCase)` in a query. EF Core can't translate it; use the `LIKE` helper.
- Use `DateTime.UtcNow`. Use the injected `TimeProvider` so tests can fix the clock.
- `Include` children on a list query to count them.
- Load an entity, change it and `SaveChanges` to update one column. Use `ExecuteUpdateAsync`.
- Return a database model or `IQueryable` from the repository.

## Check

``` bash
dotnet test tests/Infrastructure.Tests.Integration
```
