import { ComponentFixture, TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

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
    expect(links[0].textContent).toContain('photo.png');
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

  it('opens and closes the image lightbox without changing the download link', () => {
    fixture.componentRef.setInput('attachment', createAttachment());
    fixture.detectChanges();

    const downloadLink = fixture.nativeElement.querySelector('a') as HTMLAnchorElement;
    (fixture.nativeElement.querySelector('.preview-button') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="dialog"]')).not.toBeNull();
    expect(downloadLink.hasAttribute('download')).toBe(true);

    (fixture.nativeElement.querySelector('.close-button') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="dialog"]')).toBeNull();
  });

  it('renders the file name in the download link', () => {
    fixture.componentRef.setInput(
      'attachment',
      createAttachment({ originalFileName: 'document.txt', contentType: 'text/plain' }),
    );
    fixture.detectChanges();

    const downloadLink = fixture.nativeElement.querySelector('a') as HTMLAnchorElement;

    expect(downloadLink.textContent?.trim()).toContain('document.txt');
    expect(downloadLink.hasAttribute('download')).toBe(true);
  });

  it('displays the file size in bytes for small files', () => {
    fixture.componentRef.setInput('attachment', createAttachment({ fileSizeBytes: 500 }));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('500 B');
  });

  it('displays the file size in KB for medium files', () => {
    fixture.componentRef.setInput('attachment', createAttachment({ fileSizeBytes: 51200 }));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('50.0 KB');
  });

  it('displays the file size in MB for large files', () => {
    fixture.componentRef.setInput(
      'attachment',
      createAttachment({ fileSizeBytes: 5 * 1024 * 1024 }),
    );
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('5.0 MB');
  });

  it('has a labeled section for accessibility', () => {
    fixture.componentRef.setInput(
      'attachment',
      createAttachment({ originalFileName: 'report.png' }),
    );
    fixture.detectChanges();

    const section = fixture.nativeElement.querySelector('section.attachment') as HTMLElement;

    expect(section.getAttribute('aria-label')).toBe('Вкладення report.png');
  });

  it('renders the preview button with an accessible label for images', () => {
    fixture.componentRef.setInput(
      'attachment',
      createAttachment({ originalFileName: 'photo.jpg' }),
    );
    fixture.detectChanges();

    const previewButton = fixture.nativeElement.querySelector(
      '.preview-button',
    ) as HTMLButtonElement;

    expect(previewButton.getAttribute('aria-label')).toBe('Відкрити зображення photo.jpg');
  });

  it('closes the lightbox with the Escape key', () => {
    fixture.componentRef.setInput('attachment', createAttachment());
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.preview-button') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="dialog"]')).not.toBeNull();

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="dialog"]')).toBeNull();
  });

  it('closes the lightbox when the backdrop is clicked', () => {
    fixture.componentRef.setInput('attachment', createAttachment());
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.preview-button') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="dialog"]')).not.toBeNull();

    (fixture.nativeElement.querySelector('.backdrop') as HTMLElement).click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="dialog"]')).toBeNull();
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
