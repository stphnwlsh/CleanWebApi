---
name: paging
description: Offset paging for every list (offset, limit, totalCount, Page of T), its defaults and limits, and stable ordering. Use when adding or changing anything that returns a list.
metadata:
  scope: shared
---

# Paging

## The rule

Every list is paged with an offset and a limit, and no endpoint returns an unbounded list. A page is `Page<T>(Items, Offset, Limit, TotalCount)` from `src/Application/Common/Paging/Page.cs`. The default limit is `Paging.DefaultLimit` (10) and the maximum is `Paging.MaxLimit` (50). To get the next page, a caller adds the limit to the offset.

Lists return summaries (`MovieSummary`, `AuthorSummary`) worked out in SQL, not full entities with their children. A by-id query returns the full entity.

## Steps

1. Give the query `Offset` (default 0) and `Limit` (default `Paging.DefaultLimit`) properties, plus an optional `Query` search term if the list can be searched.
2. Validate them with `ValidOffset()`, `ValidLimit()` and `ValidSearchTerm()` (`validation`).
3. Have the repository return `(List<T> Items, int TotalCount)`. Count before you skip and take.
4. Order by something that makes a stable order, ending with the id, so a row never shows up on two pages.
5. Build the page in the handler: `new Page<T>(items, request.Offset, request.Limit, totalCount)`.

## Example

```csharp
// src/Infrastructure/Databases/MovieReviews/EntityFrameworkMovieReviewsRepository.cs
var totalCount = await movies.CountAsync(cancellationToken);

var result = await movies
    .OrderBy(m => m.Title)
    .ThenBy(m => m.Id)
    .Skip(offset)
    .Take(limit)
    .Select(m => new
    {
        m.Id,
        m.Title,
        ReviewCount = m.Reviews.Count,
        AverageStars = m.Reviews.Average(r => (double?)r.Stars)
    })
    .ToListAsync(cancellationToken);
```

## Don't

- Add an endpoint or query that returns every row.
- Use cursors or page numbers. Offset and limit everywhere; a presentation layer that needs cursors (GraphQL connections) wraps the offset.
- Order only by a column that can repeat, such as a title.
- `Include` child collections on a list just to count or average them. Project the numbers in SQL.

## Check

``` bash
dotnet test --filter "FullyQualifiedName~GetMovies|FullyQualifiedName~GetAuthors|FullyQualifiedName~GetReviews"
```
