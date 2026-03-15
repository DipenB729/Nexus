import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthApiService } from '../../../core/services/hms/auth-api.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {
  errorMessage = '';
  isSubmitting = false;

  readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

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

    this.auth.login(this.form.getRawValue() as { email: string; password: string }).subscribe({
      next: (res) => {
        this.isSubmitting = false;
        this.router.navigate([res.role === 'Admin' ? '/settings' : '/users']);
      },
      error: () => {
        this.isSubmitting = false;
        this.errorMessage = 'Invalid email or password.';
      }
    });
  }
}
