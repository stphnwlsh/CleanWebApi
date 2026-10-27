namespace CleanWebApi.Presentation.Controllers.Reviews.Requests;

public class CreateReviewRequest
{
    public Guid AuthorId { get; init; }

    public Guid MovieId { get; init; }

    public int Stars { get; init; }
}
