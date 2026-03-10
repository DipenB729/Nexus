import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { AdminSettings } from '../../core/models/admin.model';
import { AdminStateService } from '../../core/services/admin-state.service';

@Component({
  selector: 'app-admin-settings',
  templateUrl: './admin-settings.component.html',
  styleUrls: ['./admin-settings.component.scss']
})
export class AdminSettingsComponent implements OnInit, OnDestroy {
  form: AdminSettings = {
    smtpHost: '',
    smtpPort: 587,
    senderEmail: '',
    senderName: '',
    enableTls: true,
    bookingNotifications: true,
    reminderNotifications: true
  };

  saveMessage = '';
  private sub?: Subscription;

  constructor(private state: AdminStateService) {}

  ngOnInit(): void {
    this.sub = this.state.settings$.subscribe(settings => {
      this.form = { ...settings };
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  saveSettings(): void {
    this.state.updateSettings({ ...this.form });
    this.saveMessage = 'Settings saved successfully.';
    setTimeout(() => (this.saveMessage = ''), 2500);
  }
}
