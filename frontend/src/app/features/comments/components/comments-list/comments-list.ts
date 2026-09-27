import { DatePipe } from '@angular/common';
import { Component, input, output } from '@angular/core';

import { CommentResponse } from '../../models/comment.models';
import { CommentAttachment } from '../comment-attachment/comment-attachment';

export interface CommentReplyRequestedEvent {
  id: string;
  userName: string;
}

@Component({
  selector: 'app-comments-list',
  imports: [DatePipe, CommentAttachment],
  templateUrl: './comments-list.html',
  styleUrl: './comments-list.scss',
})
export class CommentsList {
  readonly comments = input.required<readonly CommentResponse[]>();
  readonly replyRequested = output<CommentReplyRequestedEvent>();

  protected requestReply(comment: CommentResponse): void {
    this.replyRequested.emit({
      id: comment.id,
      userName: comment.userName,
    });
  }
}
