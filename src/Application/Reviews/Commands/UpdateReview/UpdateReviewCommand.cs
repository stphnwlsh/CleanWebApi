namespace CleanWebApi.Application.Reviews.Commands.UpdateReview;

using Entities;
using Mediator;

/// <summary>
/// Changes the star rating of a review, and its text if there's new text. The author and the movie of a review can't change.
/// </summary>
public class UpdateReviewCommand : IRequest<Review>
{
    /// <summary>The unique identifier of the review.</summary>
    public Guid Id { get; init; }

    /// <summary>The new star rating, from 1 to 5.</summary>
    public int Stars { get; init; }

    /// <summary>The new text. Leave it null to keep the text the review has.</summary>
    public string? Text { get; init; }
}
