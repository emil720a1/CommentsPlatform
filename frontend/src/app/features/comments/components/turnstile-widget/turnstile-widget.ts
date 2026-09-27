import {
  AfterViewInit,
  Component,
  ElementRef,
  inject,
  OnDestroy,
  output,
  signal,
  ViewChild,
} from '@angular/core';

import { environment } from '../../../../../environments/environment';
import { TurnstileLoaderService } from './turnstile-loader.service';
import { TurnstileApi } from './turnstile.types';

@Component({
  selector: 'app-turnstile-widget',
  imports: [],
  templateUrl: './turnstile-widget.html',
  styleUrl: './turnstile-widget.scss',
})
export class TurnstileWidget implements AfterViewInit, OnDestroy {
  private readonly turnstileLoader = inject(TurnstileLoaderService);

  @ViewChild('container', { static: true })
  private container!: ElementRef<HTMLDivElement>;

  readonly tokenChange = output<string | null>();

  protected readonly errorMessage = signal<string | null>(null);

  private widgetId: string | null = null;

  private turnstileApi: TurnstileApi | null = null;

  private isDestroyed = false;

  ngAfterViewInit(): void {
    void this.turnstileLoader
      .load()
      .then((turnstileApi) => {
        if (!this.isDestroyed) {
          this.renderWidget(turnstileApi);
        }
      })
      .catch(() => {
        if (!this.isDestroyed) {
          this.handleFailure(
            'Не вдалося завантажити CAPTCHA. Оновіть сторінку та спробуйте ще раз.',
          );
        }
      });
  }

  ngOnDestroy(): void {
    this.isDestroyed = true;

    if (this.widgetId !== null) {
      this.turnstileApi?.remove(this.widgetId);
    }

    this.widgetId = null;
    this.turnstileApi = null;
  }

  reset(): void {
    if (this.widgetId !== null) {
      this.turnstileApi?.reset(this.widgetId);
    }

    this.errorMessage.set(null);
    this.tokenChange.emit(null);
  }

  private renderWidget(turnstileApi: TurnstileApi): void {
    if (this.isDestroyed || this.widgetId !== null) {
      return;
    }

    this.turnstileApi = turnstileApi;
    this.widgetId = turnstileApi.render(this.container.nativeElement, {
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
