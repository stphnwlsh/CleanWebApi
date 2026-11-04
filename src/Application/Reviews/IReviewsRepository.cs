namespace CleanWebApi.Application.Reviews;

using Common.Paging;
using Entities;

public interface IReviewsRepository
{
    public Task<Review> CreateReview(
        Guid authorId,
        Guid movieId,
        int stars,
        CancellationToken cancellationToken);

    public Task<bool> DeleteReview(Guid id, CancellationToken cancellationToken);

    public Task<Page<Review>> GetReviews(
        Guid? movieId,
        Guid? authorId,
        PageRequest page,
        CancellationToken cancellationToken);

    public Task<List<Review>> GetReviewsForMovies(IReadOnlyCollection<Guid> movieIds, CancellationToken cancellationToken);

    public Task<List<Review>> GetReviewsForAuthors(IReadOnlyCollection<Guid> authorIds, CancellationToken cancellationToken);

    public Task<Review?> GetReviewById(Guid id, CancellationToken cancellationToken);

    public Task<Review?> UpdateReview(Guid id, int stars, CancellationToken cancellationToken);
}
