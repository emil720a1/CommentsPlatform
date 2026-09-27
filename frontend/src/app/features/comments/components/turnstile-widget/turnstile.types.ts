export interface TurnstileRenderOptions {
  sitekey: string;
  action?: string;
  theme?: 'auto' | 'light' | 'dark';
  callback: (token: string) => void;
  'error-callback'?: (errorCode: string) => boolean | void;
  'expired-callback'?: () => void;
  'timeout-callback'?: () => void;
}

export interface TurnstileApi {
  render(container: string | HTMLElement, options: TurnstileRenderOptions): string;

  reset(widgetId?: string): void;

  remove(widgetId: string): void;
}

declare global {
  interface Window {
    turnstile?: TurnstileApi;
  }
}
