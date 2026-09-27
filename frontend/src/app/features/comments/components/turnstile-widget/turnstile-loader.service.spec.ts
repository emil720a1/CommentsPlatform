import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import { TurnstileLoaderService } from './turnstile-loader.service';
import { TurnstileApi } from './turnstile.types';

describe('TurnstileLoaderService', () => {
  let service: TurnstileLoaderService;

  beforeEach(() => {
    window.turnstile = undefined;
    document.getElementById('turnstile-script')?.remove();

    TestBed.configureTestingModule({});
    service = TestBed.inject(TurnstileLoaderService);
  });

  afterEach(() => {
    window.turnstile = undefined;
    document.getElementById('turnstile-script')?.remove();
  });

  it('shares one script load between multiple callers', async () => {
    const firstLoad = service.load();
    const secondLoad = service.load();
    const script = document.getElementById('turnstile-script');
    const turnstileApi: TurnstileApi = {
      render: vi.fn(() => 'widget-id'),
      reset: vi.fn(),
      remove: vi.fn(),
    };

    expect(script).toBeInstanceOf(HTMLScriptElement);
    expect(document.querySelectorAll('#turnstile-script')).toHaveLength(1);
    expect(firstLoad).toBe(secondLoad);

    window.turnstile = turnstileApi;
    script?.dispatchEvent(new Event('load'));

    await expect(firstLoad).resolves.toBe(turnstileApi);
    await expect(secondLoad).resolves.toBe(turnstileApi);
  });
});
