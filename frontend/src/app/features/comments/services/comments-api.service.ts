import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { GetCommentsParams, GetCommentsResponse } from '../models/comment.models';

@Injectable({
  providedIn: 'root',
})
export class CommentsApiService {
  private readonly http = inject(HttpClient);

  private readonly commentsUrl = `${environment.apiUrl}/comments`;

  getComments(params: GetCommentsParams = {}): Observable<GetCommentsResponse> {
    let httpParams = new HttpParams();

    if (params.page !== undefined) {
      httpParams = httpParams.set('page', params.page);
    }

    if (params.pageSize !== undefined) {
      httpParams = httpParams.set('pageSize', params.pageSize);
    }

    if (params.sortBy !== undefined) {
      httpParams = httpParams.set('sortBy', params.sortBy);
    }

    if (params.sortDirection !== undefined) {
      httpParams = httpParams.set('sortDirection', params.sortDirection);
    }

    return this.http.get<GetCommentsResponse>(this.commentsUrl, {
      params: httpParams,
    });
  }
}
