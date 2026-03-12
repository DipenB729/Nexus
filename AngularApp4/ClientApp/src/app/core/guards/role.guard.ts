import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router } from '@angular/router';
import { AuthApiService } from '../services/hms/auth-api.service';

@Injectable({ providedIn: 'root' })
export class RoleGuard implements CanActivate {
  constructor(private readonly auth: AuthApiService, private readonly router: Router) {}

  canActivate(route: ActivatedRouteSnapshot): boolean {
    const expectedRole = route.data['role'] as string;
    const role = this.auth.getRole();

    if (role !== expectedRole) {
      this.router.navigate(['/']);
      return false;
    }

    return true;
  }
}
