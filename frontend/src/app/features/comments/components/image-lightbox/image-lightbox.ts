import {
  AfterViewInit,
  Component,
  ElementRef,
  HostListener,
  input,
  OnDestroy,
  output,
  signal,
  viewChild,
} from '@angular/core';

@Component({
  selector: 'app-image-lightbox',
  templateUrl: './image-lightbox.html',
  styleUrl: './image-lightbox.scss',
})
export class ImageLightbox implements AfterViewInit, OnDestroy {
  readonly imageUrl = input.required<string>();
  readonly alternativeText = input.required<string>();
  readonly closed = output<void>();

  private readonly dialog = viewChild.required<ElementRef<HTMLElement>>('dialog');
  private readonly previouslyFocusedElement = document.activeElement as HTMLElement | null;
  private readonly previousBodyOverflow = document.body.style.overflow;

  protected readonly isLoading = signal(true);
  protected readonly loadFailed = signal(false);

  constructor() {
    document.body.style.overflow = 'hidden';
  }

  ngAfterViewInit(): void {
    this.dialog().nativeElement.querySelector<HTMLElement>('button')?.focus();
  }

  ngOnDestroy(): void {
    document.body.style.overflow = this.previousBodyOverflow;

    if (this.previouslyFocusedElement?.isConnected) {
      this.previouslyFocusedElement.focus();
    }
  }

  @HostListener('document:keydown', ['$event'])
  protected onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      this.close();
      return;
    }

    if (event.key === 'Tab') {
      this.keepFocusInsideDialog(event);
    }
  }

  protected close(): void {
    this.closed.emit();
  }

  protected closeFromBackdrop(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.close();
    }
  }

  protected onImageLoaded(): void {
    this.isLoading.set(false);
  }

  protected onImageError(): void {
    this.isLoading.set(false);
    this.loadFailed.set(true);
  }

  private keepFocusInsideDialog(event: KeyboardEvent): void {
    const focusableElements = Array.from(
      this.dialog().nativeElement.querySelectorAll<HTMLElement>(
        'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])',
      ),
    );

    if (focusableElements.length === 0) {
      event.preventDefault();
      this.dialog().nativeElement.focus();
      return;
    }

    const first = focusableElements[0];
    const last = focusableElements[focusableElements.length - 1];
    const activeElement = document.activeElement;

    if (event.shiftKey && activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  }
}
