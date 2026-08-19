import {
  DatePipe
} from '@angular/common';

import {
  Component,
  OnDestroy,
  OnInit,
  computed,
  signal
} from '@angular/core';

import {
  Router
} from '@angular/router';

import {
  finalize,
  Subscription
} from 'rxjs';

import {
  ConfirmationService
} from 'primeng/api';

import {
  ButtonModule
} from 'primeng/button';

import {
  ConfirmDialogModule
} from 'primeng/confirmdialog';

import {
  TableModule
} from 'primeng/table';

import {
  TabsModule
} from 'primeng/tabs';

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
    ConfirmDialogModule,
    TableModule,
    TabsModule
  ],

  providers: [
    ConfirmationService
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
      borrow =>
        !borrow.isReturned
    )
  );

  returnedBorrows = computed(() =>
    this.borrows().filter(
      borrow =>
        borrow.isReturned
    )
  );

  loading = signal(false);

  returningBorrowRecordId =
    signal<number | null>(null);

  errorMessage =
    signal('');

  successMessage =
    signal('');

  private borrowsChangedSubscription:
    Subscription | null = null;

  private successMessageTimeout:
    ReturnType<typeof setTimeout> |
    null = null;

  constructor(
    private readonly borrowService:
      BorrowService,

    private readonly authService:
      AuthService,

    private readonly signalRService:
      SignalRService,

    private readonly router:
      Router,

    private readonly confirmationService:
      ConfirmationService
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

    if (
      this.successMessageTimeout !==
      null
    ) {
      clearTimeout(
        this.successMessageTimeout
      );
    }
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
          this.borrows.set(
            borrows
          );
        },

        error: error => {
          if (
            error.status === 403
          ) {
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

  confirmReturnBorrow(
    borrow: AdminBorrow
  ): void {
    this.errorMessage.set('');

    this.confirmationService
      .confirm({
        header:
          'Kitabı İade Al',

        message:
          `"${borrow.bookName}" kitabının ${borrow.userEmail} kullanıcısından teslim alındığını onaylıyor musunuz?`,

        acceptLabel:
          'İade Al',

        rejectLabel:
          'Vazgeç',

        accept: () => {
          this.returnBorrowForAdmin(
            borrow
          );
        }
      });
  }

  private returnBorrowForAdmin(
    borrow: AdminBorrow
  ): void {
    this.errorMessage.set('');

    this.returningBorrowRecordId
      .set(
        borrow.borrowRecordId
      );

    this.borrowService
      .returnBorrowForAdmin(
        borrow.borrowRecordId
      )
      .pipe(
        finalize(() => {
          this.returningBorrowRecordId
            .set(null);
        })
      )
      .subscribe({
        next: result => {
          if (!result.success) {
            this.errorMessage.set(
              result.message
            );

            return;
          }

          this.showSuccessMessage(
            result.message
          );
        },

        error: error => {
          if (
            error.status === 403
          ) {
            this.errorMessage.set(
              'Bu işlem için Admin yetkisi gereklidir.'
            );

            return;
          }

          this.errorMessage.set(
            error.error?.message ??
            'Kitap iade alınırken bir hata oluştu.'
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
      new Date(
        borrow.dueDate
      );

    const difference =
      dueDate.getTime() -
      now.getTime();

    const millisecondsPerDay =
      1000 * 60 * 60 * 24;

    if (difference < 0) {
      const overdueDays =
        Math.ceil(
          Math.abs(
            difference
          ) /
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
      new Date(
        borrow.dueDate
      );

    const returnDate =
      new Date(
        borrow.returnDate
      );

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

  private showSuccessMessage(
    message: string
  ): void {
    if (
      this.successMessageTimeout !==
      null
    ) {
      clearTimeout(
        this.successMessageTimeout
      );
    }

    this.successMessage.set(
      message
    );

    this.successMessageTimeout =
      setTimeout(() => {
        this.successMessage.set('');

        this.successMessageTimeout =
          null;
      }, 3600);
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