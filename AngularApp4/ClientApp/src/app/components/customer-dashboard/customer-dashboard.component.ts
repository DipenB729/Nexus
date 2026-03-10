import { Component, OnInit } from '@angular/core';
import { AuthStateService } from '../../core/services/auth-state.service';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-customer-dashboard',
  templateUrl: './customer-dashboard.component.html',
  styleUrls: ['./customer-dashboard.component.scss']
})
export class CustomerDashboardComponent implements OnInit {
  fullName = 'Customer';
  upcoming = 0;

  constructor(private auth: AuthStateService, private api: ApiService) {}

  ngOnInit(): void {
    this.auth.user$.subscribe(u => this.fullName = u?.fullName || 'Customer');
    this.api.getMyBookings().subscribe(items => this.upcoming = items.filter(i => i.status !== 'Cancelled').length);
  }
}
