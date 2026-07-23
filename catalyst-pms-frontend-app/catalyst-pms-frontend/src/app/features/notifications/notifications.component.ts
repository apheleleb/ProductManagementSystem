import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { NotificationService } from '../../core/services/notification.service';
import { AppNotification } from '../../core/models/notification.model';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, MatIconModule, MatButtonModule, MatProgressSpinnerModule],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.scss'
})
export class NotificationsComponent implements OnInit {
  readonly loading = signal(true);
  readonly notifications = signal<AppNotification[]>([]);

  constructor(private notificationService: NotificationService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.notificationService.getMine().subscribe({
      next: (res) => {
        this.notifications.set(res.data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  markAsRead(notification: AppNotification): void {
    if (notification.isRead) return;
    this.notificationService.markAsRead(notification.notificationId).subscribe({
      next: () => {
        this.notifications.update((list) =>
          list.map((n) => (n.notificationId === notification.notificationId ? { ...n, isRead: true } : n))
        );
      },
      error: () => {}
    });
  }
}
