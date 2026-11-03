namespace CleanWebApi.Application.Reviews.Commands.DeleteReview;

using Common.Enums;
using Common.Exceptions;
using Mediator;

public class DeleteReviewHandler(IReviewsRepository repository) : IRequestHandler<DeleteReviewCommand>
{
    public async ValueTask<Unit> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        if (!await repository.DeleteReview(request.Id, cancellationToken))
        {
            NotFoundException.Throw(EntityType.Review);
        }

        return Unit.Value;
    }
}
