import { Component } from '@angular/core';
import { AbstractControl, FormBuilder, ValidationErrors, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthApiService } from '../../../core/services/hms/auth-api.service';

type ForgotPasswordStep = 'request' | 'reset' | 'done';

@Component({
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.scss']
})
export class ForgotPasswordComponent {
  step: ForgotPasswordStep = 'request';
  isSubmitting = false;
  errorMessage = '';
  statusMessage = '';
  resetCodePreview: string | null = null;
  expiresAtLabel = '';

  readonly requestForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  readonly resetForm = this.fb.group(
    {
      email: ['', [Validators.required, Validators.email]],
      resetCode: ['', [Validators.required, Validators.minLength(6)]],
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]]
    },
    { validators: ForgotPasswordComponent.passwordMatchValidator }
  );

  constructor(
    private readonly fb: FormBuilder,
    private readonly auth: AuthApiService,
    private readonly router: Router
  ) {}

  submitRequest(): void {
    if (this.requestForm.invalid || this.isSubmitting) {
      this.requestForm.markAllAsTouched();
      return;
    }

    this.errorMessage = '';
    this.statusMessage = '';
    this.isSubmitting = true;

    const email = this.requestForm.getRawValue().email!;
    this.auth.requestPasswordReset({ email }).subscribe({
      next: (response) => {
        this.isSubmitting = false;
        this.step = 'reset';
        this.resetCodePreview = response.resetCodePreview ?? null;
        this.expiresAtLabel = response.expiresAt ? new Date(response.expiresAt).toLocaleString() : '';
        this.resetForm.patchValue({
          email,
          resetCode: response.resetCodePreview ?? ''
        });
        this.statusMessage = this.resetCodePreview
          ? 'Use the reset code below to create a new password.'
          : 'If the account exists, reset instructions are ready.';
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSubmitting = false;
        this.errorMessage = error?.error?.message || 'Unable to start password reset.';
      }
    });
  }

  submitReset(): void {
    if (this.resetForm.invalid || this.isSubmitting) {
      this.resetForm.markAllAsTouched();
      return;
    }

    this.errorMessage = '';
    this.statusMessage = '';
    this.isSubmitting = true;

    const { email, resetCode, newPassword } = this.resetForm.getRawValue();
    this.auth
      .resetPassword({
        email: email!,
        resetCode: resetCode!,
        newPassword: newPassword!
      })
      .subscribe({
        next: () => {
          this.isSubmitting = false;
          this.step = 'done';
        },
        error: (error: { error?: { message?: string } }) => {
          this.isSubmitting = false;
          this.errorMessage = error?.error?.message || 'Unable to reset password.';
        }
      });
  }

  backToRequest(): void {
    if (this.isSubmitting) {
      return;
    }

    this.step = 'request';
    this.errorMessage = '';
    this.statusMessage = '';
  }

  goToLogin(): void {
    this.router.navigate(['/auth/login']);
  }

  private static passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('newPassword')?.value;
    const confirmPassword = control.get('confirmPassword')?.value;

    if (!password || !confirmPassword) {
      return null;
    }

    return password === confirmPassword ? null : { passwordMismatch: true };
  }
}
