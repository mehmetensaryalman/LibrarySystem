import {
  DatePipe
} from '@angular/common';

import {
  HttpErrorResponse
} from '@angular/common/http';

import {
  Component,
  OnDestroy,
  OnInit,
  computed,
  signal
} from '@angular/core';

import {
  FormsModule
} from '@angular/forms';

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
  DialogModule
} from 'primeng/dialog';

import {
  TableModule
} from 'primeng/table';

import {
  TabsModule
} from 'primeng/tabs';

import {
  AdminBorrow,
  AdminBorrowRequest
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

import {
  AdminNotificationsComponent
} from '../../../shared/admin-notifications/admin-notifications.component';

@Component({
  selector: 'app-borrow-management',

  imports: [
    DatePipe,
    FormsModule,
    ButtonModule,
    AdminNotificationsComponent,
    ConfirmDialogModule,
    DialogModule,
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

  borrowRequests =
    signal<AdminBorrowRequest[]>([]);

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

  borrowRequestsLoading =
    signal(false);

  borrowsLoading =
    signal(false);

  processingBorrowRequestId =
    signal<number | null>(null);

  returningBorrowRecordId =
    signal<number | null>(null);

  rejectDialogVisible = false;

  rejectingBorrowRequest:
    AdminBorrowRequest | null = null;

  rejectionReason = '';

  rejectDialogErrorMessage =
    signal('');

  errorMessage =
    signal('');

  successMessage =
    signal('');

  private borrowsChangedSubscription:
    Subscription | null = null;

  private successMessageTimeout:
    ReturnType<typeof setTimeout> |
    null = null;

  private successMessageRestartTimeout:
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
          this.refreshData();
        });

    void this.signalRService
      .startConnection();

    this.refreshData();
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

    if (
      this.successMessageRestartTimeout !==
      null
    ) {
      clearTimeout(
        this.successMessageRestartTimeout
      );
    }
  }

  private refreshData(): void {
    this.loadBorrowRequests();
    this.loadBorrows();
  }

  loadBorrowRequests(): void {
    this.borrowRequestsLoading.set(true);
    this.errorMessage.set('');

    this.borrowService
      .getPendingBorrowRequestsForAdmin()
      .pipe(
        finalize(() => {
          this.borrowRequestsLoading
            .set(false);
        })
      )
      .subscribe({
        next: requests => {
          this.borrowRequests.set(
            requests
          );
        },

        error: error => {
          this.setLoadError(
            error,
            'Bekleyen ödünç talepleri yüklenirken bir hata oluştu.'
          );
        }
      });
  }

  loadBorrows(): void {
    this.borrowsLoading.set(true);
    this.errorMessage.set('');

    this.borrowService
      .getAllBorrowsForAdmin()
      .pipe(
        finalize(() => {
          this.borrowsLoading.set(false);
        })
      )
      .subscribe({
        next: borrows => {
          this.borrows.set(
            borrows
          );
        },

        error: error => {
          this.setLoadError(
            error,
            'Ödünç kayıtları yüklenirken bir hata oluştu.'
          );
        }
      });
  }

  confirmApproveBorrowRequest(
    request: AdminBorrowRequest
  ): void {
    this.errorMessage.set('');

    this.confirmationService
      .confirm({
        header:
          'Ödünç Talebini Onayla',

        message:
          `"${request.bookName}" kitabını ${request.userEmail} kullanıcısına fiziksel olarak teslim ettiğinizi onaylıyor musunuz?`,

        acceptLabel:
          'Onayla',

        rejectLabel:
          'Vazgeç',

        accept: () => {
          this.approveBorrowRequest(
            request
          );
        }
      });
  }

  private approveBorrowRequest(
    request: AdminBorrowRequest
  ): void {
    this.processingBorrowRequestId.set(
      request.borrowRequestId
    );

    this.borrowService
      .approveBorrowRequest(
        request.borrowRequestId
      )
      .pipe(
        finalize(() => {
          this.processingBorrowRequestId
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

          this.removeBorrowRequestLocally(
            request.borrowRequestId
          );

          this.showSuccessMessage(
            result.message
          );

          this.refreshData();
        },

        error: error => {
          this.setOperationError(
            error,
            'Ödünç talebi onaylanırken bir hata oluştu.'
          );
        }
      });
  }

  openRejectDialog(
    request: AdminBorrowRequest
  ): void {
    this.errorMessage.set('');

    this.rejectDialogErrorMessage
      .set('');

    this.rejectingBorrowRequest =
      request;

    this.rejectionReason = '';

    this.rejectDialogVisible =
      true;
  }

  closeRejectDialog(): void {
    if (
      this.processingBorrowRequestId() !==
      null
    ) {
      return;
    }

    this.rejectDialogVisible =
      false;

    this.rejectingBorrowRequest =
      null;

    this.rejectionReason = '';

    this.rejectDialogErrorMessage
      .set('');
  }

  rejectBorrowRequest(): void {
    this.rejectDialogErrorMessage
      .set('');

    const request =
      this.rejectingBorrowRequest;

    if (!request) {
      return;
    }

    const normalizedReason =
      this.rejectionReason.trim();

    if (normalizedReason.length > 500) {
      this.rejectDialogErrorMessage
        .set(
          'Reddetme açıklaması en fazla 500 karakter olabilir.'
        );

      return;
    }

    this.processingBorrowRequestId.set(
      request.borrowRequestId
    );

    this.borrowService
      .rejectBorrowRequest(
        request.borrowRequestId,
        normalizedReason || null
      )
      .pipe(
        finalize(() => {
          this.processingBorrowRequestId
            .set(null);
        })
      )
      .subscribe({
        next: result => {
          if (!result.success) {
            this.rejectDialogErrorMessage
              .set(
                result.message
              );

            return;
          }

          this.removeBorrowRequestLocally(
            request.borrowRequestId
          );

          this.rejectDialogVisible =
            false;

          this.rejectingBorrowRequest =
            null;

          this.rejectionReason = '';

          this.showSuccessMessage(
            result.message
          );

          this.refreshData();
        },

        error: error => {
          if (error.status === 403) {
            this.rejectDialogErrorMessage
              .set(
                'Bu işlem için Admin yetkisi gereklidir.'
              );

            return;
          }

          this.rejectDialogErrorMessage
            .set(
              error.error?.message ??
              'Ödünç talebi reddedilirken bir hata oluştu.'
            );
        }
      });
  }

  confirmReturnBorrow(
    borrow: AdminBorrow
  ): void {
    this.errorMessage.set('');

    if (!borrow.returnRequestedAt) {
      this.errorMessage.set(
        'Bu kitap için kullanıcı tarafından oluşturulmuş bekleyen bir iade talebi bulunmamaktadır.'
      );

      return;
    }

    this.confirmationService
      .confirm({
        header:
          'Fiziksel İadeyi Tamamla',

        message:
          `"${borrow.bookName}" kitabını ${borrow.userEmail} kullanıcısından fiziksel olarak teslim aldığınızı onaylıyor musunuz?`,

        acceptLabel:
          'İadeyi Tamamla',

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

          this.loadBorrows();
        },

        error: error => {
          this.setOperationError(
            error,
            'Kitabın fiziksel iadesi tamamlanırken bir hata oluştu.'
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

  private removeBorrowRequestLocally(
    borrowRequestId: number
  ): void {
    this.borrowRequests.update(
      requests =>
        requests.filter(request =>
          request.borrowRequestId !==
          borrowRequestId
        )
    );
  }

  private setLoadError(
    error: HttpErrorResponse,
    fallbackMessage: string
  ): void {
    if (error.status === 403) {
      this.errorMessage.set(
        'Bu sayfayı görüntülemek için Admin yetkisi gereklidir.'
      );

      return;
    }

    this.errorMessage.set(
      error.error?.message ??
      fallbackMessage
    );
  }

  private setOperationError(
    error: HttpErrorResponse,
    fallbackMessage: string
  ): void {
    if (error.status === 403) {
      this.errorMessage.set(
        'Bu işlem için Admin yetkisi gereklidir.'
      );

      return;
    }

    this.errorMessage.set(
      error.error?.message ??
      fallbackMessage
    );
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

      this.successMessageTimeout =
        null;
    }

    if (
      this.successMessageRestartTimeout !==
      null
    ) {
      clearTimeout(
        this.successMessageRestartTimeout
      );

      this.successMessageRestartTimeout =
        null;
    }

    this.successMessage.set('');

    this.successMessageRestartTimeout =
      setTimeout(() => {
        this.successMessage.set(
          message
        );

        this.successMessageRestartTimeout =
          null;

        this.successMessageTimeout =
          setTimeout(() => {
            this.successMessage.set('');

            this.successMessageTimeout =
              null;
          }, 3600);
      }, 0);
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