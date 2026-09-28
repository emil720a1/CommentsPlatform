using CommentsPlatform.Api.Contracts.Common;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CommentsPlatform.Api.Common.Errors;

public static class ApiErrorMapper
{
    private const string UnexpectedErrorCode = "Server.Unexpected";
    private const string UnexpectedErrorDescription =
        "An unexpected error occurred.";

    public static ObjectResult Map(IReadOnlyList<Error> errors)
    {
        if (errors.Count == 0)
        {
            return CreateUnexpectedErrorResult();
        }

        var statusCode = MapStatusCode(errors[0].Type);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            return CreateUnexpectedErrorResult();
        }

        var apiErrors = errors
            .Select(error => new ApiError(
                error.Code,
                error.Description))
            .ToArray();

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = MapTitle(statusCode),
            Detail = MapDetail(statusCode, errors[0].Description)
        };

        problemDetails.Extensions["errors"] = apiErrors;

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode
        };
    }

    public static ObjectResult Map(ModelStateDictionary modelState)
    {
        var apiErrors = modelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry => entry.Value!.Errors)
            .Select(error => new ApiError(
                "Request.Validation",
                string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? "The request is invalid."
                    : error.ErrorMessage))
            .ToArray();

        if (apiErrors.Length == 0)
        {
            apiErrors =
            [
                new ApiError(
                    "Request.Validation",
                    "The request is invalid.")
            ];
        }

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation error",
            Detail = "One or more validation errors occurred."
        };

        problemDetails.Extensions["errors"] = apiErrors;

        return new ObjectResult(problemDetails)
        {
            StatusCode = StatusCodes.Status400BadRequest
        };
    }

    private static int MapStatusCode(ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Failure => StatusCodes.Status500InternalServerError,
            ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static string MapTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "Validation error",
            StatusCodes.Status401Unauthorized => "Unauthorized",
            StatusCodes.Status403Forbidden => "Forbidden",
            StatusCodes.Status404NotFound => "Resource not found",
            StatusCodes.Status409Conflict => "Conflict",
            _ => "Internal server error"
        };
    }

    private static string MapDetail(
        int statusCode,
        string firstErrorDescription)
    {
        return statusCode == StatusCodes.Status400BadRequest
            ? "One or more validation errors occurred."
            : firstErrorDescription;
    }

    private static ObjectResult CreateUnexpectedErrorResult()
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal server error",
            Detail = UnexpectedErrorDescription
        };

        problemDetails.Extensions["errors"] =
            new[]
            {
                new ApiError(
                    UnexpectedErrorCode,
                    UnexpectedErrorDescription)
            };

        return new ObjectResult(problemDetails)
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };
    }
}
