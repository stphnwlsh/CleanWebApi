namespace CleanWebApi.Presentation.Errors;

using Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

/// <summary>
/// Maps exceptions raised by the Application layer to RFC 9457 problem details responses. Anything it doesn't
/// expect becomes a 500 and is logged here as an error, because handling the exception stops ASP.NET Core logging it.
/// </summary>
/// <param name="problemDetailsService">Writes the problem details response.</param>
/// <param name="logger">Where unexpected exceptions are logged.</param>
public partial class ApplicationExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ApplicationExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            ValidationException validationException => new HttpValidationProblemDetails(
                validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray()))
            {
                Status = StatusCodes.Status400BadRequest
            },
            NotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Detail = exception.Message
            },
            BadHttpRequestException badHttpRequestException => new ProblemDetails
            {
                Status = badHttpRequestException.StatusCode,
                Detail = badHttpRequestException.Message
            },
            _ => this.Unexpected(httpContext, exception)
        };

        httpContext.Response.StatusCode = problemDetails.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }

    private ProblemDetails Unexpected(HttpContext httpContext, Exception exception)
    {
        LogUnexpected(logger, exception, httpContext.Request.Method, httpContext.Request.Path);

        return new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError
        };
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "An unexpected exception stopped {RequestMethod} {RequestPath}")]
    private static partial void LogUnexpected(ILogger logger, Exception exception, string requestMethod, string requestPath);
}
