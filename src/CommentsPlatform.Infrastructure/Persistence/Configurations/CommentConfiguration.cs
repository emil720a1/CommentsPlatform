using CommentsPlatform.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommentsPlatform.Infrastructure.Persistence.Configurations;

public sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");

        builder.HasKey(comment => comment.Id);

        builder.Property(comment => comment.Id)
            .ValueGeneratedNever();

        builder.Property(comment => comment.UserName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(comment => comment.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(comment => comment.HomePage)
            .HasMaxLength(2048)
            .IsRequired(false);

        builder.Property(comment => comment.Message)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(comment => comment.ParentCommentId)
            .IsRequired(false);

        builder.Property(comment => comment.CreatedAt)
            .HasColumnType("datetimeoffset")
            .IsRequired();

        builder
            .HasOne<Comment>()
            .WithMany()
            .HasForeignKey(comment => comment.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(comment => comment.Attachments)
            .WithOne()
            .HasForeignKey(attachment => attachment.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(comment => comment.Attachments)
            .HasField("_attachments")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
