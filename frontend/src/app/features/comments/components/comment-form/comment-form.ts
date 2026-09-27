import { HttpErrorResponse } from '@angular/common/http';
import { Component, ElementRef, inject, input, output, signal, ViewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { catchError, finalize, map, Observable, of, switchMap } from 'rxjs';

import { ApiProblemDetails, CreateCommentRequest } from '../../models/comment.models';
import { CommentsApiService } from '../../services/comments-api.service';
import {
  notBlankValidator,
  optionalHttpUrlValidator,
} from '../../validators/comment-form.validators';
import { TurnstileWidget } from '../turnstile-widget/turnstile-widget';

const MAX_ATTACHMENT_SIZE_BYTES = 5 * 1024 * 1024;

const ALLOWED_ATTACHMENT_TYPES = new Set(['image/jpeg', 'image/png', 'image/gif', 'text/plain']);

const API_ERROR_MESSAGES: Record<string, string> = {
  'Comments.UserName.Required': 'Введіть ім’я користувача.',
  'Comments.UserName.InvalidCharacters': 'Ім’я може містити лише латинські літери та цифри.',
  'Comments.Email.Required': 'Введіть email.',
  'Comments.Email.InvalidFormat': 'Введіть коректний email.',
  'Comments.HomePage.InvalidFormat': 'Домашня сторінка повинна починатися з http:// або https://.',
  'Comments.Message.Required': 'Введіть повідомлення.',
  'Comments.ParentCommentId.Empty': 'Не вдалося визначити коментар для відповіді.',
  'Comments.ParentNotFound': 'Коментар, на який ви відповідаєте, більше не існує.',
  'Comments.Captcha.Required': 'Підтвердьте CAPTCHA.',
  'Comments.Captcha.Invalid': 'CAPTCHA недійсна. Підтвердьте її ще раз.',
  'Comments.DomainValidation': 'Перевірте введені дані та спробуйте ще раз.',
  'Attachments.File.Required': 'Виберіть attachment.',
  'Attachments.Content.Required': 'Вибраний attachment порожній.',
  'Attachments.FileName.Required': 'Attachment повинен мати назву.',
  'Attachments.FileName.TooLong': 'Назва attachment занадто довга.',
  'Attachments.FileName.Invalid': 'Назва attachment недійсна.',
  'Attachments.ContentType.Required': 'Не вдалося визначити тип attachment.',
  'Attachments.ContentType.Unsupported': 'Тип attachment не підтримується.',
  'Attachments.FileSize.Empty': 'Attachment не може бути порожнім.',
  'Attachments.FileSize.TooLarge': 'Attachment перевищує максимальний розмір 5 MiB.',
  'Attachments.CommentNotFound': 'Коментар створено, але його не знайдено для attachment.',
};

type SubmissionResult =
  { kind: 'success' } | { kind: 'attachment-error'; error: HttpErrorResponse };

export interface CommentCreatedEvent {
  attachmentErrorMessage: string | null;
}

@Component({
  selector: 'app-comment-form',
  imports: [ReactiveFormsModule, TurnstileWidget],
  templateUrl: './comment-form.html',
  styleUrl: './comment-form.scss',
})
export class CommentForm {
  private static nextInstanceId = 0;

  private readonly formBuilder = inject(FormBuilder);

  private readonly commentsApi = inject(CommentsApiService);

  @ViewChild(TurnstileWidget)
  private turnstileWidget?: TurnstileWidget;

  @ViewChild('attachmentInput')
  private attachmentInput?: ElementRef<HTMLInputElement>;

  @ViewChild('messageInput')
  private messageInput?: ElementRef<HTMLTextAreaElement>;

  readonly parentCommentId = input<string | null>(null);

  readonly replyToUserName = input<string | null>(null);

  readonly commentCreated = output<CommentCreatedEvent>();

  readonly replyCancelled = output<void>();

  protected readonly fieldIdPrefix = `comment-form-${CommentForm.nextInstanceId++}`;

  protected readonly selectedFile = signal<File | null>(null);

  protected readonly attachmentError = signal<string | null>(null);

  protected readonly isSubmitting = signal(false);

  protected readonly successMessage = signal<string | null>(null);

  protected readonly apiErrorMessage = signal<string | null>(null);

  protected readonly formattingError = signal<string | null>(null);

  protected readonly commentForm = this.formBuilder.nonNullable.group({
    userName: ['', [Validators.required, Validators.pattern(/^[A-Za-z0-9]+$/)]],
    email: ['', [Validators.required, Validators.email]],
    homePage: ['', optionalHttpUrlValidator],
    message: ['', [Validators.required, notBlankValidator]],
    captchaToken: ['', Validators.required],
  });

  protected onCaptchaTokenChange(token: string | null): void {
    const control = this.commentForm.controls.captchaToken;

    control.setValue(token ?? '');

    if (token === null) {
      control.markAsTouched();
    }

    control.updateValueAndValidity();
  }

  protected applyInlineFormat(tag: 'strong' | 'i' | 'code'): void {
    this.wrapSelection(`<${tag}>`, `</${tag}>`, 'текст');
  }

  protected applyLinkFormat(): void {
    const selection = this.getSelection();

    if (selection === null) {
      return;
    }

    const enteredUrl = window.prompt('Введіть адресу посилання (http:// або https://):');

    if (enteredUrl === null) {
      return;
    }

    const url = enteredUrl.trim();

    if (!this.isSafeHttpUrl(url)) {
      this.formattingError.set('Посилання повинно починатися з http:// або https://.');
      this.messageInput?.nativeElement.focus();
      return;
    }

    const enteredTitle = window.prompt('Введіть опис посилання:', selection.text);

    if (enteredTitle === null) {
      this.messageInput?.nativeElement.focus();
      return;
    }

    const text = selection.text || 'посилання';
    const markup = `<a href="${this.escapeAttribute(url)}" title="${this.escapeAttribute(enteredTitle.trim())}">${text}</a>`;
    const contentStart = selection.start + markup.indexOf(text);

    this.replaceSelection(selection, markup, contentStart, contentStart + text.length);
  }

  protected onFileSelected(event: Event): void {
    const inputElement = event.target as HTMLInputElement;
    const file = inputElement.files?.[0] ?? null;

    this.selectedFile.set(null);
    this.attachmentError.set(null);

    if (file === null) {
      return;
    }

    if (file.size === 0) {
      this.rejectFile(inputElement, 'Файл не може бути порожнім.');
      return;
    }

    if (file.size > MAX_ATTACHMENT_SIZE_BYTES) {
      this.rejectFile(inputElement, 'Розмір файлу не може перевищувати 5 MiB.');
      return;
    }

    if (!ALLOWED_ATTACHMENT_TYPES.has(file.type)) {
      this.rejectFile(inputElement, 'Дозволені лише файли JPEG, PNG, GIF або TXT.');
      return;
    }

    if (file.name.length > 255) {
      this.rejectFile(inputElement, 'Назва файлу занадто довга.');
      return;
    }

    this.selectedFile.set(file);
  }

  protected removeSelectedFile(): void {
    this.selectedFile.set(null);
    this.attachmentError.set(null);

    if (this.attachmentInput !== undefined) {
      this.attachmentInput.nativeElement.value = '';
    }
  }

  protected formatFileSize(bytes: number): string {
    if (bytes < 1024) {
      return `${bytes} Б`;
    }

    const kilobytes = bytes / 1024;

    if (kilobytes < 1024) {
      return `${kilobytes.toFixed(kilobytes < 10 ? 1 : 0)} КБ`;
    }

    return `${(kilobytes / 1024).toFixed(1)} МБ`;
  }

  protected onSubmit(): void {
    if (this.isSubmitting()) {
      return;
    }

    if (this.commentForm.invalid || this.attachmentError() !== null) {
      this.commentForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.successMessage.set(null);
    this.apiErrorMessage.set(null);

    const request = this.createRequest();
    const selectedFile = this.selectedFile();

    this.commentsApi
      .createComment(request)
      .pipe(
        switchMap(({ id }) => this.uploadAttachmentIfSelected(id, selectedFile)),
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: (result) => {
          if (result.kind === 'attachment-error') {
            const attachmentErrorMessage = this.getAttachmentErrorMessage(result.error);

            this.resetAfterCreatedComment();
            this.commentCreated.emit({ attachmentErrorMessage });
            return;
          }

          this.resetAfterCreatedComment();
          this.successMessage.set('Коментар успішно створено.');
          this.commentCreated.emit({ attachmentErrorMessage: null });
        },
        error: (error: HttpErrorResponse) => {
          this.apiErrorMessage.set(this.getCreateErrorMessage(error));
          this.resetCaptcha();
        },
      });
  }

  protected cancelReply(): void {
    if (!this.isSubmitting()) {
      this.replyCancelled.emit();
    }
  }

  private createRequest(): CreateCommentRequest {
    const value = this.commentForm.getRawValue();
    const homePage = value.homePage.trim();

    return {
      userName: value.userName.trim(),
      email: value.email.trim(),
      homePage: homePage.length === 0 ? null : homePage,
      message: value.message,
      parentCommentId: this.parentCommentId(),
      captchaToken: value.captchaToken,
    };
  }

  private uploadAttachmentIfSelected(
    commentId: string,
    file: File | null,
  ): Observable<SubmissionResult> {
    if (file === null) {
      return of({ kind: 'success' });
    }

    return this.commentsApi.uploadAttachment(commentId, file).pipe(
      map((): SubmissionResult => ({ kind: 'success' })),
      catchError((error: HttpErrorResponse) =>
        of<SubmissionResult>({ kind: 'attachment-error', error }),
      ),
    );
  }

  private rejectFile(inputElement: HTMLInputElement, message: string): void {
    this.attachmentError.set(message);
    inputElement.value = '';
  }

  private wrapSelection(openingTag: string, closingTag: string, placeholder: string): void {
    const selection = this.getSelection();

    if (selection === null) {
      return;
    }

    const text = selection.text || placeholder;
    const markup = `${openingTag}${text}${closingTag}`;
    const contentStart = selection.start + openingTag.length;

    this.replaceSelection(selection, markup, contentStart, contentStart + text.length);
  }

  private getSelection(): { start: number; end: number; text: string } | null {
    const textarea = this.messageInput?.nativeElement;

    if (textarea === undefined) {
      return null;
    }

    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;

    return { start, end, text: textarea.value.slice(start, end) };
  }

  private replaceSelection(
    selection: { start: number; end: number },
    markup: string,
    selectionStart: number,
    selectionEnd: number,
  ): void {
    const control = this.commentForm.controls.message;
    const value = control.value;
    const nextValue = value.slice(0, selection.start) + markup + value.slice(selection.end);

    control.setValue(nextValue);
    control.markAsDirty();
    this.formattingError.set(null);

    const textarea = this.messageInput?.nativeElement;
    textarea?.focus();
    textarea?.setSelectionRange(selectionStart, selectionEnd);
  }

  private isSafeHttpUrl(value: string): boolean {
    try {
      const url = new URL(value);
      return url.protocol === 'http:' || url.protocol === 'https:';
    } catch {
      return false;
    }
  }

  private escapeAttribute(value: string): string {
    return value
      .replaceAll('&', '&amp;')
      .replaceAll('"', '&quot;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;');
  }

  private resetAfterCreatedComment(): void {
    this.commentForm.reset();
    this.selectedFile.set(null);
    this.attachmentError.set(null);

    if (this.attachmentInput !== undefined) {
      this.attachmentInput.nativeElement.value = '';
    }

    this.turnstileWidget?.reset();
  }

  private resetCaptcha(): void {
    this.commentForm.controls.captchaToken.reset();
    this.turnstileWidget?.reset();
  }

  private getCreateErrorMessage(error: HttpErrorResponse): string {
    if (error.status === 0) {
      return 'Не вдалося підключитися до API. Перевірте з’єднання та спробуйте ще раз.';
    }

    const knownMessage = this.getKnownApiErrorMessage(error);

    if (knownMessage !== null) {
      return knownMessage;
    }

    return 'Не вдалося створити коментар. Спробуйте ще раз.';
  }

  private getAttachmentErrorMessage(error: HttpErrorResponse): string {
    if (error.status === 0) {
      return 'Коментар створено, але attachment не завантажено через помилку з’єднання.';
    }

    const knownMessage = this.getKnownApiErrorMessage(error);

    return knownMessage === null
      ? 'Коментар створено, але attachment не вдалося завантажити.'
      : `Коментар створено, але attachment не завантажено: ${knownMessage}`;
  }

  private getKnownApiErrorMessage(error: HttpErrorResponse): string | null {
    const problem = error.error as Partial<ApiProblemDetails> | null;
    const apiErrors = problem?.errors;

    if (!Array.isArray(apiErrors)) {
      return null;
    }

    for (const apiError of apiErrors) {
      const message = API_ERROR_MESSAGES[apiError.code];

      if (message !== undefined) {
        return message;
      }
    }

    return null;
  }
}
