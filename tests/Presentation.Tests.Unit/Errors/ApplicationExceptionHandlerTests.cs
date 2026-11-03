namespace CleanWebApi.Presentation.Tests.Unit.Errors;

using System.Threading;
using System.Threading.Tasks;
using CleanWebApi.Application.Common.Exceptions;
using CleanWebApi.Presentation.Errors;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Shouldly;
using Xunit;

public class ApplicationExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ShouldReturn_ValidationProblem()
    {
        // Arrange
        var exception = new ValidationException(
        [
            new ValidationFailure("Id", "A Review Id was not supplied."),
            new ValidationFailure("Stars", "A star rating must be between 1 and 5.")
        ]);

        // Act
        var (handled, httpContext, context, logger) = await Handle(exception);

        // Assert
        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);

        var problemDetails = context.ProblemDetails.ShouldBeOfType<HttpValidationProblemDetails>();

        problemDetails.Status.ShouldBe(StatusCodes.Status400BadRequest);
        problemDetails.Errors.Count.ShouldBe(2);
        problemDetails.Errors["Id"].ShouldBe(["A Review Id was not supplied."]);
        problemDetails.Errors["Stars"].ShouldBe(["A star rating must be between 1 and 5."]);
        context.Exception.ShouldBe(exception);
        logger.Collector.Count.ShouldBe(0);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturn_NotFound()
    {
        // Arrange
        var exception = new NotFoundException("Expected Exception");

        // Act
        var (handled, httpContext, context, logger) = await Handle(exception);

        // Assert
        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        context.ProblemDetails.Status.ShouldBe(StatusCodes.Status404NotFound);
        context.ProblemDetails.Detail.ShouldBe("Expected Exception");
        logger.Collector.Count.ShouldBe(0);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturn_BadRequest()
    {
        // Arrange
        var exception = new BadHttpRequestException("Expected Exception");

        // Act
        var (handled, httpContext, context, logger) = await Handle(exception);

        // Assert
        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        context.ProblemDetails.Status.ShouldBe(StatusCodes.Status400BadRequest);
        context.ProblemDetails.Detail.ShouldBe("Expected Exception");
        logger.Collector.Count.ShouldBe(0);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturn_Problem()
    {
        // Arrange
        var exception = new ArgumentException("Expected Exception");

        // Act
        var (handled, httpContext, context, logger) = await Handle(exception);

        // Assert
        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        context.ProblemDetails.Status.ShouldBe(StatusCodes.Status500InternalServerError);
        context.ProblemDetails.Detail.ShouldBeNull();

        var record = logger.Collector.LatestRecord;

        logger.Collector.Count.ShouldBe(1);
        record.Level.ShouldBe(LogLevel.Error);
        record.Exception.ShouldBe(exception);
        record.Message.ShouldBe("An unexpected exception stopped GET /api/movies");
    }

    private static async Task<(bool Handled, HttpContext HttpContext, ProblemDetailsContext Context, FakeLogger<ApplicationExceptionHandler> Logger)> Handle(Exception exception)
    {
        var problemDetailsService = new CapturingProblemDetailsService();
        var logger = new FakeLogger<ApplicationExceptionHandler>();

        var handler = new ApplicationExceptionHandler(problemDetailsService, logger);
        var httpContext = new DefaultHttpContext();

        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = "/api/movies";

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        return (handled, httpContext, problemDetailsService.Context.ShouldNotBeNull(), logger);
    }

    private sealed class CapturingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetailsContext? Context { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            this.Context = context;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            this.Context = context;
            return ValueTask.FromResult(true);
        }
    }
}
