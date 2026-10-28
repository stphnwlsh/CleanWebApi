namespace CleanWebApi.Presentation.Controllers.Reviews;

using Application.Common.Paging;
using Common;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Requests;
using Commands = Application.Reviews.Commands;
using Entities = Application.Reviews.Entities;
using Queries = Application.Reviews.Queries;

public class ReviewsController : ApiControllerBase
{
    private readonly ILogger<ReviewsController> logger;

    public ReviewsController(ILogger<ReviewsController> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Lookup all Reviews
    /// </summary>
    /// <remarks>
    /// Returns every review along with the movie it is about and the author who wrote it.
    ///
    /// Sample request:
    ///
    ///     GET /api/Reviews
    /// </remarks>
    /// <response code="200">A list of all the Reviews.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpGet]
    [EndpointName(nameof(GetReviews))]
    [ProducesResponseType(typeof(List<Entities.Review>), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<ActionResult<IList<Entities.Review>>> GetReviews()
    {
        return await this.Mediator.Send(new Queries.GetReviews.GetReviewsQuery());
    }

    /// <summary>
    /// Search Reviews by movie or author
    /// </summary>
    /// <remarks>
    /// Returns one page of reviews, optionally only those of one movie, by one author, or both,
    /// ordered by movie then author. When more results remain, the response holds a nextCursor.
    /// Send it back as the cursor to fetch the next page.
    ///
    /// Sample request:
    ///
    ///     GET /api/Reviews/search?movieId=3fa85f64-5717-4562-b3fc-2c963f66afa6&amp;limit=10
    /// </remarks>
    /// <param name="movieId">Only return reviews of this movie. Leave it out for every movie.</param>
    /// <param name="authorId">Only return reviews by this author. Leave it out for every author.</param>
    /// <param name="limit">The largest number of reviews to return, from 1 to 50.</param>
    /// <param name="cursor">The nextCursor from a previous page. Leave it out for the first page.</param>
    /// <response code="200">One page of matching Reviews.</response>
    /// <response code="400">An Id, the limit or the cursor is not valid.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpGet("search")]
    [EndpointName(nameof(SearchReviews))]
    [ProducesResponseType(typeof(Page<Entities.Review>), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<ActionResult<Page<Entities.Review>>> SearchReviews(
        [FromQuery] Guid? movieId,
        [FromQuery] Guid? authorId,
        [FromQuery] int limit = Paging.DefaultLimit,
        [FromQuery] string cursor = null)
    {
        return this.Ok(await this.Mediator.Send(new Queries.SearchReviews.SearchReviewsQuery
        {
            MovieId = movieId,
            AuthorId = authorId,
            Limit = limit,
            Cursor = cursor
        }));
    }

    /// <summary>
    /// Lookup a Review by its Id
    /// </summary>
    /// <remarks>
    /// Returns a single review along with the movie it is about and the author who wrote it.
    ///
    /// Sample request:
    ///
    ///     GET /api/Reviews/3fa85f64-5717-4562-b3fc-2c963f66afa6
    /// </remarks>
    /// <param name="id">The unique identifier of the review.</param>
    /// <response code="200">The Review with the specified Id.</response>
    /// <response code="400">The Id is not valid.</response>
    /// <response code="404">No Review exists with the specified Id.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpGet("{id:guid}")]
    [EndpointName("GetReviewById")]
    [ProducesResponseType(typeof(Entities.Review), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<ActionResult<Entities.Review>> GetReviewsById(Guid id)
    {
        return this.Ok(await this.Mediator.Send(new Queries.GetReviewById.GetReviewByIdQuery { Id = id }));
    }

    /// <summary>
    /// Create a Review
    /// </summary>
    /// <remarks>
    /// Creates a review of a movie by an author. Both the author and the movie must already exist,
    /// and the star rating must be between 1 and 5.
    ///
    /// Sample request:
    ///
    ///     POST /api/Reviews
    ///     {
    ///         "authorId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "movieId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "stars": 5
    ///     }
    /// </remarks>
    /// <param name="request">The review to create.</param>
    /// <response code="201">The Review that was created. The Location header holds its URL.</response>
    /// <response code="400">The request is not valid, for example the star rating is out of range.</response>
    /// <response code="404">The author or the movie does not exist.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpPost]
    [EndpointName(nameof(CreateReview))]
    [ProducesResponseType(typeof(Entities.Review), StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<ActionResult<Entities.Review>> CreateReview(CreateReviewRequest request)
    {
        var command = new Commands.CreateReview.CreateReviewCommand()
        {
            AuthorId = request.AuthorId,
            MovieId = request.MovieId,
            Stars = request.Stars
        };

        return this.Created(this.HttpContext.Request.GetEncodedUrl(), await this.Mediator.Send(command));
    }

    /// <summary>
    /// Delete a Review by its Id
    /// </summary>
    /// <remarks>
    /// Permanently removes a review.
    ///
    /// Sample request:
    ///
    ///     DELETE /api/Reviews/3fa85f64-5717-4562-b3fc-2c963f66afa6
    /// </remarks>
    /// <param name="id">The unique identifier of the review.</param>
    /// <response code="204">The Review was deleted.</response>
    /// <response code="400">The Id is not valid.</response>
    /// <response code="404">No Review exists with the specified Id.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpDelete("{id:guid}")]
    [EndpointName("DeleteReview")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> DeleteReviewById(Guid id)
    {
        _ = await this.Mediator.Send(new Commands.DeleteReview.DeleteReviewCommand { Id = id });

        return this.NoContent();
    }

    /// <summary>
    /// Update a Review by its Id
    /// </summary>
    /// <remarks>
    /// Replaces the author, movie and star rating of an existing review. The author and the movie
    /// must already exist, and the star rating must be between 1 and 5.
    ///
    /// Sample request:
    ///
    ///     PUT /api/Reviews/3fa85f64-5717-4562-b3fc-2c963f66afa6
    ///     {
    ///         "authorId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "movieId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "stars": 5
    ///     }
    /// </remarks>
    /// <param name="id">The unique identifier of the review.</param>
    /// <param name="request">The new details of the review.</param>
    /// <response code="204">The Review was updated.</response>
    /// <response code="400">The request is not valid, for example the star rating is out of range.</response>
    /// <response code="404">The review, the author or the movie does not exist.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpPut("{id:guid}")]
    [EndpointName("UpdateReview")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> UpdateReviewById(Guid id, UpdateReviewRequest request)
    {
        var command = new Commands.UpdateReview.UpdateReviewCommand
        {
            Id = id,
            AuthorId = request.AuthorId,
            MovieId = request.MovieId,
            Stars = request.Stars
        };

        _ = await this.Mediator.Send(command);

        return this.NoContent();
    }
}
