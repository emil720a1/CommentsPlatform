import { DatePipe } from '@angular/common';
import { Component, forwardRef, input, output } from '@angular/core';

import { CommentCreatedEvent, CommentForm } from '../comment-form/comment-form';
import { CommentResponse } from '../../models/comment.models';
import { CommentAttachment } from '../comment-attachment/comment-attachment';
import { CommentAvatar } from '../comment-avatar/comment-avatar';

export interface CommentReplyRequestedEvent {
  id: string;
  userName: string;
}

@Component({
  selector: 'app-comment-item',
  imports: [DatePipe, CommentAttachment, CommentAvatar, CommentForm, forwardRef(() => CommentItem)],
  templateUrl: './comment-item.html',
  styleUrl: './comment-item.scss',
})
export class CommentItem {
  readonly comment = input.required<CommentResponse>();
  readonly replyingTo = input<CommentReplyRequestedEvent | null>(null);
  readonly replyRequested = output<CommentReplyRequestedEvent>();
  readonly commentCreated = output<CommentCreatedEvent>();
  readonly replyCancelled = output<void>();

  protected requestReply(): void {
    const comment = this.comment();

    this.replyRequested.emit({
      id: comment.id,
      userName: comment.userName,
    });
  }
}
