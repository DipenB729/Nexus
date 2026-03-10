import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthUser } from '../../../core/models/api.model';
import { ApiService } from '../../../core/services/api.service';
import { AuthStateService } from '../../../core/services/auth-state.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {
  email = '';
  password = '';
  error = '';

  constructor(private api: ApiService, private auth: AuthStateService, private router: Router) {}

  submit(): void {
    this.error = '';
    this.api.login({ email: this.email, password: this.password }).subscribe({
      next: res => {
        const user = res.user as AuthUser;
        this.auth.setUser(user);
        this.router.navigateByUrl(user.role === 'Admin' ? '/' : '/customer');
      },
      error: () => {
        this.error = 'Invalid email or password.';
      }
    });
  }
}
