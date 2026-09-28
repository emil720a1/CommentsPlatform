import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../../environments/environment';
import { CreateCommentRequest, GetCommentsResponse } from '../models/comment.models';
import { CommentsApiService } from './comments-api.service';

describe('CommentsApiService', () => {
  let service: CommentsApiService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CommentsApiService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(CommentsApiService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('creates a comment with the backend request contract', () => {
    const request: CreateCommentRequest = {
      userName: 'Alice1',
      email: 'alice@example.com',
      homePage: 'https://example.com',
      message: 'Hello',
      parentCommentId: null,
      captchaToken: 'captcha-token',
    };

    service.createComment(request).subscribe((response) => {
      expect(response).toEqual({ id: 'comment-id' });
    });

    const apiRequest = httpTesting.expectOne(`${environment.apiUrl}/comments`);

    expect(apiRequest.request.method).toBe('POST');
    expect(apiRequest.request.body).toEqual(request);

    apiRequest.flush({ id: 'comment-id' });
  });

  it('loads comments with pagination and sorting query parameters', () => {
    service
      .getComments({
        page: 2,
        pageSize: 50,
        sortBy: 'CreatedAt',
        sortDirection: 'Ascending',
      })
      .subscribe();

    const apiRequest = httpTesting.expectOne(
      (request) => request.url === `${environment.apiUrl}/comments`,
    );

    expect(apiRequest.request.method).toBe('GET');
    expect(apiRequest.request.params.get('page')).toBe('2');
    expect(apiRequest.request.params.get('pageSize')).toBe('50');
    expect(apiRequest.request.params.get('sortBy')).toBe('CreatedAt');
    expect(apiRequest.request.params.get('sortDirection')).toBe('Ascending');

    apiRequest.flush({
      items: [],
      page: 2,
      pageSize: 50,
      totalCount: 0,
      totalPages: 0,
      hasPreviousPage: false,
      hasNextPage: false,
    });
  });

  it('uploads an attachment as multipart form data', () => {
    const file = new File(['hello'], 'note.txt', { type: 'text/plain' });

    service.uploadAttachment('comment-id', file).subscribe((response) => {
      expect(response).toEqual({ attachmentId: 'attachment-id' });
    });

    const apiRequest = httpTesting.expectOne(
      `${environment.apiUrl}/comments/comment-id/attachments`,
    );

    expect(apiRequest.request.method).toBe('POST');
    expect(apiRequest.request.body).toBeInstanceOf(FormData);

    const uploadedFile = (apiRequest.request.body as FormData).get('file') as File;

    expect(uploadedFile.name).toBe(file.name);
    expect(uploadedFile.type).toBe(file.type);
    expect(uploadedFile.size).toBe(file.size);

    apiRequest.flush({ attachmentId: 'attachment-id' });
  });

  it('sends a GET request without optional query parameters when none are specified', () => {
    service.getComments().subscribe();

    const apiRequest = httpTesting.expectOne(`${environment.apiUrl}/comments`);

    expect(apiRequest.request.method).toBe('GET');
    expect(apiRequest.request.params.keys()).toHaveLength(0);

    apiRequest.flush({
      items: [],
      page: 1,
      pageSize: 25,
      totalCount: 0,
      totalPages: 0,
      hasPreviousPage: false,
      hasNextPage: false,
    });
  });

  it('sends only the specified subset of query parameters', () => {
    service.getComments({ page: 3, sortBy: 'Email' }).subscribe();

    const apiRequest = httpTesting.expectOne(
      (request) => request.url === `${environment.apiUrl}/comments`,
    );

    expect(apiRequest.request.params.get('page')).toBe('3');
    expect(apiRequest.request.params.get('sortBy')).toBe('Email');
    expect(apiRequest.request.params.has('pageSize')).toBe(false);
    expect(apiRequest.request.params.has('sortDirection')).toBe(false);

    apiRequest.flush({
      items: [],
      page: 3,
      pageSize: 25,
      totalCount: 0,
      totalPages: 0,
      hasPreviousPage: true,
      hasNextPage: false,
    });
  });

  it('returns a parsed success response from getComments', () => {
    const expected: GetCommentsResponse = {
      items: [
        {
          id: 'c1',
          userName: 'User1',
          email: 'user@example.com',
          homePage: null,
          createdAt: '2026-09-27T10:00:00Z',
          message: 'Test message',
          attachments: [],
          replies: [],
        },
      ],
      page: 1,
      pageSize: 25,
      totalCount: 1,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    };

    let result: GetCommentsResponse | undefined;

    service.getComments({ page: 1 }).subscribe((response) => {
      result = response;
    });

    httpTesting
      .expectOne((request) => request.url === `${environment.apiUrl}/comments`)
      .flush(expected);

    expect(result).toEqual(expected);
  });

  it('propagates an API error from createComment', () => {
    let error: HttpErrorResponse | undefined;

    service
      .createComment({
        userName: 'Alice1',
        email: 'alice@example.com',
        homePage: null,
        message: 'Hello',
        parentCommentId: null,
        captchaToken: 'token',
      })
      .subscribe({
        error: (err: HttpErrorResponse) => {
          error = err;
        },
      });

    httpTesting.expectOne(`${environment.apiUrl}/comments`).flush(
      {
        title: 'Validation Error',
        status: 400,
        detail: 'Invalid data',
        errors: [{ code: 'Comments.UserName.Required', description: 'Username is required' }],
      },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(error?.status).toBe(400);
  });

  it('propagates a network error from getComments', () => {
    let error: HttpErrorResponse | undefined;

    service.getComments({ page: 1 }).subscribe({
      error: (err: HttpErrorResponse) => {
        error = err;
      },
    });

    httpTesting
      .expectOne((request) => request.url === `${environment.apiUrl}/comments`)
      .error(new ProgressEvent('error'));

    expect(error?.status).toBe(0);
  });

  it('propagates a network error from uploadAttachment', () => {
    const file = new File(['data'], 'file.txt', { type: 'text/plain' });
    let error: HttpErrorResponse | undefined;

    service.uploadAttachment('comment-id', file).subscribe({
      error: (err: HttpErrorResponse) => {
        error = err;
      },
    });

    httpTesting
      .expectOne(`${environment.apiUrl}/comments/comment-id/attachments`)
      .error(new ProgressEvent('error'));

    expect(error?.status).toBe(0);
  });
});
