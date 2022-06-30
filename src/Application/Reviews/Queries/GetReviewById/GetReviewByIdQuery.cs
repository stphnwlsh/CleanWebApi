namespace CleanWebApi.Application.Reviews.Queries.GetReviewById;

using System.ComponentModel.DataAnnotations;
using Entities;
using MediatR;

public class GetReviewByIdQuery : IRequest<Review>
{
    public Guid Id { get; init; }
}
