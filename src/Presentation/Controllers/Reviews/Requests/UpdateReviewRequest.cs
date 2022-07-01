namespace CleanWebApi.Presentation.Controllers.Reviews.Requests;

using System.ComponentModel.DataAnnotations;

public class UpdateReviewRequest
{
    [Required]
    public Guid AuthorId { get; init; }

    [Required]
    public Guid MovieId { get; init; }

    [Required]
    [Range(1, 5)]
    public int Stars { get; init; }
}
