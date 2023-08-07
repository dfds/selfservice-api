using Microsoft.AspNetCore.Mvc;

namespace SelfService.Infrastructure.Api;

public static class CustomObjectResults
{
    [NonAction]
    public static InternalServerErrorResult InternalServerError(object error) => new(error);
}

public class InternalServerErrorResult : ObjectResult
{
    private const int DefaultStatusCode = StatusCodes.Status500InternalServerError;

    public InternalServerErrorResult(object error) : base(error)
    {
        StatusCode = DefaultStatusCode;
    }
}