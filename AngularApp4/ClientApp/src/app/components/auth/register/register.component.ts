import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AccountRole } from '../../../core/models/auth.model';
import { AuthApiService } from '../../../core/services/auth-api.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent {
  fullName = '';
  email = '';
  password = '';
  role: AccountRole = 'User';
  loading = false;
  error = '';

  constructor(private authApi: AuthApiService, private router: Router) {}

  submit(): void {
    this.error = '';
    this.loading = true;

    this.authApi.register({
      fullName: this.fullName,
      email: this.email,
      password: this.password,
      role: this.role
    }).subscribe({
      next: user => {
        localStorage.setItem('nexus_auth_user', JSON.stringify(user));
        this.loading = false;
        this.router.navigateByUrl(user.role === 'Admin' ? '/' : '/user');
      },
      error: err => {
        this.loading = false;
        this.error = err?.error || 'Unable to register. Please try again.';
      }
    });
  }
}
