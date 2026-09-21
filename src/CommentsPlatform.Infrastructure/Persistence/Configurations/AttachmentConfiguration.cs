using CommentsPlatform.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommentsPlatform.Infrastructure.Persistence.Configurations;

public sealed class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments");

        builder.HasKey(attachment => attachment.Id);

        builder.Property(attachment => attachment.Id)
            .ValueGeneratedNever();

        builder.Property(attachment => attachment.CommentId)
            .IsRequired();

        builder.Property(attachment => attachment.StorageKey)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(attachment => attachment.OriginalFileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(attachment => attachment.ContentType)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(attachment => attachment.FileSizeBytes)
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(attachment => attachment.Width)
            .HasColumnType("int")
            .IsRequired(false);

        builder.Property(attachment => attachment.Height)
            .HasColumnType("int")
            .IsRequired(false);

        builder.Property(attachment => attachment.CreatedAt)
            .HasColumnType("datetimeoffset")
            .IsRequired();
    }
}
