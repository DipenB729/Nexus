import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription, interval } from 'rxjs';
import { AppNotification } from '../../core/models/hms/auth.model';
import { NotificationsService } from '../../core/services/notifications.service';

type NotificationFilter = 'all' | 'unread';

@Component({
  selector: 'app-notification-center',
  templateUrl: './notification-center.component.html',
  styleUrls: ['./notification-center.component.scss']
})
export class NotificationCenterComponent implements OnInit, OnDestroy {
  notifications: AppNotification[] = [];
  filter: NotificationFilter = 'all';
  isLoading = true;
  errorMessage = '';

  private notificationsSub?: Subscription;
  private refreshSub?: Subscription;
  private pollingSub?: Subscription;

  constructor(
    private readonly notificationsApi: NotificationsService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.refreshSub = this.notificationsApi.refresh$.subscribe(() => {
      this.loadNotifications();
    });

    this.pollingSub = interval(30000).subscribe(() => {
      this.loadNotifications();
    });

    this.loadNotifications();
  }

  ngOnDestroy(): void {
    this.notificationsSub?.unsubscribe();
    this.refreshSub?.unsubscribe();
    this.pollingSub?.unsubscribe();
  }

  get unreadCount(): number {
    return this.notifications.filter((item) => !item.isRead).length;
  }

  get visibleNotifications(): AppNotification[] {
    return this.filter === 'unread'
      ? this.notifications.filter((item) => !item.isRead)
      : this.notifications;
  }

  setFilter(filter: NotificationFilter): void {
    this.filter = filter;
  }

  openNotification(notification: AppNotification): void {
    const navigate = () => {
      if (notification.actionUrl) {
        void this.router.navigateByUrl(notification.actionUrl);
      }
    };

    if (notification.isRead) {
      navigate();
      return;
    }

    this.notificationsApi.markAsRead(notification.appNotificationId).subscribe({
      next: () => {
        this.notifications = this.notifications.map((item) =>
          item.appNotificationId === notification.appNotificationId ? { ...item, isRead: true } : item);
        navigate();
      }
    });
  }

  markAllAsRead(): void {
    if (!this.unreadCount) {
      return;
    }

    this.notificationsApi.markAllAsRead().subscribe({
      next: () => {
        this.notifications = this.notifications.map((item) => ({ ...item, isRead: true }));
      }
    });
  }

  notificationStatusClass(notification: AppNotification): string {
    return notification.isRead ? 'read' : 'unread';
  }

  private loadNotifications(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.notificationsSub?.unsubscribe();
    this.notificationsSub = this.notificationsApi.getNotifications(50).subscribe({
      next: (items) => {
        this.notifications = items;
        this.isLoading = false;
      },
      error: () => {
        this.notifications = [];
        this.isLoading = false;
        this.errorMessage = 'Unable to load notifications right now.';
      }
    });
  }
}
