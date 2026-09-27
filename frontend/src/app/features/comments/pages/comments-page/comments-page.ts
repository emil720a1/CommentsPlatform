import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { catchError, EMPTY, finalize, map, Subject, switchMap, takeUntil } from 'rxjs';

import { CommentForm, CommentCreatedEvent } from '../../components/comment-form/comment-form';
import {
  CommentReplyRequestedEvent,
  CommentsList,
} from '../../components/comments-list/comments-list';
import {
  ApiProblemDetails,
  CommentSortDirection,
  CommentSortField,
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
  private readonly router = inject(Router);
  private readonly destroyed = new Subject<void>();
  private hasReadInitialParameters = false;

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
        switchMap((parameters) => {
          const currentParameters = this.parameters();
          const sortingChanged =
            this.hasReadInitialParameters &&
            (parameters.sortBy !== currentParameters.sortBy ||
              parameters.sortDirection !== currentParameters.sortDirection);

          this.hasReadInitialParameters = true;

          if (sortingChanged && parameters.page !== CommentsPage.defaultPage) {
            void this.navigateToPage(CommentsPage.defaultPage);
            return EMPTY;
          }

          this.parameters.set(parameters);
          return this.requestComments(parameters);
        }),
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

  protected goToPreviousPage(): void {
    const page = this.commentsPage();

    if (page?.hasPreviousPage) {
      void this.navigateToPage(page.page - 1);
    }
  }

  protected goToNextPage(): void {
    const page = this.commentsPage();

    if (page?.hasNextPage) {
      void this.navigateToPage(page.page + 1);
    }
  }

  protected changeSortField(event: Event): void {
    const sortBy = (event.target as HTMLSelectElement).value as CommentSortField;
    void this.navigateToSorting(sortBy, this.parameters().sortDirection);
  }

  protected changeSortDirection(event: Event): void {
    const sortDirection = (event.target as HTMLSelectElement).value as CommentSortDirection;
    void this.navigateToSorting(this.parameters().sortBy, sortDirection);
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
    const sortBy = this.readSortField(queryParameters.get('sortBy'));
    const sortDirection = this.readSortDirection(queryParameters.get('sortDirection'));

    return {
      page,
      pageSize: CommentsPage.defaultPageSize,
      sortBy,
      sortDirection,
    };
  }

  private navigateToSorting(
    sortBy: CommentSortField,
    sortDirection: CommentSortDirection,
  ): Promise<boolean> {
    return this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        page: CommentsPage.defaultPage,
        pageSize: CommentsPage.defaultPageSize,
        sortBy,
        sortDirection,
      },
      queryParamsHandling: 'merge',
    });
  }

  private readSortField(value: string | null): CommentSortField {
    return value === 'UserName' || value === 'Email' || value === 'CreatedAt' ? value : 'CreatedAt';
  }

  private readSortDirection(value: string | null): CommentSortDirection {
    return value === 'Ascending' || value === 'Descending' ? value : 'Descending';
  }

  private navigateToPage(page: number): Promise<boolean> {
    return this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        page,
        pageSize: CommentsPage.defaultPageSize,
      },
      queryParamsHandling: 'merge',
    });
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
