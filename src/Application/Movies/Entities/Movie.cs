namespace CleanWebApi.Application.Movies.Entities;

using CleanWebApi.Application.Common.Entities;
using CleanWebApi.Application.Reviews.Entities;

public record Movie : Entity
{
    public string Title { get; init; }

    public ICollection<Review> Reviews { get; init; }
}
