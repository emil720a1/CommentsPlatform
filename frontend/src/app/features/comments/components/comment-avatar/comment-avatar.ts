import { Component, computed, input } from '@angular/core';

const AVATAR_COLORS = ['#4f46a5', '#315f9f', '#176b87', '#6852a5', '#3e6f72', '#6b4f8f'];
const FALLBACK_COLOR = '#667085';

@Component({
  selector: 'app-comment-avatar',
  templateUrl: './comment-avatar.html',
  styleUrl: './comment-avatar.scss',
})
export class CommentAvatar {
  readonly userName = input<string | null | undefined>('');

  protected readonly normalizedUserName = computed(() => this.userName()?.trim() ?? '');

  protected readonly initial = computed(() => {
    const [firstCharacter] = Array.from(this.normalizedUserName());
    return firstCharacter?.toLocaleUpperCase('uk-UA') ?? '?';
  });

  protected readonly ariaLabel = computed(() => {
    const userName = this.normalizedUserName();
    return userName.length > 0 ? `Аватар користувача ${userName}` : 'Аватар невідомого користувача';
  });

  protected readonly backgroundColor = computed(() => {
    const userName = this.normalizedUserName().toLocaleLowerCase('uk-UA');

    if (userName.length === 0) {
      return FALLBACK_COLOR;
    }

    let hash = 0;

    for (const character of userName) {
      hash = (Math.imul(hash, 31) + (character.codePointAt(0) ?? 0)) >>> 0;
    }

    return AVATAR_COLORS[hash % AVATAR_COLORS.length];
  });
}
