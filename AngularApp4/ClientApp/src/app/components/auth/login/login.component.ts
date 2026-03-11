import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthApiService } from '../../../core/services/auth-api.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {
  email = '';
  password = '';
  loading = false;
  error = '';

  constructor(private authApi: AuthApiService, private router: Router) {}

  submit(): void {
    this.error = '';
    this.loading = true;

    this.authApi.login({ email: this.email, password: this.password }).subscribe({
      next: user => {
        localStorage.setItem('nexus_auth_user', JSON.stringify(user));
        this.loading = false;
        this.router.navigateByUrl(user.role === 'Admin' ? '/' : '/user');
      },
      error: err => {
        this.loading = false;
        this.error = err?.error || 'Unable to login. Please try again.';
      }
    });
  }
}
