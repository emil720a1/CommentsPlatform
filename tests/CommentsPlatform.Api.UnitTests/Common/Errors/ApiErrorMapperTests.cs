using CommentsPlatform.Api.Common.Errors;
using CommentsPlatform.Api.Contracts.Common;
using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CommentsPlatform.Api.UnitTests.Common.Errors;

public sealed class ApiErrorMapperTests
{
    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest, "Validation error")]
    [InlineData(ErrorType.Unauthorized, StatusCodes.Status401Unauthorized, "Unauthorized")]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden, "Forbidden")]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound, "Resource not found")]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict, "Conflict")]
    public void Map_WithClientError_ReturnsExpectedProblemDetails(
        ErrorType errorType,
        int expectedStatusCode,
        string expectedTitle)
    {
        var error = CreateError(errorType, "Test.Code", "Test description.");

        var result = ApiErrorMapper.Map(new[] { error });

        Assert.Equal(expectedStatusCode, result.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(result.Value);

        Assert.Equal(expectedStatusCode, problemDetails.Status);
        Assert.Equal(expectedTitle, problemDetails.Title);

        var expectedDetail = errorType == ErrorType.Validation
            ? "One or more validation errors occurred."
            : error.Description;

        Assert.Equal(expectedDetail, problemDetails.Detail);

        var apiErrors = Assert.IsType<ApiError[]>(
            problemDetails.Extensions["errors"]);
        var apiError = Assert.Single(apiErrors);

        Assert.Equal(error.Code, apiError.Code);
        Assert.Equal(error.Description, apiError.Description);
    }

    [Fact]
    public void Map_WithMultipleValidationErrors_PreservesAllErrors()
    {
        var errors = new[]
        {
            Error.Validation("First.Code", "First description."),
            Error.Validation("Second.Code", "Second description.")
        };

        var result = ApiErrorMapper.Map(errors);

        var problemDetails = Assert.IsType<ProblemDetails>(result.Value);
        var apiErrors = Assert.IsType<ApiError[]>(
            problemDetails.Extensions["errors"]);

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Collection(
            apiErrors,
            firstError =>
            {
                Assert.Equal("First.Code", firstError.Code);
                Assert.Equal("First description.", firstError.Description);
            },
            secondError =>
            {
                Assert.Equal("Second.Code", secondError.Code);
                Assert.Equal("Second description.", secondError.Description);
            });
    }

    [Theory]
    [InlineData(ErrorType.Failure)]
    [InlineData(ErrorType.Unexpected)]
    public void Map_WithServerError_ReturnsSafeGenericProblemDetails(
        ErrorType errorType)
    {
        var error = CreateError(
            errorType,
            "Internal.SecretCode",
            "Sensitive internal description.");

        var result = ApiErrorMapper.Map(new[] { error });

        AssertUnexpectedErrorResult(result);
    }

    [Fact]
    public void Map_WithEmptyErrors_ReturnsSafeGenericProblemDetails()
    {
        var result = ApiErrorMapper.Map(Array.Empty<Error>());

        AssertUnexpectedErrorResult(result);
    }

    [Fact]
    public void Map_WithInvalidModelState_ReturnsValidationProblemDetails()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError(
            "SortBy",
            "The value '999' is invalid.");

        var result = ApiErrorMapper.Map(modelState);

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(result.Value);

        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
        Assert.Equal("Validation error", problemDetails.Title);
        Assert.Equal(
            "One or more validation errors occurred.",
            problemDetails.Detail);

        var apiErrors = Assert.IsType<ApiError[]>(
            problemDetails.Extensions["errors"]);
        var apiError = Assert.Single(apiErrors);

        Assert.Equal("Request.Validation", apiError.Code);
        Assert.Equal("The value '999' is invalid.", apiError.Description);
    }

    private static Error CreateError(
        ErrorType errorType,
        string code,
        string description)
    {
        return errorType switch
        {
            ErrorType.Validation => Error.Validation(code, description),
            ErrorType.Unauthorized => Error.Unauthorized(code, description),
            ErrorType.Forbidden => Error.Forbidden(code, description),
            ErrorType.NotFound => Error.NotFound(code, description),
            ErrorType.Conflict => Error.Conflict(code, description),
            ErrorType.Failure => Error.Failure(code, description),
            ErrorType.Unexpected => Error.Unexpected(code, description),
            _ => throw new ArgumentOutOfRangeException(nameof(errorType))
        };
    }

    private static void AssertUnexpectedErrorResult(ObjectResult result)
    {
        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            result.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(result.Value);

        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            problemDetails.Status);
        Assert.Equal("Internal server error", problemDetails.Title);
        Assert.Equal(
            "An unexpected error occurred.",
            problemDetails.Detail);

        var apiErrors = Assert.IsType<ApiError[]>(
            problemDetails.Extensions["errors"]);
        var apiError = Assert.Single(apiErrors);

        Assert.Equal("Server.Unexpected", apiError.Code);
        Assert.Equal(
            "An unexpected error occurred.",
            apiError.Description);
    }
}
