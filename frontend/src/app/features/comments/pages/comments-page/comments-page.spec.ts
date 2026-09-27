import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
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
      email: 'alice@example.com',
      homePage: 'https://example.com/',
      createdAt: '2026-09-27T10:00:00Z',
      message: 'Hello world',
      attachments: [],
      replies: [],
    },
  ],
  page: 1,
  pageSize: 25,
  totalCount: 60,
  totalPages: 3,
  hasPreviousPage: false,
  hasNextPage: true,
};

describe('CommentsPage', () => {
  let component: CommentsPage;
  let fixture: ComponentFixture<CommentsPage>;
  let router: Router;
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
      providers: [provideRouter([]), { provide: CommentsApiService, useValue: commentsApi }],
    }).compileComponents();

    router = TestBed.inject(Router);
    await router.navigateByUrl('/');

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
    expect(commentsApi.getComments).toHaveBeenCalledWith({
      page: 1,
      pageSize: 25,
      sortBy: 'CreatedAt',
      sortDirection: 'Descending',
    });
    expect(fixture.nativeElement.textContent).toContain('Alice1');
    expect(fixture.nativeElement.textContent).toContain('Hello world');

    const homePageLink = fixture.nativeElement.querySelector(
      'article header a',
    ) as HTMLAnchorElement;
    expect(homePageLink.href).toBe('https://example.com/');
    expect(homePageLink.rel).toContain('noopener');
  });

  it('restores the current page and sorting from the URL with a fixed page size', async () => {
    fixture.destroy();
    await router.navigateByUrl('/?page=3&pageSize=10&sortBy=CreatedAt&sortDirection=Ascending');
    commentsApi.getComments.mockClear();
    fixture = TestBed.createComponent(CommentsPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();

    expect(commentsApi.getComments).toHaveBeenCalledWith({
      page: 3,
      pageSize: 25,
      sortBy: 'CreatedAt',
      sortDirection: 'Ascending',
    });
  });

  it('disables Previous on the first page and enables Next', () => {
    const previousButton = getButton('Попередня');
    const nextButton = getButton('Наступна');

    expect(previousButton.disabled).toBe(true);
    expect(nextButton.disabled).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('Сторінка 1 із 3');
  });

  it('loads the next page and preserves it in the URL', async () => {
    getButton('Наступна').click();
    await fixture.whenStable();

    expect(router.url).toContain('page=2');
    expect(commentsApi.getComments).toHaveBeenLastCalledWith({
      page: 2,
      pageSize: 25,
      sortBy: 'CreatedAt',
      sortDirection: 'Descending',
    });
  });

  it('loads the previous page and disables Next on the last page', async () => {
    commentsApi.getComments.mockReturnValue(
      of({
        ...commentsResponse,
        page: 3,
        hasPreviousPage: true,
        hasNextPage: false,
      }),
    );

    await router.navigate([], { queryParams: { page: 3 } });
    fixture.detectChanges();

    expect(getButton('Попередня').disabled).toBe(false);
    expect(getButton('Наступна').disabled).toBe(true);

    getButton('Попередня').click();
    await fixture.whenStable();

    expect(router.url).toContain('page=2');
    expect(commentsApi.getComments).toHaveBeenLastCalledWith({
      page: 2,
      pageSize: 25,
      sortBy: 'CreatedAt',
      sortDirection: 'Descending',
    });
  });

  it('resets to page one when sorting changes', async () => {
    await router.navigate([], { queryParams: { page: 3 } });
    await fixture.whenStable();
    commentsApi.getComments.mockClear();

    await router.navigate([], {
      queryParams: { sortDirection: 'Ascending' },
      queryParamsHandling: 'merge',
    });
    await fixture.whenStable();

    expect(router.url).toContain('page=1');
    expect(commentsApi.getComments).toHaveBeenCalledWith({
      page: 1,
      pageSize: 25,
      sortBy: 'CreatedAt',
      sortDirection: 'Ascending',
    });
  });

  it('displays the active default sorting options', () => {
    expect(getSelect('comment-sort-field').value).toBe('CreatedAt');
    expect(getSelect('comment-sort-direction').value).toBe('Descending');
  });

  it('sorts by username and resets pagination to page one', async () => {
    await router.navigate([], { queryParams: { page: 3 } });
    await fixture.whenStable();
    commentsApi.getComments.mockClear();

    changeSelect('comment-sort-field', 'UserName');
    await fixture.whenStable();

    expect(router.url).toContain('page=1');
    expect(router.url).toContain('sortBy=UserName');
    expect(commentsApi.getComments).toHaveBeenCalledWith({
      page: 1,
      pageSize: 25,
      sortBy: 'UserName',
      sortDirection: 'Descending',
    });
  });

  it('sorts by email in ascending order and preserves the state in the URL', async () => {
    changeSelect('comment-sort-field', 'Email');
    await fixture.whenStable();
    changeSelect('comment-sort-direction', 'Ascending');
    await fixture.whenStable();

    expect(router.url).toContain('sortBy=Email');
    expect(router.url).toContain('sortDirection=Ascending');
    expect(commentsApi.getComments).toHaveBeenLastCalledWith({
      page: 1,
      pageSize: 25,
      sortBy: 'Email',
      sortDirection: 'Ascending',
    });
  });

  it('restores username sorting after component recreation', async () => {
    fixture.destroy();
    await router.navigateByUrl('/?page=2&sortBy=UserName&sortDirection=Ascending');
    commentsApi.getComments.mockClear();
    fixture = TestBed.createComponent(CommentsPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();

    expect(getSelect('comment-sort-field').value).toBe('UserName');
    expect(getSelect('comment-sort-direction').value).toBe('Ascending');
    expect(commentsApi.getComments).toHaveBeenCalledWith({
      page: 2,
      pageSize: 25,
      sortBy: 'UserName',
      sortDirection: 'Ascending',
    });
  });

  it('falls back to safe defaults for invalid query parameters', async () => {
    await router.navigateByUrl('/?page=-1&pageSize=999&sortBy=Unknown&sortDirection=Unknown');
    await fixture.whenStable();

    expect(commentsApi.getComments).toHaveBeenLastCalledWith({
      page: 1,
      pageSize: 25,
      sortBy: 'CreatedAt',
      sortDirection: 'Descending',
    });
  });

  it('shows a loading state while the API request is pending', async () => {
    const pendingResponse = new Subject<GetCommentsResponse>();
    commentsApi.getComments.mockReturnValueOnce(pendingResponse);

    await router.navigate([], { queryParams: { page: 2 } });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Завантаження коментарів...');

    pendingResponse.next(commentsResponse);
    pendingResponse.complete();
  });

  it('shows an empty state when the API returns no comments', async () => {
    commentsApi.getComments.mockReturnValueOnce(
      of({
        ...commentsResponse,
        items: [],
        totalCount: 0,
        totalPages: 0,
        hasNextPage: false,
      }),
    );

    await router.navigate([], { queryParams: { page: 2 } });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Коментарів поки немає.');
  });

  it('shows an API error and retry control when loading fails', async () => {
    commentsApi.getComments.mockReturnValueOnce(
      throwError(() => ({ status: 500, error: { detail: 'API failure' } })),
    );

    await router.navigate([], { queryParams: { page: 2 } });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('API failure');
    expect(fixture.nativeElement.textContent).toContain('Спробувати ще раз');
  });

  it('renders allowed message markup without inserting unsafe script elements', async () => {
    commentsApi.getComments.mockReturnValueOnce(
      of({
        ...commentsResponse,
        items: [
          {
            ...commentsResponse.items[0],
            message: '<strong>Safe text</strong><script>alert(1)</script>',
          },
        ],
      }),
    );

    await router.navigate([], { queryParams: { page: 2 } });
    fixture.detectChanges();

    const message = fixture.nativeElement.querySelector('.comment-message') as HTMLElement;
    expect(message.querySelector('strong')?.textContent).toBe('Safe text');
    expect(message.querySelector('script')).toBeNull();
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

  it('opens the reply form for a nested comment', async () => {
    commentsApi.getComments.mockReturnValueOnce(
      of({
        ...commentsResponse,
        items: [
          {
            ...commentsResponse.items[0],
            replies: [
              {
                ...commentsResponse.items[0],
                id: 'nested-comment-id',
                userName: 'Nested1',
                email: 'nested@example.com',
                message: 'Nested reply',
                replies: [],
              },
            ],
          },
        ],
      }),
    );

    await router.navigate([], { queryParams: { page: 2 } });
    fixture.detectChanges();

    const replyButtons = fixture.nativeElement.querySelectorAll(
      'app-comment-item button',
    ) as NodeListOf<HTMLButtonElement>;
    replyButtons[1].click();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Відповідь для Nested1');
  });

  function getButton(label: string): HTMLButtonElement {
    const button = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((candidate) => candidate.textContent?.trim() === label);

    if (!button) {
      throw new Error(`Button "${label}" was not found.`);
    }

    return button;
  }

  function getSelect(id: string): HTMLSelectElement {
    return fixture.nativeElement.querySelector(`#${id}`) as HTMLSelectElement;
  }

  function changeSelect(id: string, value: string): void {
    const select = getSelect(id);
    select.value = value;
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();
  }
});
