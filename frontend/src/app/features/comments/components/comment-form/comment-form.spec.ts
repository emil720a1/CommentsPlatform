import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { vi } from 'vitest';

import { CommentsApiService } from '../../services/comments-api.service';
import { TurnstileApi } from '../turnstile-widget/turnstile.types';
import { CommentForm } from './comment-form';

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

  it('shows validation errors and does not submit an empty form', () => {
    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;

    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(commentsApi.createComment).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Введіть ім’я користувача.');
    expect(fixture.nativeElement.textContent).toContain('Введіть email.');
    expect(fixture.nativeElement.textContent).toContain('Введіть повідомлення.');
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
      'Посилання',
      'Код',
    ]);
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
