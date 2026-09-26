export type CommentSortField = 'CreatedAt';

export type CommentSortDirection = 'Ascending' | 'Descending';

export interface GetCommentsParams {
  page?: number;
  pageSize?: number;
  sortBy?: CommentSortField;
  sortDirection?: CommentSortDirection;
}

export interface CommentResponse {
  id: string;
  userName: string;
  homePage: string | null;
  createdAt: string;
  message: string;
}

export interface GetCommentsResponse {
  items: CommentResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface CreateCommentRequest {
  userName: string;
  email: string;
  homePage: string | null;
  message: string;
  parentCommentId: string | null;
  captchaToken: string;
}

export interface CreateCommentResponse {
  id: string;
}

export interface UploadAttachmentResponse {
  attachmentId: string;
}

export interface ApiError {
  code: string;
  description: string;
}

export interface ApiProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail: string;
  instance?: string;
  errors: ApiError[];
  traceId?: string;
}
