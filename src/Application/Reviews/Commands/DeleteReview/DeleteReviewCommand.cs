namespace CleanWebApi.Application.Reviews.Commands.DeleteReview;

using System.ComponentModel.DataAnnotations;
using MediatR;

public class DeleteReviewCommand : IRequest<bool>
{
    public Guid Id { get; init; }
}
