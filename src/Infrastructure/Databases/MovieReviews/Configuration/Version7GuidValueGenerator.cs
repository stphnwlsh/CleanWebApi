namespace CleanWebApi.Infrastructure.Databases.MovieReviews.Configuration;

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

/// <summary>
/// Gives new rows a version 7 GUID. They start with a timestamp, so new rows land at the end of the primary key
/// index instead of at a random place in it.
/// </summary>
internal sealed class Version7GuidValueGenerator : ValueGenerator<Guid>
{
    public override bool GeneratesTemporaryValues => false;

    public override Guid Next(EntityEntry entry)
    {
        return Guid.CreateVersion7();
    }
}
