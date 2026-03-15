import { Component } from '@angular/core';
import { AbstractControl, FormBuilder, ValidationErrors, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthApiService } from '../../../core/services/hms/auth-api.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent {
  errorMessage = '';
  isSubmitting = false;

  readonly form = this.fb.group(
    {
      fullName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      password: [
        '',
        [
          Validators.required,
          Validators.minLength(8),
          Validators.pattern(/^(?=.*[A-Za-z])(?=.*\d).+$/)
        ]
      ],
      confirmPassword: ['', [Validators.required]]
    },
    { validators: RegisterComponent.passwordMatchValidator }
  );

  constructor(
    private readonly fb: FormBuilder,
    private readonly auth: AuthApiService,
    private readonly router: Router
  ) {}

  submit(): void {
    if (this.form.invalid || this.isSubmitting) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage = '';
    this.isSubmitting = true;

    const { fullName, email, password } = this.form.getRawValue();
    this.auth
      .register({
        fullName: fullName!.trim(),
        email: email!.trim().toLowerCase(),
        password: password!,
        role: 'User'
      })
      .subscribe({
        next: () => {
          this.isSubmitting = false;
          this.router.navigate(['/users']);
        },
        error: () => {
          this.isSubmitting = false;
          this.errorMessage = 'Unable to register. Please check your details and try again.';
        }
      });
  }

  private static passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password')?.value;
    const confirmPassword = control.get('confirmPassword')?.value;

    if (!password || !confirmPassword) {
      return null;
    }

    return password === confirmPassword ? null : { passwordMismatch: true };
  }
}
