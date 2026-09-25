namespace CommentsPlatform.Domain.UnitTests;

public sealed class AttachmentTests
{
   private static readonly DateTimeOffset CreatedAt =
      new(2026, 9, 25, 10, 0, 0, TimeSpan.Zero);

   [Fact]
   public void AddAttachment_WithValidFileMetadata_AddsAttachmentToComment()
   {
      var comment = CreateComment();

      var attachment = comment.AddAttachment(
         originalFileName: "document.pdf",
         storageKey: "attachments/document.pdf",
         contentType: "application/pdf",
         fileSizeBytes: 1024,
         width: null,
         height: null
      );

      Assert.NotEqual(Guid.Empty, attachment.Id);
      Assert.Equal(comment.Id, attachment.CommentId);
      Assert.Equal(1024L, attachment.FileSizeBytes);
      Assert.Null(attachment.Width);
      Assert.Null(attachment.Height);
      Assert.Equal(TimeSpan.Zero, attachment.CreatedAt.Offset);
      Assert.Equal("document.pdf", attachment.OriginalFileName);
      Assert.Equal("attachments/document.pdf", attachment.StorageKey);
      Assert.Equal("application/pdf", attachment.ContentType);

      var storedAttachment = Assert.Single(comment.Attachments);

      Assert.Same(attachment, storedAttachment);
   }

   [Fact]
   public void AddAttachment_WithValidImageMetadata_PreservesDimensions()
   {
      var comment = CreateComment();

      var attachment = comment.AddAttachment(
         originalFileName: "photo.png",
         storageKey: "attachments/photo.png",
         contentType: "image/png",
         fileSizeBytes: 2048,
         width: 1920,
         height: 1080);

      Assert.Equal(1920, attachment.Width);
      Assert.Equal(1080, attachment.Height);

      var storedAttachment = Assert.Single(comment.Attachments);

      Assert.Same(attachment, storedAttachment);
   }

   [Theory]
   [InlineData(null)]
   [InlineData("")]
   [InlineData(" ")]
   public void AddAttachment_WithMissingOriginalFileName_ThrowsArgumentException(
      string? invalidOriginalFileName)
   {
      var comment = CreateComment();

      var exception = Assert.Throws<ArgumentException>(() =>
         comment.AddAttachment(
            originalFileName: invalidOriginalFileName!,
            storageKey: "attachments/photo.png",
            contentType: "image/png",
            fileSizeBytes: 2048,
            width: 1920,
            height: 1080));

      Assert.Equal("originalFileName", exception.ParamName);
      Assert.Empty(comment.Attachments);
   }

   [Theory]
   [InlineData(null)]
   [InlineData("")]
   [InlineData(" ")]
   public void AddAttachment_WithMissingStorageKey_ThrowsArgumentException(
      string? invalidStorageKey)
   {
      var comment = CreateComment();

      var exception = Assert.Throws<ArgumentException>(() =>
         comment.AddAttachment(
            originalFileName: "photo.png",
            storageKey: invalidStorageKey!,
            contentType: "image/png",
            fileSizeBytes: 2048,
            width: 1920,
            height: 1080));

      Assert.Equal("storageKey", exception.ParamName);
      Assert.Empty(comment.Attachments);
   }

   [Theory]
   [InlineData(null)]
   [InlineData("")]
   [InlineData(" ")]
   public void AddAttachment_WithMissingContentType_ThrowsArgumentException(
      string? invalidContentType)
   {
      var comment = CreateComment();

      var exception = Assert.Throws<ArgumentException>(() =>
         comment.AddAttachment(
            originalFileName: "photo.png",
            storageKey: "attachments/photo.png",
            contentType: invalidContentType!,
            fileSizeBytes: 2048,
            width: 1920,
            height: 1080));

      Assert.Equal("contentType", exception.ParamName);
      Assert.Empty(comment.Attachments);
   }

   [Theory]
   [InlineData(0L)]
   [InlineData(-1L)]
   public void AddAttachment_WithNonPositiveFileSize_ThrowsArgumentOutOfRangeException(
      long invalidFileSizeBytes)
   {
      var comment = CreateComment();

      var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
         comment.AddAttachment(
            originalFileName: "photo.png",
            storageKey: "attachments/photo.png",
            contentType: "image/png",
            fileSizeBytes: invalidFileSizeBytes,
            width: 1920,
            height: 1080));

      Assert.Equal("fileSizeBytes", exception.ParamName);
      Assert.Empty(comment.Attachments);
   }

   [Theory]
   [InlineData(1920, null)]
   [InlineData(null, 1080)]
   public void AddAttachment_WithOnlyOneImageDimension_ThrowsArgumentException(
      int? width,
      int? height)
   {
      var comment = CreateComment();

      Assert.Throws<ArgumentException>(() =>
         comment.AddAttachment(
            originalFileName: "photo.png",
            storageKey: "attachments/photo.png",
            contentType: "image/png",
            fileSizeBytes: 2048,
            width: width,
            height: height));

      Assert.Empty(comment.Attachments);
   }

   [Theory]
   [InlineData(0, 1080, "width")]
   [InlineData(-1, 1080, "width")]
   [InlineData(1920, 0, "height")]
   [InlineData(1920, -1, "height")]
   public void AddAttachment_WithNonPositiveDimension_ThrowsArgumentOutOfRangeException(
      int width,
      int height,
      string expectedParamName)
   {
      var comment = CreateComment();

      var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
         comment.AddAttachment(
            originalFileName: "photo.png",
            storageKey: "attachments/photo.png",
            contentType: "image/png",
            fileSizeBytes: 2048,
            width: width,
            height: height));

      Assert.Equal(expectedParamName, exception.ParamName);
      Assert.Empty(comment.Attachments);

   }

   [Fact]
   public void Attachments_WhenModifiedExternally_ThrowsNotSupportedException()
   {
      var comment = CreateComment();

      var attachment = comment.AddAttachment(
         originalFileName: "document.pdf",
         storageKey: "attachments/document.pdf",
         contentType: "application/pdf",
         fileSizeBytes: 1024,
         width: null,
         height: null);

      var attachments =
         Assert.IsAssignableFrom<ICollection<Attachment>>(comment.Attachments);

      Assert.True(attachments.IsReadOnly);
      Assert.Throws<NotSupportedException>(() =>
         attachments.Add(attachment));

      var storedAttachment = Assert.Single(comment.Attachments);

      Assert.Same(attachment, storedAttachment);
   }

   private static Comment CreateComment()
   {
      return Comment.Create(
         userName: "username",
         email: "user@example.com",
         homePage: null,
         message: "message",
         parentCommentId: null,
         createdAt: CreatedAt);
   }
}
