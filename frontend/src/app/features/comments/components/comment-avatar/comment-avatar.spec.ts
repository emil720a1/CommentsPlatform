import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CommentAvatar } from './comment-avatar';

describe('CommentAvatar', () => {
  let fixture: ComponentFixture<CommentAvatar>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [CommentAvatar] }).compileComponents();
    fixture = TestBed.createComponent(CommentAvatar);
  });

  it('renders the first username letter in uppercase', () => {
    fixture.componentRef.setInput('userName', 'alice1');
    fixture.detectChanges();

    expect(getAvatar().textContent?.trim()).toBe('A');
    expect(getAvatar().getAttribute('aria-label')).toBe('Аватар користувача alice1');
  });

  it('generates a stable color for the same normalized username', () => {
    fixture.componentRef.setInput('userName', 'Alice1');
    fixture.detectChanges();
    const firstColor = getAvatar().style.backgroundColor;

    fixture.componentRef.setInput('userName', '  alice1  ');
    fixture.detectChanges();

    expect(getAvatar().style.backgroundColor).toBe(firstColor);
  });

  it('uses a neutral fallback for an empty username', () => {
    fixture.componentRef.setInput('userName', '   ');
    fixture.detectChanges();

    expect(getAvatar().textContent?.trim()).toBe('?');
    expect(getAvatar().style.backgroundColor).toBe('rgb(102, 112, 133)');
    expect(getAvatar().getAttribute('aria-label')).toBe('Аватар невідомого користувача');
  });

  function getAvatar(): HTMLSpanElement {
    return fixture.nativeElement.querySelector('.avatar') as HTMLSpanElement;
  }
});
