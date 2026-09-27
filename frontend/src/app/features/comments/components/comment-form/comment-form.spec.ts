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
});
