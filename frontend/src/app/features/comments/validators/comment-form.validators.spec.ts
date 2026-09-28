import { FormControl } from '@angular/forms';

import { notBlankValidator, optionalHttpUrlValidator } from './comment-form.validators';

describe('notBlankValidator', () => {
  it('passes for a non-empty string', () => {
    expect(notBlankValidator(new FormControl('Hello'))).toBeNull();
  });

  it('passes for an empty string because it is intended to combine with required', () => {
    expect(notBlankValidator(new FormControl(''))).toBeNull();
  });

  it('passes for a non-string value', () => {
    expect(notBlankValidator(new FormControl(42))).toBeNull();
  });

  it('rejects a whitespace-only string', () => {
    expect(notBlankValidator(new FormControl('   '))).toEqual({ whitespace: true });
  });

  it('rejects a string with only newlines and tabs', () => {
    expect(notBlankValidator(new FormControl('\n\t'))).toEqual({ whitespace: true });
  });
});

describe('optionalHttpUrlValidator', () => {
  it('passes when the value is empty', () => {
    expect(optionalHttpUrlValidator(new FormControl(''))).toBeNull();
  });

  it('passes when the value is whitespace only', () => {
    expect(optionalHttpUrlValidator(new FormControl('   '))).toBeNull();
  });

  it('passes for an http URL', () => {
    expect(optionalHttpUrlValidator(new FormControl('http://example.com'))).toBeNull();
  });

  it('passes for an https URL', () => {
    expect(optionalHttpUrlValidator(new FormControl('https://example.com/path'))).toBeNull();
  });

  it('rejects a javascript URL', () => {
    expect(optionalHttpUrlValidator(new FormControl('javascript:alert(1)'))).toEqual({
      httpUrl: true,
    });
  });

  it('rejects an ftp URL', () => {
    expect(optionalHttpUrlValidator(new FormControl('ftp://files.example.com'))).toEqual({
      httpUrl: true,
    });
  });

  it('rejects an invalid URL', () => {
    expect(optionalHttpUrlValidator(new FormControl('not a url'))).toEqual({ httpUrl: true });
  });

  it('passes for a non-string value', () => {
    expect(optionalHttpUrlValidator(new FormControl(null))).toBeNull();
  });
});
