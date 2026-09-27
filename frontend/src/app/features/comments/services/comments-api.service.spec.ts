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
