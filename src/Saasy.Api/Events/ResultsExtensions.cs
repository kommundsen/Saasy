using Microsoft.AspNetCore.Http.HttpResults;

namespace Saasy.Api.Events;

internal static class ResultsExtensions
{
    // Returns 429 with an empty body and a Retry-After header set to retryAfterSeconds.
    internal static IResult TooManyRequests(this IResultExtensions _, int retryAfterSeconds)
        => new TooManyRequestsResult(retryAfterSeconds);

    private sealed class TooManyRequestsResult(int retryAfterSeconds) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            httpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
            return Task.CompletedTask;
        }
    }
}
