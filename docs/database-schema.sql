-- CommentsPlatform database schema
-- Target: Microsoft SQL Server
-- The application normally creates this schema through EF Core migrations.
-- This script is provided as a design/reference artifact for review.

CREATE TABLE dbo.Comments
(
    Id uniqueidentifier NOT NULL,
    UserName nvarchar(100) NOT NULL,
    Email nvarchar(320) NOT NULL,
    Message nvarchar(max) NOT NULL,
    HomePage nvarchar(2048) NULL,
    CreatedAt datetimeoffset NOT NULL,
    ParentCommentId uniqueidentifier NULL,

    CONSTRAINT PK_Comments PRIMARY KEY (Id),
    CONSTRAINT FK_Comments_Comments_ParentCommentId
        FOREIGN KEY (ParentCommentId)
        REFERENCES dbo.Comments (Id)
        ON DELETE NO ACTION
);

CREATE TABLE dbo.Attachments
(
    Id uniqueidentifier NOT NULL,
    CommentId uniqueidentifier NOT NULL,
    StorageKey nvarchar(512) NOT NULL,
    OriginalFileName nvarchar(255) NOT NULL,
    ContentType nvarchar(255) NOT NULL,
    FileSizeBytes bigint NOT NULL,
    Width int NULL,
    Height int NULL,
    CreatedAt datetimeoffset NOT NULL,

    CONSTRAINT PK_Attachments PRIMARY KEY (Id),
    CONSTRAINT FK_Attachments_Comments_CommentId
        FOREIGN KEY (CommentId)
        REFERENCES dbo.Comments (Id)
        ON DELETE CASCADE
);

CREATE INDEX IX_Comments_ParentCommentId
    ON dbo.Comments (ParentCommentId);

CREATE INDEX IX_Attachments_CommentId
    ON dbo.Attachments (CommentId);
