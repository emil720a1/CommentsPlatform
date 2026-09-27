import { ComponentFixture, TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import { environment } from '../../../../../environments/environment';
import { TurnstileApi, TurnstileRenderOptions } from './turnstile.types';
import { TurnstileWidget } from './turnstile-widget';

describe('TurnstileWidget', () => {
  let component: TurnstileWidget;
  let fixture: ComponentFixture<TurnstileWidget>;
  let renderOptions: TurnstileRenderOptions;
  let turnstile: TurnstileApi;

  beforeEach(async () => {
    turnstile = {
      render: vi.fn((_container, options) => {
        renderOptions = options;
        return 'widget-id';
      }),
      reset: vi.fn(),
      remove: vi.fn(),
    };

    window.turnstile = turnstile;

    await TestBed.configureTestingModule({
      imports: [TurnstileWidget],
    }).compileComponents();

    fixture = TestBed.createComponent(TurnstileWidget);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    fixture.destroy();
    window.turnstile = undefined;
  });

  it('renders the widget with the configured site key and action', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    expect(turnstile.render).toHaveBeenCalledOnce();
    expect(renderOptions.sitekey).toBe(environment.turnstileSiteKey);
    expect(renderOptions.action).toBe(environment.turnstileAction);
  });

  it('emits the token and can reset the widget', async () => {
    const tokenChange = vi.fn();
    component.tokenChange.subscribe(tokenChange);

    fixture.detectChanges();
    await fixture.whenStable();
    renderOptions.callback('captcha-token');
    component.reset();

    expect(tokenChange).toHaveBeenNthCalledWith(1, 'captcha-token');
    expect(tokenChange).toHaveBeenNthCalledWith(2, null);
    expect(turnstile.reset).toHaveBeenCalledWith('widget-id');
  });
});
