import { ComponentFixture, TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import { ImageLightbox } from './image-lightbox';

describe('ImageLightbox', () => {
  let fixture: ComponentFixture<ImageLightbox>;
  let previouslyFocusedButton: HTMLButtonElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ImageLightbox],
    }).compileComponents();

    previouslyFocusedButton = document.createElement('button');
    document.body.append(previouslyFocusedButton);
    previouslyFocusedButton.focus();

    fixture = TestBed.createComponent(ImageLightbox);
    fixture.componentRef.setInput('imageUrl', 'https://api.example.com/image.png');
    fixture.componentRef.setInput('alternativeText', 'image.png');
    fixture.detectChanges();
  });

  afterEach(() => {
    fixture.destroy();
    previouslyFocusedButton.remove();
  });

  it('uses accessible dialog semantics, traps focus and blocks background scrolling', () => {
    const dialog = fixture.nativeElement.querySelector('[role="dialog"]') as HTMLElement;
    const closeButton = fixture.nativeElement.querySelector('.close-button') as HTMLButtonElement;

    expect(dialog.getAttribute('aria-modal')).toBe('true');
    expect(dialog.getAttribute('aria-label')).toContain('image.png');
    expect(document.activeElement).toBe(closeButton);
    expect(document.body.style.overflow).toBe('hidden');

    closeButton.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', bubbles: true }));
    expect(document.activeElement).toBe(closeButton);
  });

  it('closes with the button, Escape key and backdrop', () => {
    const closeHandler = vi.fn();
    fixture.componentInstance.closed.subscribe(closeHandler);

    (fixture.nativeElement.querySelector('.close-button') as HTMLButtonElement).click();
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    (fixture.nativeElement.querySelector('.backdrop') as HTMLElement).click();

    expect(closeHandler).toHaveBeenCalledTimes(3);
  });

  it('does not close when the image area is clicked', () => {
    const closeHandler = vi.fn();
    fixture.componentInstance.closed.subscribe(closeHandler);

    (fixture.nativeElement.querySelector('.lightbox') as HTMLElement).click();

    expect(closeHandler).not.toHaveBeenCalled();
  });

  it('shows loading and error states safely', () => {
    expect(fixture.nativeElement.textContent).toContain('Завантаження зображення...');

    const image = fixture.nativeElement.querySelector('img') as HTMLImageElement;
    image.dispatchEvent(new Event('error'));
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('img')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Не вдалося завантажити зображення.');
  });

  it('restores focus and background scrolling when destroyed', () => {
    fixture.destroy();

    expect(document.activeElement).toBe(previouslyFocusedButton);
    expect(document.body.style.overflow).toBe('');
  });
});
