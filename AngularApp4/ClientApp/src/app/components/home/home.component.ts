import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { AdminUser, Appointment } from '../../core/models/admin.model';
import { AdminStateService } from '../../core/services/admin-state.service';

interface DashboardMetric {
  label: string;
  value: string;
  trend: string;
  positive: boolean;
  icon: string;
}

interface ActivityItem {
  actor: string;
  action: string;
  when: string;
  type: 'booking' | 'user' | 'system';
}

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit, OnDestroy {
  users: AdminUser[] = [];
  bookings: Appointment[] = [];

  metrics: DashboardMetric[] = [];
  recentActivities: ActivityItem[] = [];

  private subs: Subscription[] = [];

  constructor(private state: AdminStateService) {}

  ngOnInit(): void {
    this.subs.push(
      this.state.users$.subscribe(users => {
        this.users = users;
        this.refreshDashboard();
      }),
      this.state.bookings$.subscribe(bookings => {
        this.bookings = bookings;
        this.refreshDashboard();
      }),
      this.state.settings$.subscribe(settings => {
        const systemItem: ActivityItem = {
          actor: 'System',
          action: `SMTP host set to ${settings.smtpHost}`,
          when: 'just now',
          type: 'system'
        };
        const base = this.recentActivities.filter(a => a.type !== 'system').slice(0, 3);
        this.recentActivities = [...base, systemItem].slice(0, 4);
      })
    );
  }

  ngOnDestroy(): void {
    this.subs.forEach(s => s.unsubscribe());
  }

  badgeClass(type: ActivityItem['type']): string {
    return `badge ${type}`;
  }

  private refreshDashboard(): void {
    const activeUsers = this.users.filter(u => u.status === 'Active').length;
    const confirmed = this.bookings.filter(b => b.status === 'Confirmed').length;
    const completed = this.bookings.filter(b => b.status === 'Completed').length;
    const cancelled = this.bookings.filter(b => b.status === 'Cancelled').length;
    const revenue = this.bookings.reduce((sum, b) => sum + b.price, 0);

    this.metrics = [
      { label: 'Revenue (MTD)', value: `$${revenue.toFixed(0)}`, trend: '+8.2%', positive: true, icon: 'paid' },
      { label: 'Bookings Completion', value: `${this.percent(completed, this.bookings.length)}%`, trend: '+3.1%', positive: true, icon: 'task_alt' },
      { label: 'Cancellation Rate', value: `${this.percent(cancelled, this.bookings.length)}%`, trend: '-0.5%', positive: true, icon: 'event_busy' },
      { label: 'Active Users', value: `${activeUsers}`, trend: `${confirmed} confirmed`, positive: true, icon: 'groups' }
    ];

    this.recentActivities = [
      ...this.bookings.slice(0, 2).map(b => ({
        actor: b.customerName,
        action: `booking ${b.id} is ${b.status.toLowerCase()}`,
        when: b.time,
        type: 'booking' as const
      })),
      ...this.users.slice(0, 2).map(u => ({
        actor: u.name,
        action: `${u.role.toLowerCase()} account is ${u.status.toLowerCase()}`,
        when: u.lastLogin,
        type: 'user' as const
      }))
    ].slice(0, 4);
  }

  private percent(numerator: number, denominator: number): number {
    if (!denominator) return 0;
    return Math.round((numerator / denominator) * 100);
  }
}
