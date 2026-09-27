import { ComponentFixture, TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import { CommentResponse } from '../../models/comment.models';
import { CommentsList } from './comments-list';

describe('CommentsList', () => {
  let fixture: ComponentFixture<CommentsList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommentsList],
    }).compileComponents();

    fixture = TestBed.createComponent(CommentsList);
  });

  it('renders comment details, safe links and attachments', () => {
    fixture.componentRef.setInput('comments', [createComment()]);
    fixture.detectChanges();

    const homePage = fixture.nativeElement.querySelector(
      'header a[href="https://example.com/"]',
    ) as HTMLAnchorElement;
    const email = fixture.nativeElement.querySelector(
      'header a[href="mailto:alice@example.com"]',
    ) as HTMLAnchorElement;

    expect(fixture.nativeElement.textContent).toContain('Alice1');
    expect(fixture.nativeElement.textContent).toContain('Safe message');
    expect(homePage.rel).toContain('noopener');
    expect(email.textContent).toContain('alice@example.com');
    expect(fixture.nativeElement.textContent).toContain('notes.txt');
    expect(
      (
        fixture.nativeElement.querySelector('app-comment-avatar .avatar') as HTMLElement
      ).textContent?.trim(),
    ).toBe('A');
  });

  it('renders visible author, email and date metadata without bullet elements', () => {
    fixture.componentRef.setInput('comments', [createComment()]);
    fixture.detectChanges();

    const metadata = fixture.nativeElement.querySelector('.comment-meta') as HTMLElement;
    const author = metadata.querySelector('.comment-author') as HTMLElement;
    const email = metadata.querySelector('.comment-email') as HTMLAnchorElement;
    const date = metadata.querySelector('time') as HTMLTimeElement;

    expect(author.textContent?.trim()).toBe('Alice1');
    expect(email.textContent?.trim()).toBe('alice@example.com');
    expect(date.dateTime).toBe('2026-09-27T10:00:00Z');
    expect(metadata.textContent).not.toContain('·');
  });

  it('sanitizes unsafe message markup', () => {
    fixture.componentRef.setInput('comments', [
      createComment({
        message: '<strong>Safe</strong><script>alert(1)</script>',
      }),
    ]);
    fixture.detectChanges();

    const message = fixture.nativeElement.querySelector('.comment-message') as HTMLElement;
    expect(message.querySelector('strong')?.textContent).toBe('Safe');
    expect(message.querySelector('script')).toBeNull();
  });

  it('emits the selected comment when reply is requested', () => {
    const replyHandler = vi.fn();
    fixture.componentRef.setInput('comments', [createComment()]);
    fixture.componentInstance.replyRequested.subscribe(replyHandler);
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('button') as HTMLButtonElement).click();

    expect(replyHandler).toHaveBeenCalledWith({
      id: 'comment-id',
      userName: 'Alice1',
    });
  });

  it('renders multiple reply levels and emits the nested reply target', () => {
    const replyHandler = vi.fn();
    const nestedReply = createComment({
      id: 'nested-reply-id',
      userName: 'Nested1',
      email: 'nested@example.com',
      message: 'Nested reply',
    });
    const reply = createComment({
      id: 'reply-id',
      userName: 'Reply1',
      email: 'reply@example.com',
      message: 'First reply',
      replies: [nestedReply],
    });
    fixture.componentRef.setInput('comments', [createComment({ replies: [reply] })]);
    fixture.componentInstance.replyRequested.subscribe(replyHandler);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('.replies')).toHaveLength(2);
    expect(fixture.nativeElement.textContent).toContain('First reply');
    expect(fixture.nativeElement.textContent).toContain('Nested reply');
    expect(
      Array.from(
        fixture.nativeElement.querySelectorAll(
          'app-comment-avatar .avatar',
        ) as NodeListOf<HTMLElement>,
      ).map((avatar) => avatar.textContent?.trim()),
    ).toEqual(['A', 'R', 'N']);

    const buttons = fixture.nativeElement.querySelectorAll(
      'button',
    ) as NodeListOf<HTMLButtonElement>;
    buttons[2].click();

    expect(replyHandler).toHaveBeenCalledWith({
      id: 'nested-reply-id',
      userName: 'Nested1',
    });
  });

  function createComment(overrides: Partial<CommentResponse> = {}): CommentResponse {
    return {
      id: 'comment-id',
      userName: 'Alice1',
      email: 'alice@example.com',
      homePage: 'https://example.com/',
      createdAt: '2026-09-27T10:00:00Z',
      message: 'Safe message',
      attachments: [
        {
          id: 'attachment-id',
          originalFileName: 'notes.txt',
          contentType: 'text/plain',
          fileSizeBytes: 10,
          width: null,
          height: null,
          createdAt: '2026-09-27T10:00:00Z',
          downloadUrl: '/api/comments/comment-id/attachments/attachment-id',
        },
      ],
      replies: [],
      ...overrides,
    };
  }
});
