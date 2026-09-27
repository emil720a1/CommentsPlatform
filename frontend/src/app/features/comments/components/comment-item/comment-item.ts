import { DatePipe } from '@angular/common';
import { Component, forwardRef, input, output } from '@angular/core';

import { CommentResponse } from '../../models/comment.models';
import { CommentAttachment } from '../comment-attachment/comment-attachment';

export interface CommentReplyRequestedEvent {
  id: string;
  userName: string;
}

@Component({
  selector: 'app-comment-item',
  imports: [DatePipe, CommentAttachment, forwardRef(() => CommentItem)],
  templateUrl: './comment-item.html',
  styleUrl: './comment-item.scss',
})
export class CommentItem {
  readonly comment = input.required<CommentResponse>();
  readonly replyRequested = output<CommentReplyRequestedEvent>();

  protected requestReply(): void {
    const comment = this.comment();

    this.replyRequested.emit({
      id: comment.id,
      userName: comment.userName,
    });
  }
}
