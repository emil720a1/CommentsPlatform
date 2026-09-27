import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { GetCommentsResponse } from '../../models/comment.models';
import { CommentsApiService } from '../../services/comments-api.service';
import { TurnstileApi } from '../../components/turnstile-widget/turnstile.types';
import { CommentsPage } from './comments-page';

const commentsResponse: GetCommentsResponse = {
  items: [
    {
      id: 'comment-id',
      userName: 'Alice1',
      homePage: null,
      createdAt: '2026-09-27T10:00:00Z',
      message: 'Hello world',
    },
  ],
  page: 1,
  pageSize: 25,
  totalCount: 1,
  totalPages: 1,
  hasPreviousPage: false,
  hasNextPage: false,
};

describe('CommentsPage', () => {
  let component: CommentsPage;
  let fixture: ComponentFixture<CommentsPage>;
  let commentsApi: {
    getComments: ReturnType<typeof vi.fn>;
    createComment: ReturnType<typeof vi.fn>;
    uploadAttachment: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    commentsApi = {
      getComments: vi.fn().mockReturnValue(of(commentsResponse)),
      createComment: vi.fn().mockReturnValue(of({ id: 'new-comment-id' })),
      uploadAttachment: vi.fn().mockReturnValue(of({ attachmentId: 'attachment-id' })),
    };

    const turnstile: TurnstileApi = {
      render: vi.fn((_container, options) => {
        options.callback('captcha-token');
        return 'widget-id';
      }),
      reset: vi.fn(),
      remove: vi.fn(),
    };

    window.turnstile = turnstile;

    await TestBed.configureTestingModule({
      imports: [CommentsPage],
      providers: [{ provide: CommentsApiService, useValue: commentsApi }],
    }).compileComponents();

    fixture = TestBed.createComponent(CommentsPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  afterEach(() => {
    fixture.destroy();
    window.turnstile = undefined;
  });

  it('loads and displays comments', () => {
    expect(commentsApi.getComments).toHaveBeenCalledOnce();
    expect(fixture.nativeElement.textContent).toContain('Alice1');
    expect(fixture.nativeElement.textContent).toContain('Hello world');
  });

  it('opens and cancels the reply form for a comment', () => {
    const replyButton = fixture.nativeElement.querySelector(
      'article > button',
    ) as HTMLButtonElement;

    replyButton.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Відповідь для Alice1');

    const cancelButton = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((button) => button.textContent?.includes('Скасувати відповідь'));

    cancelButton?.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('Відповідь для Alice1');
  });
});
