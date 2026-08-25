import {
  Component,
  OnDestroy,
  OnInit
} from '@angular/core';

import {
  NavigationEnd,
  Router,
  RouterOutlet
} from '@angular/router';

import {
  filter,
  Subscription,
  switchMap
} from 'rxjs';

import {
  MessageService
} from 'primeng/api';

import {
  ToastModule
} from 'primeng/toast';

import {
  AdminNotification
} from './core/models/notification.models';

import {
  AuthService
} from './core/services/auth.service';

import {
  NotificationService
} from './core/services/notification.service';

import {
  SignalRService
} from './core/services/signalr.service';

@Component({
  selector: 'app-root',

  imports: [
    RouterOutlet,
    ToastModule
  ],

  providers: [
    MessageService
  ],

  templateUrl:
    './app.component.html',

  styleUrl:
    './app.component.scss'
})
export class AppComponent
  implements OnInit, OnDestroy {

  private adminNotificationsChangedSubscription:
    Subscription | null = null;

  private notificationBaselineSubscription:
    Subscription | null = null;

  private routerSubscription:
    Subscription | null = null;

  private logoutSubscription:
    Subscription | null = null;

  private lastKnownAdminNotificationId = 0;

  private adminNotificationToastInitialized =
    false;

  constructor(
    private readonly router:
      Router,

    private readonly authService:
      AuthService,

    private readonly notificationService:
      NotificationService,

    private readonly signalRService:
      SignalRService,

    private readonly messageService:
      MessageService
  ) {
  }

  ngOnInit(): void {
    this.setupAdminNotificationToast();

    this.routerSubscription =
      this.router.events
        .pipe(
          filter(
            (
              event
            ): event is NavigationEnd =>
              event instanceof
              NavigationEnd
          )
        )
        .subscribe(() => {
          this.setupAdminNotificationToast();

          void this.signalRService
            .startConnection();
        });

    this.logoutSubscription =
      this.authService
        .logout$
        .subscribe(() => {
          this.resetAdminNotificationToast();
        });

    void this.signalRService
      .startConnection();
  }

  ngOnDestroy(): void {
    this.adminNotificationsChangedSubscription
      ?.unsubscribe();

    this.notificationBaselineSubscription
      ?.unsubscribe();

    this.routerSubscription
      ?.unsubscribe();

    this.logoutSubscription
      ?.unsubscribe();
  }

  private setupAdminNotificationToast():
    void {

    if (
      !this.authService.isAdmin() ||
      this.adminNotificationToastInitialized ||
      this.notificationBaselineSubscription !==
        null
    ) {
      return;
    }

    this.notificationBaselineSubscription =
      this.notificationService
        .getSummary()
        .subscribe({
          next: summary => {
            const latestNotification =
              this.getLatestNotification(
                summary.notifications
              );

            this.lastKnownAdminNotificationId =
              latestNotification?.id ?? 0;

            this.subscribeToAdminNotificationChanges();

            this.adminNotificationToastInitialized =
              true;
          },

          error: error => {
            console.error(
              'Toast bildirim başlangıç bilgisi alınamadı:',
              error
            );

            this.lastKnownAdminNotificationId =
              0;

            this.subscribeToAdminNotificationChanges();

            this.adminNotificationToastInitialized =
              true;

            this.notificationBaselineSubscription =
              null;
          },

          complete: () => {
            this.notificationBaselineSubscription =
              null;
          }
        });
  }

  private subscribeToAdminNotificationChanges():
    void {

    if (
      this.adminNotificationsChangedSubscription !==
      null
    ) {
      return;
    }

    this.adminNotificationsChangedSubscription =
      this.signalRService
        .adminNotificationsChanged$
        .pipe(
          switchMap(() =>
            this.notificationService
              .getSummary()
          )
        )
        .subscribe({
          next: summary => {
            this.showLatestAdminNotification(
              summary.notifications
            );
          },

          error: error => {
            console.error(
              'Anlık admin bildirimi alınamadı:',
              error
            );

            this.adminNotificationsChangedSubscription =
              null;

            this.adminNotificationToastInitialized =
              false;
          }
        });
  }

  private showLatestAdminNotification(
    notifications:
      AdminNotification[]
  ): void {

    const latestNotification =
      this.getLatestNotification(
        notifications
      );

    if (!latestNotification) {
      return;
    }

    if (
      latestNotification.id <=
      this.lastKnownAdminNotificationId
    ) {
      return;
    }

    this.lastKnownAdminNotificationId =
      latestNotification.id;

    if (
      !this.authService.isAdmin()
    ) {
      return;
    }

    this.messageService.add({
      severity:
        this.getNotificationSeverity(
          latestNotification
        ),

      summary:
        latestNotification.title,

      detail:
        latestNotification.message,

      life: 6000
    });
  }

  private getLatestNotification(
    notifications:
      AdminNotification[]
  ): AdminNotification | null {

    if (notifications.length === 0) {
      return null;
    }

    return notifications.reduce(
      (
        latest,
        current
      ) =>
        current.id > latest.id
          ? current
          : latest
    );
  }

  private getNotificationSeverity(
    notification:
      AdminNotification
  ): 'info' | 'warn' {

    if (
      notification.type ===
      'ReturnRequested'
    ) {
      return 'warn';
    }

    return 'info';
  }

  private resetAdminNotificationToast():
    void {

    this.adminNotificationsChangedSubscription
      ?.unsubscribe();

    this.adminNotificationsChangedSubscription =
      null;

    this.notificationBaselineSubscription
      ?.unsubscribe();

    this.notificationBaselineSubscription =
      null;

    this.adminNotificationToastInitialized =
      false;

    this.lastKnownAdminNotificationId =
      0;

    this.messageService.clear();
  }
}