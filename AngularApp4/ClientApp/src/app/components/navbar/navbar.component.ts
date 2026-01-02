import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-navbar', // Ensure this matches what you use in app.component.html
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent {
  // Fixes NG8002
  @Input() isExpanded: boolean = true;
  @Output() toggleEvent = new EventEmitter<void>();

  toggle() {
    this.toggleEvent.emit();
  }
}
