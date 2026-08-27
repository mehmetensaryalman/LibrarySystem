import {
  Component,
  DestroyRef,
  HostListener,
  OnInit,
  computed,
  signal
} from '@angular/core';

import {
  takeUntilDestroyed
} from '@angular/core/rxjs-interop';

import {
  AdminNotification,
  NotificationSummary
} from '../../core/models/notification.models';

import {
  NotificationService
} from '../../core/services/notification.service';

import {
  SignalRService
} from '../../core/services/signalr.service';

@Component({
  selector: 'app-admin-notifications',

  imports: [],

  templateUrl:
    './admin-notifications.component.html',

  styleUrl:
    './admin-notifications.component.scss'
})
export class AdminNotificationsComponent
  implements OnInit {

  readonly panelOpen =
    signal(false);

  readonly loading =
    signal(false);

  readonly deletingReadNotifications =
    signal(false);

  readonly loadError =
    signal('');

  readonly summary =
    signal<NotificationSummary>({
      unreadCount: 0,
      notifications: []
    });

  readonly unreadCount =
    computed(() =>
      this.summary().unreadCount
    );

  readonly notifications =
    computed(() =>
      this.summary().notifications
    );

  readonly hasNotifications =
    computed(() =>
      this.notifications().length > 0
    );

  readonly badgeText =
    computed(() => {
      const count =
        this.unreadCount();

      return count > 99
        ? '99+'
        : count.toString();
    });

  constructor(
    private readonly notificationService:
      NotificationService,

    private readonly signalRService:
      SignalRService,

    private readonly destroyRef:
      DestroyRef
  ) {
  }

  ngOnInit(): void {
    this.loadNotifications();

    this.signalRService
      .adminNotificationsChanged$
      .pipe(
        takeUntilDestroyed(
          this.destroyRef
        )
      )
      .subscribe(() => {
        this.loadNotifications();
      });
  }

  @HostListener(
    'document:keydown.escape'
  )
  onEscapePressed(): void {
    if (this.panelOpen()) {
      this.closePanel();
    }
  }

  togglePanel(): void {
    this.panelOpen.update(
      value => !value
    );
  }

  closePanel(): void {
    this.panelOpen.set(false);
  }

  markAsRead(
    notification:
      AdminNotification
  ): void {

    if (
      notification.isRead ||
      this.deletingReadNotifications()
    ) {
      return;
    }

    this.notificationService
      .markAsRead(
        notification.id
      )
      .pipe(
        takeUntilDestroyed(
          this.destroyRef
        )
      )
      .subscribe({
        next: () => {
          this.loadNotifications();
        },

        error: () => {
          this.loadError.set(
            'Bildirim güncellenemedi.'
          );
        }
      });
  }

  markAllAsRead(): void {
    if (
      this.unreadCount() === 0 ||
      this.deletingReadNotifications()
    ) {
      return;
    }

    this.notificationService
      .markAllAsRead()
      .pipe(
        takeUntilDestroyed(
          this.destroyRef
        )
      )
      .subscribe({
        next: () => {
          this.loadNotifications();
        },

        error: () => {
          this.loadError.set(
            'Bildirimler güncellenemedi.'
          );
        }
      });
  }

  deleteReadNotifications():
    void {

    if (
      !this.hasNotifications() ||
      this.deletingReadNotifications()
    ) {
      return;
    }

    this.deletingReadNotifications
      .set(true);

    this.loadError.set('');

    this.notificationService
      .deleteReadNotifications()
      .pipe(
        takeUntilDestroyed(
          this.destroyRef
        )
      )
      .subscribe({
        next: () => {
          this.deletingReadNotifications
            .set(false);

          this.loadNotifications();
        },

        error: () => {
          this.deletingReadNotifications
            .set(false);

          this.loadError.set(
            'Okunmuş bildirimler silinemedi.'
          );
        }
      });
  }

  formatNotificationTime(
    value: string
  ): string {

    const date =
      new Date(value);

    const now =
      new Date();

    const differenceMs =
      Math.max(
        0,
        now.getTime() -
          date.getTime()
      );

    const differenceMinutes =
      Math.floor(
        differenceMs / 60000
      );

    if (
      differenceMinutes < 1
    ) {
      return 'Az önce';
    }

    if (
      differenceMinutes < 60
    ) {
      return `${differenceMinutes} dk önce`;
    }

    const differenceHours =
      Math.floor(
        differenceMinutes / 60
      );

    if (
      differenceHours < 24
    ) {
      return `${differenceHours} sa önce`;
    }

    const differenceDays =
      Math.floor(
        differenceHours / 24
      );

    if (
      differenceDays === 1
    ) {
      return `Dün ${this.formatTime(date)}`;
    }

    if (
      differenceDays < 7
    ) {
      return `${differenceDays} gün önce`;
    }

    return new Intl.DateTimeFormat(
      'tr-TR',
      {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
      }
    ).format(date);
  }

  private loadNotifications():
    void {

    this.loading.set(true);

    this.loadError.set('');

    this.notificationService
      .getSummary()
      .pipe(
        takeUntilDestroyed(
          this.destroyRef
        )
      )
      .subscribe({
        next: summary => {
          this.summary.set(
            summary
          );

          this.loading.set(false);
        },

        error: () => {
          this.loadError.set(
            'Bildirimler yüklenemedi.'
          );

          this.loading.set(false);
        }
      });
  }

  private formatTime(
    date: Date
  ): string {

    return new Intl.DateTimeFormat(
      'tr-TR',
      {
        hour: '2-digit',
        minute: '2-digit'
      }
    ).format(date);
  }
}