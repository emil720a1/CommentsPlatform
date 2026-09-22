using FluentValidation;

namespace CommentsPlatform.Application.Features.Comments.Queries.GetComments;

public sealed class GetCommentsQueryValidator : AbstractValidator<GetCommentsQuery>
{
    public GetCommentsQueryValidator()
    {
        RuleFor(q => q.Page)
        .GreaterThan(0);


        RuleFor(q => q.PageSize)
        .GreaterThan(0)
        .LessThanOrEqualTo(100);

        RuleFor(q => q.SortBy)
            .IsInEnum();

        RuleFor(q => q.SortDirection)
            .IsInEnum();
    }
}
