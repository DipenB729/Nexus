import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { AdminSettings, AdminUser, Appointment } from '../models/admin.model';

@Injectable({ providedIn: 'root' })
export class AdminStateService {
  private readonly USERS_KEY = 'nexus_admin_users';
  private readonly SETTINGS_KEY = 'nexus_admin_settings';

  private defaultUsers: AdminUser[] = [
    { id: 'USR-1001', name: 'Dipen Patel', email: 'dipen@company.com', role: 'Admin', status: 'Active', lastLogin: 'Today, 10:14 AM' },
    { id: 'USR-1021', name: 'Nina Shah', email: 'nina@company.com', role: 'Manager', status: 'Active', lastLogin: 'Today, 08:52 AM' },
    { id: 'USR-1132', name: 'Rahul Desai', email: 'rahul@company.com', role: 'Staff', status: 'Invited', lastLogin: 'Never' },
    { id: 'USR-1160', name: 'Pooja Mehta', email: 'pooja@company.com', role: 'Staff', status: 'Suspended', lastLogin: '2 days ago' }
  ];

  private defaultSettings: AdminSettings = {
    smtpHost: 'smtp.office365.com',
    smtpPort: 587,
    senderEmail: 'noreply@company.com',
    senderName: 'Nexus Admin',
    enableTls: true,
    bookingNotifications: true,
    reminderNotifications: true
  };

  private bookings: Appointment[] = [
    { id: 'BK-9921', customerName: 'John Doe', serviceName: 'Initial Consultation', appointmentDate: new Date('2026-04-10'), time: '09:00 AM', status: 'Confirmed', price: 75 },
    { id: 'BK-9925', customerName: 'Sarah Smith', serviceName: 'Deep Tissue Massage', appointmentDate: new Date('2026-04-12'), time: '02:30 PM', status: 'Pending', price: 120 },
    { id: 'BK-9930', customerName: 'Mike Ross', serviceName: 'Wellness Coaching', appointmentDate: new Date('2026-04-15'), time: '11:00 AM', status: 'Cancelled', price: 90 },
    { id: 'BK-9935', customerName: 'Rachel Zane', serviceName: 'Express Checkup', appointmentDate: new Date('2026-03-28'), time: '04:00 PM', status: 'Completed', price: 30 }
  ];

  readonly users$ = new BehaviorSubject<AdminUser[]>(this.loadUsers());
  readonly settings$ = new BehaviorSubject<AdminSettings>(this.loadSettings());
  readonly bookings$ = new BehaviorSubject<Appointment[]>(this.bookings);

  addUser(payload: Pick<AdminUser, 'name' | 'email' | 'role'>): void {
    const users = this.users$.value;
    const id = `USR-${Math.floor(1000 + Math.random() * 9000)}`;
    const newUser: AdminUser = {
      id,
      name: payload.name,
      email: payload.email,
      role: payload.role,
      status: 'Invited',
      lastLogin: 'Never'
    };

    const updated = [newUser, ...users];
    this.users$.next(updated);
    this.save(this.USERS_KEY, updated);
  }

  updateSettings(settings: AdminSettings): void {
    this.settings$.next(settings);
    this.save(this.SETTINGS_KEY, settings);
  }

  private loadUsers(): AdminUser[] {
    const raw = this.read(this.USERS_KEY);
    if (!raw) return this.defaultUsers;

    try {
      return JSON.parse(raw) as AdminUser[];
    } catch {
      return this.defaultUsers;
    }
  }

  private loadSettings(): AdminSettings {
    const raw = this.read(this.SETTINGS_KEY);
    if (!raw) return this.defaultSettings;

    try {
      return JSON.parse(raw) as AdminSettings;
    } catch {
      return this.defaultSettings;
    }
  }

  private read(key: string): string | null {
    return typeof localStorage === 'undefined' ? null : localStorage.getItem(key);
  }

  private save(key: string, value: unknown): void {
    if (typeof localStorage === 'undefined') return;
    localStorage.setItem(key, JSON.stringify(value));
  }
}

