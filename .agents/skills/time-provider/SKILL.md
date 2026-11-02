---
name: time-provider
description: How code reads the current time through TimeProvider instead of DateTime.UtcNow, where the system clock is registered, and how tests fix the clock with FakeTimeProvider. Use when code needs the current date or time, sets a created or modified date, or a test depends on time.
metadata:
  scope: shared
---

# Time provider

## The rule

Nothing reads the clock directly. Code that needs the time takes `TimeProvider` through its constructor and calls `GetUtcNow()`. Production gets `TimeProvider.System`, registered once in `src/Infrastructure/DependencyInjection.cs`. Tests pass a `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`) set to a fixed moment, so every date is known in advance.

Times are stored and compared in UTC. Only the Infrastructure layer reads the time today: the repository stamps `DateCreated` and `DateModified` on writes.

## Steps

1. Take `TimeProvider timeProvider` in the constructor, check it with `ArgumentNullException.ThrowIfNull`, and keep it in a `private readonly` field.
2. Read the time once per operation into a local (`var now = this.timeProvider.GetUtcNow().UtcDateTime;`) and use that value everywhere in the operation, so created and modified dates match.
3. In repository tests, use the clock from `MovieReviewsTestDatabase` (fixed at 2009-12-31 23:51:01 UTC) and assert against it.
4. To test something that depends on time passing, call `Advance(TimeSpan)` on the fake.

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
// tests/Infrastructure.Tests.Integration/Databases/MovieReviews/EntityFrameworkMovieReviewsRepositoryTests.cs
updatedReview.DateModified.ShouldBe(this.database.TimeProvider.GetUtcNow().UtcDateTime);
```

## Don't

- Call `DateTime.Now`, `DateTime.UtcNow` or `DateTimeOffset.UtcNow` in `src`.
- Read the time more than once in one operation.
- Store local times.
- Assert a date with a tolerance ("within a second of now"). Fix the clock and assert the exact value.

The one exception is a test that creates a token for the real JWT handler, which checks expiry against the system clock (`tests/Presentation.Tests.Integration/Authentication/AuthenticationTests.cs`).

## Check

``` bash
grep -rn "DateTime.UtcNow\|DateTime.Now\|DateTimeOffset.UtcNow" src   # expect no output
dotnet test tests/Infrastructure.Tests.Integration
```
