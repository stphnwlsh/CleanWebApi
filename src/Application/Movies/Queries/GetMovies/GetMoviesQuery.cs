namespace CleanWebApi.Application.Movies.Queries.GetMovies;

using Entities;
using MediatR;

public class GetMoviesQuery : IRequest<List<Movie>>
{
}
