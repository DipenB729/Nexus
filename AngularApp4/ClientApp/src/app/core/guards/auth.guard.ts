import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { AuthApiService } from '../services/hms/auth-api.service';

@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanActivate {
  constructor(private readonly auth: AuthApiService, private readonly router: Router) {}

  canActivate(): boolean {
    if (!this.auth.isLoggedIn()) {
      this.router.navigate(['/auth/login']);
      return false;
    }
    return true;
  }
}
