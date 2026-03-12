import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthApiService } from '../../../core/services/hms/auth-api.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent {
  errorMessage = '';

  readonly form = this.fb.group({
    fullName: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    role: ['User', [Validators.required]]
  });

  constructor(private readonly fb: FormBuilder, private readonly auth: AuthApiService, private readonly router: Router) {}

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.auth.register(this.form.getRawValue() as { fullName: string; email: string; password: string; role: 'Admin' | 'User' }).subscribe({
      next: () => this.router.navigate(['/']),
      error: () => (this.errorMessage = 'Unable to register. Please check input.')
    });
  }
}
