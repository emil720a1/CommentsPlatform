using FluentValidation;

namespace CommentsPlatform.Application.Features.Comments.Queries.GetComments;

public sealed class GetCommentsQueryValidator : AbstractValidator<GetCommentsQuery>
{
    public GetCommentsQueryValidator()
    {
        RuleFor(q => q.Page)
            .GreaterThan(0)
            .WithErrorCode("Comments.Page.Invalid");

        RuleFor(q => q.PageSize)
            .GreaterThan(0)
            .WithErrorCode("Comments.PageSize.Invalid")
            .LessThanOrEqualTo(100)
            .WithErrorCode("Comments.PageSize.Invalid");

        RuleFor(q => q.SortBy)
            .IsInEnum()
            .WithErrorCode("Comments.SortBy.Invalid");

        RuleFor(q => q.SortDirection)
            .IsInEnum()
            .WithErrorCode("Comments.SortDirection.Invalid");
    }
}
