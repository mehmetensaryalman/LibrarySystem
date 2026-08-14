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

import { BorrowedBook } from '../../core/models/borrow.models';
import { AuthService } from '../../core/services/auth.service';
import { BorrowService } from '../../core/services/borrow.service';
import { SignalRService } from '../../core/services/signalr.service';

@Component({
  selector: 'app-my-books',
  imports: [
    DatePipe,
    ButtonModule,
    TableModule,
    TabsModule
  ],
  templateUrl: './my-books.component.html',
  styleUrl: './my-books.component.scss'
})
export class MyBooksComponent
  implements OnInit, OnDestroy {

  borrowedBooks =
    signal<BorrowedBook[]>([]);

  activeBorrowedBooks = computed(() =>
    this.borrowedBooks()
      .filter(book => !book.isReturned)
  );

  returnedBooks = computed(() =>
    this.borrowedBooks()
      .filter(book => book.isReturned)
  );

  loading = signal(false);

  returningBookId =
    signal<number | null>(null);

  errorMessage = signal('');
  successMessage = signal('');

  private borrowsChangedSubscription:
    Subscription | null = null;

  private successMessageTimeout:
    ReturnType<typeof setTimeout> | null = null;

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
          this.loadMyBooks();
        });

    void this.signalRService
      .startConnection();

    this.loadMyBooks();
  }

  ngOnDestroy(): void {
    this.borrowsChangedSubscription
      ?.unsubscribe();

    if (this.successMessageTimeout !== null) {
      clearTimeout(
        this.successMessageTimeout
      );
    }
  }

  loadMyBooks(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.borrowService
      .getMyBooks()
      .pipe(
        finalize(() => {
          this.loading.set(false);
        })
      )
      .subscribe({
        next: books => {
          this.borrowedBooks.set(
            books
          );
        },

        error: error => {
          this.errorMessage.set(
            error.error?.message ??
            'Ödünç aldığınız kitaplar yüklenirken bir hata oluştu.'
          );
        }
      });
  }

  getRemainingTime(
    book: BorrowedBook
  ): string {
    if (book.isReturned) {
      return '-';
    }

    const now =
      new Date();

    const dueDate =
      new Date(book.dueDate);

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
    book: BorrowedBook
  ): string {
    if (!book.returnDate) {
      return 'Bilgi Yok';
    }

    const dueDate =
      new Date(book.dueDate);

    const returnDate =
      new Date(book.returnDate);

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

  returnBook(
    book: BorrowedBook
  ): void {
    this.errorMessage.set('');

    this.returningBookId.set(
      book.bookId
    );

    this.borrowService
      .returnBook(book.bookId)
      .pipe(
        finalize(() => {
          this.returningBookId.set(
            null
          );
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
          this.errorMessage.set(
            error.error?.message ??
            'Kitap iade edilirken bir hata oluştu.'
          );
        }
      });
  }

  private showSuccessMessage(
    message: string
  ): void {
    if (this.successMessageTimeout !== null) {
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