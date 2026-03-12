import { Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { AuthApiService } from './core/services/hms/auth-api.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit, OnDestroy {
  isSidebarExpanded = true;
  isAuthRoute = false;
  isAdminRoute = false;
  isUserDashboardRoute = false;

  private routeSub?: Subscription;

  constructor(private readonly router: Router, private readonly auth: AuthApiService) {}

  ngOnInit(): void {
    this.updateLayoutFlags(this.router.url);

    this.routeSub = this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => this.updateLayoutFlags(event.urlAfterRedirects));
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
  }

  toggleSidebar(): void {
    this.isSidebarExpanded = !this.isSidebarExpanded;
  }

  logout(): void {
    this.auth.logout();
  }

  private updateLayoutFlags(url: string): void {
    this.isAuthRoute = url.startsWith('/auth');
    this.isAdminRoute = url.startsWith('/settings') || url.startsWith('/admin');
    this.isUserDashboardRoute = url.startsWith('/user') || url.startsWith('/users');
  }
}
