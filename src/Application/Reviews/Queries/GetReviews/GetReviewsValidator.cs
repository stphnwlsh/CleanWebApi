namespace CleanWebApi.Application.Reviews.Queries.GetReviews;

using Common.Paging;
using FluentValidation;

public class GetReviewsValidator : AbstractValidator<GetReviewsQuery>
{
    public GetReviewsValidator()
    {
        _ = this.RuleFor(r => r.MovieId).NotEqual(Guid.Empty).WithMessage("A Movie Id can't be empty. Leave it out to list every movie.");
        _ = this.RuleFor(r => r.AuthorId).NotEqual(Guid.Empty).WithMessage("An Author Id can't be empty. Leave it out to list every author.");
        _ = this.RuleFor(r => r.Query).ValidSearchTerm();
        _ = this.RuleFor(r => r.MinStars).InclusiveBetween(1, 5).WithMessage("A minimum star rating must be between 1 and 5.");
        _ = this.RuleFor(r => r.Offset).ValidOffset();
        _ = this.RuleFor(r => r.Limit).ValidLimit();
    }
}
