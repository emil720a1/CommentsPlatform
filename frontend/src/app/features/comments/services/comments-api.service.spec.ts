import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../../environments/environment';
import { CreateCommentRequest } from '../models/comment.models';
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
});
