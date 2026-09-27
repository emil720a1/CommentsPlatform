import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { catchError, EMPTY, finalize, map, Subject, switchMap, takeUntil, tap } from 'rxjs';

import { CommentCreatedEvent, CommentForm } from '../../components/comment-form/comment-form';
import {
  ApiProblemDetails,
  CommentSortDirection,
  GetCommentsParams,
  GetCommentsResponse,
} from '../../models/comment.models';
import { CommentsApiService } from '../../services/comments-api.service';

@Component({
  selector: 'app-comments-page',
  imports: [DatePipe, CommentForm],
  templateUrl: './comments-page.html',
  styleUrl: './comments-page.scss',
})
export class CommentsPage implements OnInit, OnDestroy {
  private static readonly defaultPage = 1;
  private static readonly defaultPageSize = 25;
  private static readonly allowedPageSizes = [10, 25, 50] as const;

  private readonly commentsApi = inject(CommentsApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyed = new Subject<void>();

  protected readonly commentsPage = signal<GetCommentsResponse | null>(null);

  protected readonly isLoading = signal(false);

  protected readonly errorMessage = signal<string | null>(null);

  protected readonly replyingTo = signal<{ id: string; userName: string } | null>(null);

  protected readonly submissionWarning = signal<string | null>(null);

  protected readonly parameters = signal<Required<GetCommentsParams>>({
    page: CommentsPage.defaultPage,
    pageSize: CommentsPage.defaultPageSize,
    sortBy: 'CreatedAt',
    sortDirection: 'Descending',
  });

  protected readonly pageNumbers = computed(() => {
    const totalPages = this.commentsPage()?.totalPages ?? 0;
    const currentPage = this.parameters().page;
    const firstPage = Math.max(1, Math.min(currentPage - 3, totalPages - 6));
    const lastPage = Math.min(totalPages, firstPage + 6);

    return Array.from(
      { length: Math.max(0, lastPage - firstPage + 1) },
      (_, index) => firstPage + index,
    );
  });

  protected readonly pageSizes = CommentsPage.allowedPageSizes;

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

  protected goToPage(page: number): void {
    const totalPages = this.commentsPage()?.totalPages ?? 0;

    if (page < 1 || page > totalPages || page === this.parameters().page) {
      return;
    }

    void this.updateQueryParameters({ page });
  }

  protected changeSortDirection(event: Event): void {
    const sortDirection = (event.target as HTMLSelectElement).value as CommentSortDirection;

    if (sortDirection !== 'Ascending' && sortDirection !== 'Descending') {
      return;
    }

    void this.updateQueryParameters({ page: 1, sortDirection });
  }

  protected changePageSize(event: Event): void {
    const pageSize = Number((event.target as HTMLSelectElement).value);

    if (!CommentsPage.allowedPageSizes.includes(pageSize as 10 | 25 | 50)) {
      return;
    }

    void this.updateQueryParameters({ page: 1, pageSize });
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

  protected startReply(id: string, userName: string): void {
    this.submissionWarning.set(null);
    this.replyingTo.set({ id, userName });
  }

  protected cancelReply(): void {
    this.replyingTo.set(null);
  }

  protected onCommentCreated(event: CommentCreatedEvent): void {
    this.submissionWarning.set(event.attachmentErrorMessage);
    this.replyingTo.set(null);
    this.loadComments();
  }

  private readParameters(queryParameters: ParamMap): Required<GetCommentsParams> {
    const page = this.readPositiveInteger(queryParameters.get('page'), CommentsPage.defaultPage);
    const requestedPageSize = this.readPositiveInteger(
      queryParameters.get('pageSize'),
      CommentsPage.defaultPageSize,
    );
    const pageSize = CommentsPage.allowedPageSizes.includes(requestedPageSize as 10 | 25 | 50)
      ? requestedPageSize
      : CommentsPage.defaultPageSize;
    const sortDirection: CommentSortDirection =
      queryParameters.get('sortDirection') === 'Ascending' ? 'Ascending' : 'Descending';

    return {
      page,
      pageSize,
      sortBy: 'CreatedAt',
      sortDirection,
    };
  }

  private readPositiveInteger(value: string | null, fallback: number): number {
    if (value === null || !/^\d+$/.test(value)) {
      return fallback;
    }

    const parsedValue = Number(value);
    return Number.isSafeInteger(parsedValue) && parsedValue > 0 ? parsedValue : fallback;
  }

  private updateQueryParameters(changes: Partial<Required<GetCommentsParams>>): Promise<boolean> {
    const parameters = { ...this.parameters(), ...changes };

    return this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        page: parameters.page,
        pageSize: parameters.pageSize,
        sortBy: parameters.sortBy,
        sortDirection: parameters.sortDirection,
      },
    });
  }

  private getErrorMessage(error: HttpErrorResponse): string {
    if (error.status === 0) {
      return 'Не вдалося підключитися до API.';
    }

    const problem = error.error as Partial<ApiProblemDetails> | null;

    return problem?.detail ?? 'Не вдалося завантажити коментарі.';
  }
}
