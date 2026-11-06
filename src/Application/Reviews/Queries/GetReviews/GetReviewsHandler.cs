namespace CleanWebApi.Application.Reviews.Queries.GetReviews;

using Common.Paging;
using Entities;
using Mediator;

public class GetReviewsHandler(IReviewsRepository repository) : IRequestHandler<GetReviewsQuery, Page<Review>>
{
    public async ValueTask<Page<Review>> Handle(GetReviewsQuery request, CancellationToken cancellationToken)
    {
        return await repository.GetReviews(request.MovieId, request.AuthorId, request.Query, request.MinStars, new PageRequest(request.Offset, request.Limit), cancellationToken);
    }
}
