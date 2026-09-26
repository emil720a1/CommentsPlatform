import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { finalize } from 'rxjs';

import { ApiProblemDetails, GetCommentsResponse } from '../../models/comment.models';
import { CommentsApiService } from '../../services/comments-api.service';

@Component({
  selector: 'app-comments-page',
  imports: [DatePipe],
  templateUrl: './comments-page.html',
  styleUrl: './comments-page.scss',
})
export class CommentsPage implements OnInit {
  private readonly commentsApi = inject(CommentsApiService);

  protected readonly commentsPage = signal<GetCommentsResponse | null>(null);

  protected readonly isLoading = signal(false);

  protected readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadComments();
  }

  protected loadComments(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.commentsPage.set(null);

    this.commentsApi
      .getComments()
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (response) => {
          this.commentsPage.set(response);
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage.set(this.getErrorMessage(error));
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
