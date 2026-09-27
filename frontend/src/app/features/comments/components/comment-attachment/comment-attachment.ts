import { Component, computed, input, signal } from '@angular/core';

import { environment } from '../../../../../environments/environment';
import { AttachmentResponse } from '../../models/comment.models';

@Component({
  selector: 'app-comment-attachment',
  templateUrl: './comment-attachment.html',
  styleUrl: './comment-attachment.scss',
})
export class CommentAttachment {
  readonly attachment = input.required<AttachmentResponse>();

  protected readonly previewFailed = signal(false);

  protected readonly downloadUrl = computed(() =>
    new URL(this.attachment().downloadUrl, environment.apiUrl).toString(),
  );

  protected readonly previewUrl = computed(() => {
    const url = new URL(this.downloadUrl());
    url.searchParams.set('inline', 'true');
    return url.toString();
  });

  protected readonly isImage = computed(() => this.attachment().contentType.startsWith('image/'));

  protected readonly formattedSize = computed(() =>
    this.formatFileSize(this.attachment().fileSizeBytes),
  );

  protected onPreviewError(): void {
    this.previewFailed.set(true);
  }

  private formatFileSize(bytes: number): string {
    if (bytes < 1024) {
      return `${bytes} B`;
    }

    if (bytes < 1024 * 1024) {
      return `${(bytes / 1024).toFixed(1)} KB`;
    }

    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }
}
