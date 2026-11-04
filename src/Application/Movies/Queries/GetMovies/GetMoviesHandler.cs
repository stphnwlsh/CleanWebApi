namespace CleanWebApi.Application.Movies.Queries.GetMovies;

using Common.Paging;
using Entities;
using Mediator;

public class GetMoviesHandler(IMoviesRepository repository) : IRequestHandler<GetMoviesQuery, Page<MovieSummary>>
{
    public async ValueTask<Page<MovieSummary>> Handle(GetMoviesQuery request, CancellationToken cancellationToken)
    {
        return await repository.GetMovies(request.Query, new PageRequest(request.Offset, request.Limit), cancellationToken);
    }
}
