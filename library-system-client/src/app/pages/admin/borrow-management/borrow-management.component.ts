import { DatePipe } from '@angular/common';

import {
  Component,
  OnDestroy,
  OnInit,
  computed,
  signal
} from '@angular/core';

import { Router } from '@angular/router';

import {
  finalize,
  Subscription
} from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { TabsModule } from 'primeng/tabs';

import {
  AdminBorrow
} from '../../../core/models/borrow.models';

import {
  AuthService
} from '../../../core/services/auth.service';

import {
  BorrowService
} from '../../../core/services/borrow.service';

import {
  SignalRService
} from '../../../core/services/signalr.service';

@Component({
  selector: 'app-borrow-management',

  imports: [
    DatePipe,
    ButtonModule,
    TableModule,
    TabsModule
  ],

  templateUrl:
    './borrow-management.component.html',

  styleUrl:
    './borrow-management.component.scss'
})
export class BorrowManagementComponent
  implements OnInit, OnDestroy {

  borrows =
    signal<AdminBorrow[]>([]);

  activeBorrows = computed(() =>
    this.borrows().filter(
      borrow => !borrow.isReturned
    )
  );

  returnedBorrows = computed(() =>
    this.borrows().filter(
      borrow => borrow.isReturned
    )
  );

  loading = signal(false);

  errorMessage = signal('');

  private borrowsChangedSubscription:
    Subscription | null = null;

  constructor(
    private readonly borrowService:
      BorrowService,

    private readonly authService:
      AuthService,

    private readonly signalRService:
      SignalRService,

    private readonly router:
      Router
  ) {
  }

  ngOnInit(): void {
    this.borrowsChangedSubscription =
      this.signalRService
        .borrowsChanged$
        .subscribe(() => {
          this.loadBorrows();
        });

    void this.signalRService
      .startConnection();

    this.loadBorrows();
  }

  ngOnDestroy(): void {
    this.borrowsChangedSubscription
      ?.unsubscribe();
  }

  loadBorrows(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.borrowService
      .getAllBorrowsForAdmin()
      .pipe(
        finalize(() => {
          this.loading.set(false);
        })
      )
      .subscribe({
        next: borrows => {
          this.borrows.set(borrows);
        },

        error: error => {
          if (error.status === 403) {
            this.errorMessage.set(
              'Bu sayfayı görüntülemek için Admin yetkisi gereklidir.'
            );
            return;
          }

          this.errorMessage.set(
            error.error?.message ??
            'Ödünç kayıtları yüklenirken bir hata oluştu.'
          );
        }
      });
  }

  getRemainingTime(
    borrow: AdminBorrow
  ): string {
    if (borrow.isReturned) {
      return '-';
    }

    const now =
      new Date();

    const dueDate =
      new Date(borrow.dueDate);

    const difference =
      dueDate.getTime() -
      now.getTime();

    const millisecondsPerDay =
      1000 * 60 * 60 * 24;

    if (difference < 0) {
      const overdueDays =
        Math.ceil(
          Math.abs(difference) /
          millisecondsPerDay
        );

      return `${overdueDays} gün gecikti`;
    }

    const remainingDays =
      Math.ceil(
        difference /
        millisecondsPerDay
      );

    return `${remainingDays} gün kaldı`;
  }

  getDeliveryStatus(
    borrow: AdminBorrow
  ): string {
    if (!borrow.returnDate) {
      return '-';
    }

    const dueDate =
      new Date(borrow.dueDate);

    const returnDate =
      new Date(borrow.returnDate);

    if (
      returnDate.getTime() <=
      dueDate.getTime()
    ) {
      return 'Zamanında';
    }

    const millisecondsPerDay =
      1000 * 60 * 60 * 24;

    const delay =
      returnDate.getTime() -
      dueDate.getTime();

    const delayedDays =
      Math.ceil(
        delay /
        millisecondsPerDay
      );

    return `${delayedDays} gün gecikmeli`;
  }

  goToAdminPanel(): void {
    this.router.navigate([
      '/admin'
    ]);
  }

  goToBooks(): void {
    this.router.navigate([
      '/books'
    ]);
  }

  logout(): void {
    this.authService.logout();

    this.router.navigate([
      '/login'
    ]);
  }
}