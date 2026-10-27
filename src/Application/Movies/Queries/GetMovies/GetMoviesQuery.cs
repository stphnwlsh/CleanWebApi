namespace CleanWebApi.Application.Movies.Queries.GetMovies;

using Entities;
using Mediator;

public class GetMoviesQuery : IRequest<List<Movie>>
{
}
