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
      ...overrides,
    };
  }
});
