namespace CleanWebApi.Application.Reviews.Commands.UpdateReview;

using Common.Enums;
using Common.Exceptions;
using Entities;
using Mediator;

public class UpdateReviewHandler(IReviewsRepository reviewsRepository) : IRequestHandler<UpdateReviewCommand, Review>
{
    public async ValueTask<Review> Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await reviewsRepository.UpdateReview(request.Id, request.Stars, request.Text, cancellationToken);

        NotFoundException.ThrowIfNull(review, EntityType.Review);

        return review;
    }
}
