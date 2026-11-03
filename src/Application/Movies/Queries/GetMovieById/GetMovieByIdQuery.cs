namespace CleanWebApi.Application.Movies.Queries.GetMovieById;

using Entities;
using Mediator;

public class GetMovieByIdQuery : IRequest<Movie>
{
    public Guid Id { get; init; }
}
