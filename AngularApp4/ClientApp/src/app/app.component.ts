import { Component } from '@angular/core';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  // Fixes TS2339
  isSidebarExpanded: boolean = true;

  toggleSidebar() {
    this.isSidebarExpanded = !this.isSidebarExpanded;
  }
}
