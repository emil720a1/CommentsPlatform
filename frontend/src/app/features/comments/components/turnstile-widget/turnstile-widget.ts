import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  output,
  signal,
  ViewChild,
} from '@angular/core';

import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-turnstile-widget',
  imports: [],
  templateUrl: './turnstile-widget.html',
  styleUrl: './turnstile-widget.scss',
})
export class TurnstileWidget implements AfterViewInit, OnDestroy {
  @ViewChild('container', { static: true })
  private container!: ElementRef<HTMLDivElement>;

  readonly tokenChange = output<string | null>();

  protected readonly errorMessage = signal<string | null>(null);

  private widgetId: string | null = null;

  private scriptElement: HTMLScriptElement | null = null;

  private isDestroyed = false;

  private readonly handleScriptLoad = (): void => {
    this.renderWidget();
  };

  private readonly handleScriptError = (): void => {
    this.handleFailure('Не вдалося завантажити CAPTCHA. Оновіть сторінку та спробуйте ще раз.');
  };

  ngAfterViewInit(): void {
    if (window.turnstile !== undefined) {
      this.renderWidget();
      return;
    }

    const script = document.getElementById('turnstile-script');

    if (!(script instanceof HTMLScriptElement)) {
      this.handleFailure('Не вдалося ініціалізувати CAPTCHA.');
      return;
    }

    this.scriptElement = script;

    script.addEventListener('load', this.handleScriptLoad, { once: true });
    script.addEventListener('error', this.handleScriptError, { once: true });
  }

  ngOnDestroy(): void {
    this.isDestroyed = true;

    this.scriptElement?.removeEventListener('load', this.handleScriptLoad);
    this.scriptElement?.removeEventListener('error', this.handleScriptError);

    if (this.widgetId !== null) {
      window.turnstile?.remove(this.widgetId);
    }

    this.widgetId = null;
  }

  reset(): void {
    if (this.widgetId !== null) {
      window.turnstile?.reset(this.widgetId);
    }

    this.errorMessage.set(null);
    this.tokenChange.emit(null);
  }

  private renderWidget(): void {
    if (this.isDestroyed || this.widgetId !== null) {
      return;
    }

    const turnstile = window.turnstile;

    if (turnstile === undefined) {
      this.handleFailure('Не вдалося ініціалізувати CAPTCHA.');
      return;
    }

    this.widgetId = turnstile.render(this.container.nativeElement, {
      sitekey: environment.turnstileSiteKey,
      action: environment.turnstileAction,
      theme: 'auto',
      callback: (token) => {
        this.errorMessage.set(null);
        this.tokenChange.emit(token);
      },
      'expired-callback': () => {
        this.handleFailure('Термін дії CAPTCHA завершився. Підтвердьте її ще раз.');
      },
      'timeout-callback': () => {
        this.handleFailure('Час проходження CAPTCHA завершився. Спробуйте ще раз.');
      },
      'error-callback': () => {
        this.handleFailure('Не вдалося пройти перевірку CAPTCHA. Спробуйте ще раз.');
        return true;
      },
    });
  }

  private handleFailure(message: string): void {
    this.errorMessage.set(message);
    this.tokenChange.emit(null);
  }
}
