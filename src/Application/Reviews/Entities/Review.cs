namespace CleanWebApi.Application.Reviews.Entities;

using CleanWebApi.Application.Authors.Entities;
using CleanWebApi.Application.Common.Entities;
using CleanWebApi.Application.Movies.Entities;

public record Review : Entity
{
    public int Stars { get; init; }

    public Movie ReviewedMovie { get; init; }

    public Author ReviewAuthor { get; init; }
}
