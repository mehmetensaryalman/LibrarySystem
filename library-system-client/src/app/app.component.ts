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
  Subscription
} from 'rxjs';

import {
  MessageService
} from 'primeng/api';

import {
  ToastModule
} from 'primeng/toast';

import {
  AdminBorrowNotification
} from './core/models/borrow.models';

import {
  AuthService
} from './core/services/auth.service';

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

  private adminBorrowNotificationSubscription:
    Subscription | null = null;

  private routerSubscription:
    Subscription | null = null;

  constructor(
    private readonly router:
      Router,

    private readonly authService:
      AuthService,

    private readonly signalRService:
      SignalRService,

    private readonly messageService:
      MessageService
  ) {
  }

  ngOnInit(): void {
    this.adminBorrowNotificationSubscription =
      this.signalRService
        .adminBorrowNotification$
        .subscribe(notification => {
          this.showAdminBorrowNotification(
            notification
          );
        });

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
          void this.signalRService
            .startConnection();
        });

    void this.signalRService
      .startConnection();
  }

  ngOnDestroy(): void {
    this.adminBorrowNotificationSubscription
      ?.unsubscribe();

    this.routerSubscription
      ?.unsubscribe();
  }

  private showAdminBorrowNotification(
    notification:
      AdminBorrowNotification
  ): void {
    if (
      !this.authService
        .isAdmin()
    ) {
      return;
    }

    const formattedDate =
      new Intl.DateTimeFormat(
        'tr-TR',
        {
          day: '2-digit',
          month: '2-digit',
          year: 'numeric',
          hour: '2-digit',
          minute: '2-digit'
        }
      )
        .format(
          new Date(
            notification.borrowDate
          )
        );

    this.messageService.add({
      severity: 'info',

      summary:
        'Yeni Ödünç İşlemi',

      detail:
        `"${notification.bookName}" (ID: ${notification.bookId}), ${notification.userEmail} tarafından ödünç alındı. ${formattedDate}`,

      life: 6000
    });
  }
}