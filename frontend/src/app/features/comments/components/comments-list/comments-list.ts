import { Component, input, output } from '@angular/core';

import { CommentResponse } from '../../models/comment.models';
import { CommentCreatedEvent } from '../comment-form/comment-form';
import { CommentItem, CommentReplyRequestedEvent } from '../comment-item/comment-item';

export type { CommentReplyRequestedEvent } from '../comment-item/comment-item';

@Component({
  selector: 'app-comments-list',
  imports: [CommentItem],
  templateUrl: './comments-list.html',
  styleUrl: './comments-list.scss',
})
export class CommentsList {
  readonly comments = input.required<readonly CommentResponse[]>();
  readonly replyingTo = input<CommentReplyRequestedEvent | null>(null);
  readonly replyRequested = output<CommentReplyRequestedEvent>();
  readonly commentCreated = output<CommentCreatedEvent>();
  readonly replyCancelled = output<void>();
}
