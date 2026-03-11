import { Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Subscription, filter } from 'rxjs';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit, OnDestroy {
  isSidebarExpanded = true;
  isUserDashboardRoute = false;
  isAuthRoute = false;

  private routeSub?: Subscription;

  constructor(private router: Router) {}

  ngOnInit(): void {
    this.updateRouteState(this.router.url);

    this.routeSub = this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(event => {
        this.updateRouteState(event.urlAfterRedirects);
      });
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
  }

  private updateRouteState(url: string): void {
    this.isUserDashboardRoute = url.startsWith('/user');
    this.isAuthRoute = url.startsWith('/login') || url.startsWith('/register');
  }

  toggleSidebar(): void {
    this.isSidebarExpanded = !this.isSidebarExpanded;
  }
}
