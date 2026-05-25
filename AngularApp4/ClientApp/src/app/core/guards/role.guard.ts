import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot } from '@angular/router';
import { catchError, map, Observable, of } from 'rxjs';
import { AuthApiService } from '../services/hms/auth-api.service';
import { AdminOpsService } from '../services/hms/admin-ops.service';

@Injectable({ providedIn: 'root' })
export class RoleGuard implements CanActivate {
  constructor(
    private readonly auth: AuthApiService,
    private readonly router: Router,
    private readonly adminOps: AdminOpsService
  ) {}

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean | Observable<boolean> {
    const expectedRole = route.data['role'] as string;
    const role = this.auth.getRole();

    if (!role) {
      this.router.navigate(['/auth/login']);
      return false;
    }

    if (expectedRole === 'Admin' && this.hasAdminWorkspaceAccess(role)) {
      if (role === 'Admin' || state.url.startsWith('/admin/dashboard')) {
        return true;
      }

      return this.adminOps.getAccessProfile().pipe(
        map((profile) => {
          const hasPageAccess = profile.permissions.some((permission) =>
            permission.canAccessPage &&
            (state.url.startsWith(permission.pageRoute) || permission.pageRoute.startsWith(state.url)));

          if (!hasPageAccess) {
            this.router.navigateByUrl('/admin/dashboard');
          }

          return hasPageAccess;
        }),
        catchError(() => {
          this.router.navigateByUrl('/auth/login');
          return of(false);
        })
      );
    }

    if (role !== expectedRole) {
      this.router.navigateByUrl(this.auth.getDashboardRoute(role));
      return false;
    }

    return true;
  }

  private hasAdminWorkspaceAccess(role: string): boolean {
    return role !== 'SuperAdmin' && role !== 'Doctor' && role !== 'User';
  }
}
