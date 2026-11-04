namespace CleanWebApi.Application.Movies;

using Common.Paging;
using Entities;

public interface IMoviesRepository
{
    public Task<Page<MovieSummary>> GetMovies(
        string? query,
        PageRequest page,
        CancellationToken cancellationToken);

    public Task<Movie?> GetMovieById(Guid id, CancellationToken cancellationToken);

    public Task<bool> MovieExists(Guid id, CancellationToken cancellationToken);
}
