using Template.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Template.Api.Common;

public static class ResultExtensions
{
    public static IResult ToProblem(this Result result)
    {
        var statusCode = result.Error.Code switch
        {
            "catalog.not_found" => StatusCodes.Status404NotFound,
            "catalog.conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        var problemDetails = new ProblemDetails
        {
            Title = "Request failed",
            Detail = result.Error.Message,
            Status = statusCode
        };
        problemDetails.Extensions["errorCode"] = result.Error.Code;

        return Results.Problem(problemDetails);
    }
}
