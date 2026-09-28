/*
   Local/demo data only. Run after migrations against the CommentsPlatform DB.
   The script is idempotent and does not create attachment rows because the
   corresponding files are not present in the demo volume.
*/

USE [CommentsPlatform];
GO

DECLARE @alice uniqueidentifier = '11111111-1111-1111-1111-111111111111';
DECLARE @bob uniqueidentifier = '22222222-2222-2222-2222-222222222222';
DECLARE @charlie uniqueidentifier = '33333333-3333-3333-3333-333333333333';
DECLARE @reply uniqueidentifier = '44444444-4444-4444-4444-444444444444';

IF NOT EXISTS (SELECT 1 FROM Comments WHERE Id = @alice)
    INSERT INTO Comments (Id, UserName, Email, HomePage, Message, CreatedAt, ParentCommentId)
    VALUES (@alice, 'Alice', 'alice@example.com', 'https://alice.example.com',
            N'Welcome to the demo thread. The comments list is ready for testing.',
            '2026-09-27T09:00:00+00:00', NULL);

IF NOT EXISTS (SELECT 1 FROM Comments WHERE Id = @bob)
    INSERT INTO Comments (Id, UserName, Email, HomePage, Message, CreatedAt, ParentCommentId)
    VALUES (@bob, 'Bob', 'bob@example.com', NULL,
            N'Try sorting these comments by username, email, and creation date.',
            '2026-09-27T09:15:00+00:00', NULL);

IF NOT EXISTS (SELECT 1 FROM Comments WHERE Id = @charlie)
    INSERT INTO Comments (Id, UserName, Email, HomePage, Message, CreatedAt, ParentCommentId)
    VALUES (@charlie, 'Charlie', 'charlie@example.com', 'https://charlie.example.com',
            N'The pagination and reply flow are also covered by the demo data.',
            '2026-09-27T09:30:00+00:00', NULL);

IF NOT EXISTS (SELECT 1 FROM Comments WHERE Id = @reply)
    INSERT INTO Comments (Id, UserName, Email, HomePage, Message, CreatedAt, ParentCommentId)
    VALUES (@reply, 'DemoReply', 'reply@example.com', NULL,
            N'This is a nested reply attached to Alice''s comment.',
            '2026-09-27T09:45:00+00:00', @alice);

SELECT Id, UserName, Email, CreatedAt, ParentCommentId
FROM Comments
WHERE Id IN (@alice, @bob, @charlie, @reply)
ORDER BY CreatedAt;
