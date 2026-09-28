import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';

import { TurnstileApi } from './turnstile.types';

const TURNSTILE_SCRIPT_ID = 'turnstile-script';
const TURNSTILE_SCRIPT_URL =
  'https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit';

@Injectable({ providedIn: 'root' })
export class TurnstileLoaderService {
  private readonly document = inject(DOCUMENT);

  private loadPromise: Promise<TurnstileApi> | null = null;

  load(): Promise<TurnstileApi> {
    const loadedApi = window.turnstile;

    if (loadedApi !== undefined) {
      return Promise.resolve(loadedApi);
    }

    if (this.loadPromise === null) {
      const pendingLoad = this.loadScript();

      this.loadPromise = pendingLoad.catch((error: unknown) => {
        this.loadPromise = null;
        throw error;
      });
    }

    return this.loadPromise;
  }

  private loadScript(): Promise<TurnstileApi> {
    return new Promise<TurnstileApi>((resolve, reject) => {
      const existingElement = this.document.getElementById(TURNSTILE_SCRIPT_ID);
      const existingScript = existingElement instanceof HTMLScriptElement ? existingElement : null;
      const script = existingScript ?? this.document.createElement('script');

      const cleanup = (): void => {
        script.removeEventListener('load', handleLoad);
        script.removeEventListener('error', handleError);
      };

      const handleLoad = (): void => {
        const api = window.turnstile;

        cleanup();

        if (api === undefined) {
          reject(new Error('Turnstile API is unavailable after the script loaded.'));
          return;
        }

        resolve(api);
      };

      const handleError = (): void => {
        cleanup();
        reject(new Error('Turnstile script failed to load.'));
      };

      script.addEventListener('load', handleLoad, { once: true });
      script.addEventListener('error', handleError, { once: true });

      const apiAfterSubscription = window.turnstile;

      if (apiAfterSubscription !== undefined) {
        cleanup();
        resolve(apiAfterSubscription);
        return;
      }

      if (existingScript === null) {
        script.id = TURNSTILE_SCRIPT_ID;
        script.src = TURNSTILE_SCRIPT_URL;
        script.defer = true;
        this.document.head.append(script);
      }
    });
  }
}
