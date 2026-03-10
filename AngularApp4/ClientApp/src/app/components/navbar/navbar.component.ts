import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { AuthStateService } from '../../core/services/auth-state.service';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent implements OnInit {
  @Input() isExpanded: boolean = true;
  @Output() toggleEvent = new EventEmitter<void>();

  role: 'Admin' | 'Customer' = 'Customer';

  constructor(private auth: AuthStateService) {}

  ngOnInit(): void {
    this.auth.user$.subscribe(u => {
      this.role = (u?.role as 'Admin' | 'Customer') || 'Customer';
    });
  }

  toggle() {
    this.toggleEvent.emit();
  }
}
