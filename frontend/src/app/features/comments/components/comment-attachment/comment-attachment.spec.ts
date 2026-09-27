import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AttachmentResponse } from '../../models/comment.models';
import { CommentAttachment } from './comment-attachment';

describe('CommentAttachment', () => {
  let fixture: ComponentFixture<CommentAttachment>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommentAttachment],
    }).compileComponents();

    fixture = TestBed.createComponent(CommentAttachment);
  });

  it('renders an image preview, file name, size and safe download URL', () => {
    fixture.componentRef.setInput(
      'attachment',
      createAttachment({
        originalFileName: 'photo.png',
        contentType: 'image/png',
        fileSizeBytes: 2048,
      }),
    );
    fixture.detectChanges();

    const image = fixture.nativeElement.querySelector('img') as HTMLImageElement;
    const links = fixture.nativeElement.querySelectorAll('a') as NodeListOf<HTMLAnchorElement>;

    expect(image).not.toBeNull();
    expect(image.alt).toBe('photo.png');
    expect(image.src).toContain('/api/comments/comment-id/attachments/attachment-id');
    expect(image.src).toContain('inline=true');
    expect(links[1].textContent).toContain('photo.png');
    expect(fixture.nativeElement.textContent).toContain('2.0 KB');
  });

  it('renders a text attachment as a download link without an image preview', () => {
    fixture.componentRef.setInput(
      'attachment',
      createAttachment({
        originalFileName: 'notes.txt',
        contentType: 'text/plain',
      }),
    );
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('img')).toBeNull();
    expect(fixture.nativeElement.querySelector('a').textContent).toContain('notes.txt');
  });

  it('shows a fallback when the image preview cannot be loaded', () => {
    fixture.componentRef.setInput('attachment', createAttachment());
    fixture.detectChanges();

    const image = fixture.nativeElement.querySelector('img') as HTMLImageElement;
    image.dispatchEvent(new Event('error'));
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('img')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Попередній перегляд недоступний.');
    expect(fixture.nativeElement.querySelector('a')).not.toBeNull();
  });

  function createAttachment(overrides: Partial<AttachmentResponse> = {}): AttachmentResponse {
    return {
      id: 'attachment-id',
      originalFileName: 'image.png',
      contentType: 'image/png',
      fileSizeBytes: 512,
      width: null,
      height: null,
      createdAt: '2026-09-27T10:00:00Z',
      downloadUrl: '/api/comments/comment-id/attachments/attachment-id',
      ...overrides,
    };
  }
});
