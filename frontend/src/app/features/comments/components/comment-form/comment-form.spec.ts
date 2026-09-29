import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { vi } from 'vitest';

import { CommentsApiService } from '../../services/comments-api.service';
import { TurnstileApi } from '../turnstile-widget/turnstile.types';
import { CommentCreatedEvent, CommentForm } from './comment-form';

describe('CommentForm', () => {
  let component: CommentForm;
  let fixture: ComponentFixture<CommentForm>;
  let commentsApi: {
    createComment: ReturnType<typeof vi.fn>;
    uploadAttachment: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    commentsApi = {
      createComment: vi.fn().mockReturnValue(of({ id: 'comment-id' })),
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
      imports: [CommentForm],
      providers: [{ provide: CommentsApiService, useValue: commentsApi }],
    }).compileComponents();

    fixture = TestBed.createComponent(CommentForm);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  afterEach(() => {
    fixture.destroy();
    window.turnstile = undefined;
  });

  it('creates a root comment and resets the form', () => {
    fillRequiredFields();

    submitForm();

    expect(commentsApi.createComment).toHaveBeenCalledWith({
      userName: 'Alice1',
      email: 'alice@example.com',
      homePage: null,
      message: 'Hello world',
      parentCommentId: null,
      captchaToken: 'captcha-token',
    });
    expect(commentsApi.uploadAttachment).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Коментар успішно створено.');
    expect(getInput('input[type="text"]').value).toBe('');
  });

  it('hides the success message after 30 seconds', () => {
    vi.useFakeTimers();

    try {
      fillRequiredFields();
      submitForm();

      expect(fixture.nativeElement.textContent).toContain('Коментар успішно створено.');

      vi.advanceTimersByTime(30_000);
      fixture.detectChanges();

      expect(fixture.nativeElement.textContent).not.toContain('Коментар успішно створено.');
    } finally {
      vi.useRealTimers();
    }
  });

  it('shows validation errors and does not submit an empty form', () => {
    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;

    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(commentsApi.createComment).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Введіть ім\u2019я користувача.');
    expect(fixture.nativeElement.textContent).toContain('Введіть email.');
    expect(fixture.nativeElement.textContent).toContain('Введіть повідомлення.');
  });

  it('shows a pattern error for a username with non-Latin characters', () => {
    setInputValue('input[type="text"]', 'Алiс#');
    const userNameInput = getInput('input[type="text"]');
    userNameInput.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain(
      'Ім\u2019я може містити лише латинські літери та цифри.',
    );
  });

  it('accepts a username with only Latin letters and digits', () => {
    setInputValue('input[type="text"]', 'Alice123');
    const userNameInput = getInput('input[type="text"]');
    userNameInput.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain(
      'Ім\u2019я може містити лише латинські літери та цифри.',
    );
  });

  it('shows an error for an invalid email address', () => {
    setInputValue('input[type="email"]', 'not-an-email');
    const emailInput = getInput('input[type="email"]');
    emailInput.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Введіть коректний email.');
  });

  it('does not show an error for a valid email address', () => {
    setInputValue('input[type="email"]', 'alice@example.com');
    const emailInput = getInput('input[type="email"]');
    emailInput.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('Введіть коректний email.');
  });

  it('allows an empty homepage since it is optional', () => {
    fillRequiredFields();
    submitForm();

    expect(commentsApi.createComment).toHaveBeenCalledWith(
      expect.objectContaining({ homePage: null }),
    );
  });

  it('shows an error for an invalid homepage URL', () => {
    setInputValue('input[type="url"]', 'not-a-url');
    const homePageInput = getInput('input[type="url"]');
    homePageInput.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain(
      'Введіть адресу, яка починається з http:// або https://.',
    );
  });

  it('does not show an error for a valid https homepage', () => {
    setInputValue('input[type="url"]', 'https://example.com');
    const homePageInput = getInput('input[type="url"]');
    homePageInput.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain(
      'Введіть адресу, яка починається з http:// або https://.',
    );
  });

  it('shows the CAPTCHA required error when submitting without CAPTCHA', () => {
    const captchaControl = component['commentForm'].controls.captchaToken;
    captchaControl.setValue('');
    captchaControl.markAsTouched();
    fixture.detectChanges();

    submitForm();

    expect(fixture.nativeElement.textContent).toContain('Підтвердьте CAPTCHA.');
  });

  it('passes the parent comment id when creating a reply', () => {
    fixture.componentRef.setInput('parentCommentId', 'parent-id');
    fixture.componentRef.setInput('replyToUserName', 'Bob2');
    fixture.detectChanges();
    fillRequiredFields();

    submitForm();

    expect(commentsApi.createComment).toHaveBeenCalledWith(
      expect.objectContaining({ parentCommentId: 'parent-id' }),
    );
    expect(fixture.nativeElement.textContent).toContain('Відповідь для Bob2');
  });

  it('uploads a selected attachment after creating the comment', () => {
    const file = new File(['hello'], 'note.txt', { type: 'text/plain' });
    const fileInput = getInput('input[type="file"]');

    Object.defineProperty(fileInput, 'files', {
      configurable: true,
      value: [file],
    });
    fileInput.dispatchEvent(new Event('change'));
    fillRequiredFields();

    submitForm();

    expect(commentsApi.uploadAttachment).toHaveBeenCalledWith('comment-id', file);
  });

  it('shows selected attachment details and allows removing it', () => {
    const file = new File(['hello'], 'note.txt', { type: 'text/plain' });
    const fileInput = getInput('input[type="file"]');

    Object.defineProperty(fileInput, 'files', {
      configurable: true,
      value: [file],
    });
    fileInput.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('note.txt');
    expect(fixture.nativeElement.textContent).toContain('5 Б');

    const removeButton = fixture.nativeElement.querySelector(
      'button[aria-label="Видалити файл note.txt"]',
    ) as HTMLButtonElement;
    removeButton.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('note.txt');
    expect(fileInput.value).toBe('');
  });

  it('disables submit while the request is in progress', () => {
    const createResult = new Subject<{ id: string }>();
    commentsApi.createComment.mockReturnValue(createResult);
    fillRequiredFields();

    submitForm();

    const submitButton = getInput('button[type="submit"]');
    expect(submitButton.disabled).toBe(true);
    expect(submitButton.textContent).toContain('Надсилання...');

    createResult.next({ id: 'comment-id' });
    createResult.complete();
    fixture.detectChanges();

    expect(submitButton.disabled).toBe(false);
  });

  it('shows a safe message for an API validation error', () => {
    commentsApi.createComment.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: {
              errors: [
                {
                  code: 'Comments.Captcha.Invalid',
                  description: 'Technical provider details',
                },
              ],
            },
          }),
      ),
    );
    fillRequiredFields();

    submitForm();

    expect(fixture.nativeElement.textContent).toContain('CAPTCHA недійсна. Підтвердьте її ще раз.');
    expect(fixture.nativeElement.textContent).not.toContain('Technical provider details');
  });

  it('shows a network error message when the API is unreachable', () => {
    commentsApi.createComment.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 0,
            error: new ProgressEvent('error'),
          }),
      ),
    );
    fillRequiredFields();

    submitForm();

    expect(fixture.nativeElement.textContent).toContain(
      'Не вдалося підключитися до API. Перевірте з\u2019єднання та спробуйте ще раз.',
    );
  });

  it('shows a generic error message for an unknown API error', () => {
    commentsApi.createComment.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 500,
            error: { detail: 'Internal server error' },
          }),
      ),
    );
    fillRequiredFields();

    submitForm();

    expect(fixture.nativeElement.textContent).toContain(
      'Не вдалося створити коментар. Спробуйте ще раз.',
    );
  });

  it('emits commentCreated with an attachment error message when upload fails', () => {
    const createdEvents: CommentCreatedEvent[] = [];
    component.commentCreated.subscribe((event) => createdEvents.push(event));

    commentsApi.uploadAttachment.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 413,
            error: {
              errors: [
                {
                  code: 'Attachments.FileSize.TooLarge',
                  description: 'Too large',
                },
              ],
            },
          }),
      ),
    );

    const file = new File(['hello'], 'note.txt', { type: 'text/plain' });
    const fileInput = getInput('input[type="file"]');
    Object.defineProperty(fileInput, 'files', { configurable: true, value: [file] });
    fileInput.dispatchEvent(new Event('change'));
    fillRequiredFields();

    submitForm();

    expect(createdEvents).toHaveLength(1);
    expect(createdEvents[0].attachmentErrorMessage).toContain(
      'Attachment перевищує максимальний розмір 100 KB.',
    );
  });

  it('emits commentCreated with null when submission succeeds without attachment', () => {
    const createdEvents: CommentCreatedEvent[] = [];
    component.commentCreated.subscribe((event) => createdEvents.push(event));
    fillRequiredFields();

    submitForm();

    expect(createdEvents).toHaveLength(1);
    expect(createdEvents[0].attachmentErrorMessage).toBeNull();
  });

  it('resets the form after a successful submission', () => {
    fillRequiredFields();
    submitForm();

    const userNameInput = getInput('input[type="text"]');
    const emailInput = getInput('input[type="email"]');
    const textarea = getTextArea();

    expect(userNameInput.value).toBe('');
    expect(emailInput.value).toBe('');
    expect(textarea.value).toBe('');
  });

  it('emits replyCancelled when the cancel reply button is clicked', () => {
    fixture.componentRef.setInput('parentCommentId', 'parent-id');
    fixture.componentRef.setInput('replyToUserName', 'Bob2');
    fixture.detectChanges();

    const cancelHandler = vi.fn();
    component.replyCancelled.subscribe(cancelHandler);

    const cancelButton = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((button) => button.textContent?.includes('Скасувати відповідь'));
    cancelButton?.click();
    fixture.detectChanges();

    expect(cancelHandler).toHaveBeenCalledOnce();
  });

  it('does not show the cancel button for a root comment form', () => {
    const cancelButton = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((button) => button.textContent?.includes('Скасувати відповідь'));

    expect(cancelButton).toBeUndefined();
  });

  it('does not emit replyCancelled while submitting', () => {
    fixture.componentRef.setInput('parentCommentId', 'parent-id');
    fixture.componentRef.setInput('replyToUserName', 'Bob2');
    fixture.detectChanges();

    const cancelHandler = vi.fn();
    component.replyCancelled.subscribe(cancelHandler);

    const createResult = new Subject<{ id: string }>();
    commentsApi.createComment.mockReturnValue(createResult);
    fillRequiredFields();
    submitForm();

    const cancelButton = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((button) => button.textContent?.includes('Скасувати відповідь'));
    cancelButton?.click();
    fixture.detectChanges();

    expect(cancelHandler).not.toHaveBeenCalled();

    createResult.next({ id: 'comment-id' });
    createResult.complete();
  });

  it('wraps selected message text in an allowed inline tag', () => {
    setInputValue('textarea', 'Hello world');
    const textarea = getTextArea();
    textarea.setSelectionRange(6, 11);

    clickToolbarButton('Жирний текст');

    expect(textarea.value).toBe('Hello <strong>world</strong>');
    expect(textarea.selectionStart).toBe(14);
    expect(textarea.selectionEnd).toBe(19);
  });

  it('inserts a placeholder and selects it when no message text is selected', () => {
    setInputValue('textarea', 'Hello ');
    const textarea = getTextArea();
    textarea.setSelectionRange(6, 6);

    clickToolbarButton('Курсив');

    expect(textarea.value).toBe('Hello <i>текст</i>');
    expect(textarea.value.slice(textarea.selectionStart, textarea.selectionEnd)).toBe('текст');
  });

  it('creates an http link for selected text', () => {
    vi.spyOn(window, 'prompt')
      .mockReturnValueOnce('https://example.com?a=1&b=2')
      .mockReturnValueOnce('Example website');
    setInputValue('textarea', 'Open website');
    const textarea = getTextArea();
    textarea.setSelectionRange(5, 12);

    clickToolbarButton('Посилання');

    expect(textarea.value).toBe(
      'Open <a href="https://example.com?a=1&amp;b=2" title="Example website">website</a>',
    );
  });

  it('rejects an unsafe link scheme without changing the message', () => {
    vi.spyOn(window, 'prompt').mockReturnValue('javascript:alert(1)');
    setInputValue('textarea', 'website');
    const textarea = getTextArea();
    textarea.select();

    clickToolbarButton('Посилання');

    expect(textarea.value).toBe('website');
    expect(fixture.nativeElement.textContent).toContain(
      'Посилання повинно починатися з http:// або https://.',
    );
  });

  it('wraps selected message text in a code tag', () => {
    setInputValue('textarea', 'const value = 1;');
    const textarea = getTextArea();
    textarea.select();

    clickToolbarButton('Код');

    expect(textarea.value).toBe('<code>const value = 1;</code>');
  });

  it('offers only the supported HTML formatting actions', () => {
    const buttons = Array.from(
      fixture.nativeElement.querySelectorAll('.formatting-toolbar button'),
    ) as HTMLButtonElement[];

    expect(buttons.map((button) => button.title)).toEqual([
      'Жирний текст',
      'Курсив',
      'Код',
      'Посилання',
    ]);
  });

  it('escapes HTML special characters in link attributes', () => {
    vi.spyOn(window, 'prompt')
      .mockReturnValueOnce('https://example.com/path?x=1&y=2')
      .mockReturnValueOnce('title with "quotes" & <angle>');
    setInputValue('textarea', 'click here');
    const textarea = getTextArea();
    textarea.select();

    clickToolbarButton('Посилання');

    expect(textarea.value).toContain('&amp;');
    expect(textarea.value).toContain('&quot;');
    expect(textarea.value).toContain('&lt;');
    expect(textarea.value).toContain('&gt;');
  });

  function fillRequiredFields(): void {
    setInputValue('input[type="text"]', 'Alice1');
    setInputValue('input[type="email"]', 'alice@example.com');
    setInputValue('textarea', 'Hello world');
  }

  function setInputValue(selector: string, value: string): void {
    const input = getInput(selector);
    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  function submitForm(): void {
    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  function getInput(selector: string): HTMLInputElement {
    return fixture.nativeElement.querySelector(selector) as HTMLInputElement;
  }

  function getTextArea(): HTMLTextAreaElement {
    return fixture.nativeElement.querySelector('textarea') as HTMLTextAreaElement;
  }

  function clickToolbarButton(title: string): void {
    const button = fixture.nativeElement.querySelector(
      `.formatting-toolbar button[title="${title}"]`,
    ) as HTMLButtonElement;
    button.click();
    fixture.detectChanges();
  }
});
