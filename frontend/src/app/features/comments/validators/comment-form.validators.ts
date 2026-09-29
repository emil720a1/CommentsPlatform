import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

const EMAIL_WITH_DOMAIN_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export const emailValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value = control.value;

  if (typeof value !== 'string' || value.trim().length === 0) {
    return null;
  }

  return EMAIL_WITH_DOMAIN_PATTERN.test(value.trim()) ? null : { email: true };
};

export const notBlankValidator: ValidatorFn = (
  control: AbstractControl,
): ValidationErrors | null => {
  const value = control.value;

  if (typeof value !== 'string' || value.length === 0) {
    return null;
  }

  return value.trim().length === 0 ? { whitespace: true } : null;
};

export const optionalHttpUrlValidator: ValidatorFn = (
  control: AbstractControl,
): ValidationErrors | null => {
  const value = control.value;

  if (typeof value !== 'string' || value.trim().length === 0) {
    return null;
  }

  try {
    const url = new URL(value);

    return url.protocol === 'http:' || url.protocol === 'https:' ? null : { httpUrl: true };
  } catch {
    return { httpUrl: true };
  }
};
