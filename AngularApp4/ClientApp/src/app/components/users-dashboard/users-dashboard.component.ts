import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { AdminUser, UserRole } from '../../core/models/admin.model';
import { AdminStateService } from '../../core/services/admin-state.service';

@Component({
  selector: 'app-users-dashboard',
  templateUrl: './users-dashboard.component.html',
  styleUrls: ['./users-dashboard.component.scss']
})
export class UsersDashboardComponent implements OnInit, OnDestroy {
  search = '';
  users: AdminUser[] = [];
  roleOptions: UserRole[] = ['Admin', 'Manager', 'Staff'];

  draftName = '';
  draftEmail = '';
  draftRole: UserRole = 'Staff';

  private sub?: Subscription;

  constructor(private state: AdminStateService) {}

  ngOnInit(): void {
    this.sub = this.state.users$.subscribe(users => (this.users = users));
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  addUser(): void {
    const name = this.draftName.trim();
    const email = this.draftEmail.trim().toLowerCase();
    if (!name || !email) return;

    this.state.addUser({ name, email, role: this.draftRole });
    this.draftName = '';
    this.draftEmail = '';
    this.draftRole = 'Staff';
  }

  get activeCount(): number {
    return this.users.filter(u => u.status === 'Active').length;
  }

  get invitedCount(): number {
    return this.users.filter(u => u.status === 'Invited').length;
  }

  get filteredUsers(): AdminUser[] {
    const q = this.search.trim().toLowerCase();
    if (!q) return this.users;

    return this.users.filter(u =>
      u.name.toLowerCase().includes(q) ||
      u.email.toLowerCase().includes(q) ||
      u.role.toLowerCase().includes(q) ||
      u.status.toLowerCase().includes(q)
    );
  }

  statusClass(status: AdminUser['status']): string {
    return status.toLowerCase();
  }
}
