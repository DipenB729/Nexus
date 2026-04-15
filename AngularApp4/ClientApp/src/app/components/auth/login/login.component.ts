import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthApiService } from '../../../core/services/hms/auth-api.service';

interface DemoAccount {
  label: string;
  email: string;
  password: string;
  role: 'Admin' | 'User' | 'Doctor';
}

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {
  errorMessage = '';
  isSubmitting = false;

  readonly demoAccounts: DemoAccount[] = [
    { label: 'Admin Demo', email: 'admin@nexus.local', password: 'Admin@123', role: 'Admin' },
    { label: 'Doctor Demo', email: 'aryan.shah@nexushospital.local', password: 'Doctor@123', role: 'Doctor' },
    { label: 'Patient Demo', email: 'mira.patient@nexus.local', password: 'User@123', role: 'User' }
  ];

  readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly auth: AuthApiService,
    private readonly router: Router
  ) {}

  fillDemo(account: DemoAccount): void {
    this.form.patchValue({
      email: account.email,
      password: account.password
    });
  }

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
        this.router.navigateByUrl(this.auth.getDashboardRoute(res.role));
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSubmitting = false;
        this.errorMessage = error?.error?.message || 'Invalid email or password.';
      }
    });
  }
}
