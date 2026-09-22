using System.Text.Json;
using CommentsPlatform.Application.Features.Comments.Queries.GetComments;
using FluentValidation.TestHelper;
using Xunit;

namespace CommentsPlatform.Application.UnitTests.Features.Comments.Queries.GetComments;

public sealed class GetCommentsQueryValidatorTests
{
    private readonly GetCommentsQueryValidator _validator;

    public GetCommentsQueryValidatorTests()
    {
        _validator = new GetCommentsQueryValidator();
    }

    [Fact]
    public void Validate_WithValidQuery_ShouldNotHaveAnyErrors()
    {
        var query = new GetCommentsQuery(
            Page: 1,
            PageSize: 50,
            SortBy: CommentSortBy.CreatedAt,
            SortDirection: SortDirection.Descending);

        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Validate_WithInvalidPage_ShouldHaveValidationErrors(int page)
    {
        var query = new GetCommentsQuery(
            Page: page,
            PageSize: 50,
            SortBy: CommentSortBy.CreatedAt,
            SortDirection: SortDirection.Descending);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Validate_WithInvalidPageSize_ShouldHaveValidationErrors(int pageSize)
    {
        var query = new GetCommentsQuery(
            Page: 1,
            PageSize: pageSize,
            SortBy: CommentSortBy.CreatedAt,
            SortDirection: SortDirection.Descending);

        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(q => q.PageSize);
    }

    [Fact]
    public void Validate_WithPageSizeGreaterThan100_ShouldHaveValidationErrors()
    {
        var query = new GetCommentsQuery(
            Page: 1,
            PageSize: 101,
            SortBy: CommentSortBy.CreatedAt,
            SortDirection: SortDirection.Descending);

        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(q => q.PageSize);
    }

    [Fact]
    public void Validate_WithInvalidSortBy_ShouldHaveValidationErrors()
    {
        var query = new GetCommentsQuery(1, 50, (CommentSortBy)999, SortDirection.Descending);

        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(q => q.SortBy);
    }

    [Fact]
    public void Validate_WithInvalidSortDirection_ShouldHaveValidationErrors()
    {
        var query = new GetCommentsQuery(1, 50, CommentSortBy.CreatedAt, (SortDirection)999);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.SortDirection);
    }

    [Fact]
    public void Validate_WithAllValidParams_ShouldNotHaveErrors()
    {
        var query = new GetCommentsQuery(
            Page: 1,
            PageSize: 10,
            SortBy: CommentSortBy.CreatedAt,
            SortDirection: SortDirection.Descending);

        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }
}