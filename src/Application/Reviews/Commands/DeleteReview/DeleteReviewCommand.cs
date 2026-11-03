namespace CleanWebApi.Application.Reviews.Commands.DeleteReview;

using Mediator;

public class DeleteReviewCommand : IRequest
{
    public Guid Id { get; init; }
}
