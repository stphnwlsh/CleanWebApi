namespace CleanWebApi.Presentation.Endpoints.Reviews;

using Errors;
using Common;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Requests;
using Swashbuckle.AspNetCore.Annotations;
using Commands = Application.Reviews.Commands;
using Entities = Application.Reviews.Entities;
using Queries = Application.Reviews.Queries;

[SwaggerTag("Create new Reviews, Lookup all Reviews or find them by Id, Update and Delete Reviews", "https://github.com/stphnwlsh/cleanwebapi")]
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
    /// <returns>
    /// A list of all the Reviews
    /// </returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /Reviews
    ///
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(List<Entities.Review>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IList<Entities.Review>>> GetReviews()
    {
        return await this.Mediator.Send(new Queries.GetReviews.GetReviewsQuery());
    }

    /// <summary>
    /// Lookup an Review by its Id
    /// </summary>
    /// <param name="id"></param>
    /// <returns>
    /// The Review with the specified Id
    /// </returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /Reviews/00000000-0000-0000-0000-000000000000
    ///
    /// </remarks>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Entities.Review), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Entities.Review>> GetReviewsById(Guid id)
    {
        return this.Ok(await this.Mediator.Send(new Queries.GetReviewById.GetReviewByIdQuery { Id = id }));
    }

    /// <summary>
    /// Create an Review by its Id
    /// </summary>
    /// <param name="request"></param>
    /// <returns>
    /// The Review that was created
    /// </returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /Reviews
    ///     {
    ///         "authorId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "movieId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "stars": 5
    ///     }
    ///
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(Entities.Review), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Entities.Review>> CreateReview(CreateReviewRequest request)
    {
        var command = new Commands.CreateReview.CreateReviewCommand()
        {
            AuthorId = request.AuthorId, MovieId = request.MovieId, Stars = request.Stars
        };

        return this.Created(this.HttpContext.Request.GetEncodedUrl(), await this.Mediator.Send(command));
    }

    /// <summary>
    /// Delete a Review by its Id
    /// </summary>
    /// <param name="id"></param>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /Reviews/00000000-0000-0000-0000-000000000000
    ///
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(Entities.Review), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Entities.Review>> DeleteReviewById(Guid id)
    {
        _ = await this.Mediator.Send(new Commands.DeleteReview.DeleteReviewCommand { Id = id });

        return this.NoContent();
    }

    /// <summary>
    /// Update a Review by its Id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /Reviews/00000000-0000-0000-0000-000000000000
    ///     {
    ///         "authorId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "movieId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "stars": 5
    ///     }
    ///
    /// </remarks>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Entities.Review), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Entities.Review>> UpdateReviewById(Guid id, UpdateReviewRequest request)
    {
        var command = new Commands.UpdateReview.UpdateReviewCommand
        {
            Id = id, AuthorId = request.AuthorId, MovieId = request.MovieId, Stars = request.Stars
        };

        _ = await this.Mediator.Send(command);

        return this.NoContent();
    }
}
