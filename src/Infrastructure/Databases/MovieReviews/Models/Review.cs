namespace CleanWebApi.Infrastructure.Databases.MovieReviews.Models;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
internal record Review : Entity
{
    public int Stars { get; set; }

    public Guid ReviewedMovieId { get; set; }

    public Movie ReviewedMovie { get; init; } = null!;

    public Guid ReviewAuthorId { get; set; }

    public Author ReviewAuthor { get; init; } = null!;
}
