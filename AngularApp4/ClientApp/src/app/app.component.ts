import { Component, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  isSidebarExpanded: boolean = true;
  isAuthRoute = false;

  constructor(private router: Router) {}

  ngOnInit(): void {
    this.router.events.subscribe(evt => {
      if (evt instanceof NavigationEnd) {
        this.isAuthRoute = evt.urlAfterRedirects.startsWith('/login') || evt.urlAfterRedirects.startsWith('/register');
      }
    });
  }

  toggleSidebar(): void {
    this.isSidebarExpanded = !this.isSidebarExpanded;
  }
}
