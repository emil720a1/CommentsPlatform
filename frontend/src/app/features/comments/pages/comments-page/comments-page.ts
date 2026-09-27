import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { catchError, EMPTY, finalize, map, Subject, switchMap, takeUntil, tap } from 'rxjs';

import { CommentForm, CommentCreatedEvent } from '../../components/comment-form/comment-form';
import {
  CommentReplyRequestedEvent,
  CommentsList,
} from '../../components/comments-list/comments-list';
import {
  ApiProblemDetails,
  CommentSortDirection,
  GetCommentsParams,
  GetCommentsResponse,
} from '../../models/comment.models';
import { CommentsApiService } from '../../services/comments-api.service';

@Component({
  selector: 'app-comments-page',
  imports: [CommentForm, CommentsList],
  templateUrl: './comments-page.html',
  styleUrl: './comments-page.scss',
})
export class CommentsPage implements OnInit, OnDestroy {
  private static readonly defaultPage = 1;
  private static readonly defaultPageSize = 25;

  private readonly commentsApi = inject(CommentsApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyed = new Subject<void>();

  protected readonly commentsPage = signal<GetCommentsResponse | null>(null);
  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly replyingTo = signal<CommentReplyRequestedEvent | null>(null);
  protected readonly submissionWarning = signal<string | null>(null);
  protected readonly parameters = signal<Required<GetCommentsParams>>({
    page: CommentsPage.defaultPage,
    pageSize: CommentsPage.defaultPageSize,
    sortBy: 'CreatedAt',
    sortDirection: 'Descending',
  });

  ngOnInit(): void {
    this.route.queryParamMap
      .pipe(
        map((queryParameters) => this.readParameters(queryParameters)),
        tap((parameters) => this.parameters.set(parameters)),
        switchMap((parameters) => this.requestComments(parameters)),
        takeUntil(this.destroyed),
      )
      .subscribe((response) => this.commentsPage.set(response));
  }

  ngOnDestroy(): void {
    this.destroyed.next();
    this.destroyed.complete();
  }

  protected loadComments(): void {
    this.requestComments(this.parameters())
      .pipe(takeUntil(this.destroyed))
      .subscribe((response) => this.commentsPage.set(response));
  }

  protected startReply(reply: CommentReplyRequestedEvent): void {
    this.submissionWarning.set(null);
    this.replyingTo.set(reply);
  }

  protected cancelReply(): void {
    this.replyingTo.set(null);
  }

  protected onCommentCreated(event: CommentCreatedEvent): void {
    this.submissionWarning.set(event.attachmentErrorMessage);
    this.replyingTo.set(null);
    this.loadComments();
  }

  private requestComments(parameters: Required<GetCommentsParams>) {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    return this.commentsApi.getComments(parameters).pipe(
      catchError((error: HttpErrorResponse) => {
        this.commentsPage.set(null);
        this.errorMessage.set(this.getErrorMessage(error));
        return EMPTY;
      }),
      finalize(() => this.isLoading.set(false)),
    );
  }

  private readParameters(queryParameters: ParamMap): Required<GetCommentsParams> {
    const page = this.readIntegerInRange(
      queryParameters.get('page'),
      CommentsPage.defaultPage,
      Number.MAX_SAFE_INTEGER,
    );
    const pageSize = this.readIntegerInRange(
      queryParameters.get('pageSize'),
      CommentsPage.defaultPageSize,
      100,
    );
    const sortDirection: CommentSortDirection =
      queryParameters.get('sortDirection') === 'Ascending' ? 'Ascending' : 'Descending';

    return {
      page,
      pageSize,
      sortBy: 'CreatedAt',
      sortDirection,
    };
  }

  private readIntegerInRange(value: string | null, fallback: number, maximum: number): number {
    if (value === null || !/^\d+$/.test(value)) {
      return fallback;
    }

    const parsedValue = Number(value);
    return Number.isSafeInteger(parsedValue) && parsedValue > 0 && parsedValue <= maximum
      ? parsedValue
      : fallback;
  }

  private getErrorMessage(error: HttpErrorResponse): string {
    if (error.status === 0) {
      return 'Не вдалося підключитися до API.';
    }

    const problem = error.error as Partial<ApiProblemDetails> | null;
    return problem?.detail ?? 'Не вдалося завантажити коментарі.';
  }
}
