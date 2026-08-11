import { DatePipe } from '@angular/common';
import {
  Component,
  OnInit,
  computed,
  signal
} from '@angular/core';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { TabsModule } from 'primeng/tabs';
import { BorrowedBook } from '../../core/models/borrow.models';
import { AuthService } from '../../core/services/auth.service';
import { BorrowService } from '../../core/services/borrow.service';

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
export class MyBooksComponent implements OnInit {
  borrowedBooks = signal<BorrowedBook[]>([]);

  activeBorrowedBooks = computed(() =>
    this.borrowedBooks().filter(book => !book.isReturned)
  );

  returnedBooks = computed(() =>
    this.borrowedBooks().filter(book => book.isReturned)
  );

  loading = signal(false);
  returningBookId = signal<number | null>(null);

  errorMessage = signal('');
  successMessage = signal('');

  constructor(
    private readonly borrowService: BorrowService,
    private readonly authService: AuthService,
    private readonly router: Router
  ) {
  }

  ngOnInit(): void {
    this.loadMyBooks();
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
          this.borrowedBooks.set(books);
        },
        error: error => {
          this.errorMessage.set(
            error.error?.message ??
            'Ödünç aldığınız kitaplar yüklenirken bir hata oluştu.'
          );
        }
      });
  }

  getRemainingTime(book: BorrowedBook): string {
    if (book.isReturned) {
      return '-';
    }

    const now = new Date();
    const dueDate = new Date(book.dueDate);

    const difference =
      dueDate.getTime() - now.getTime();

    if (difference <= 0) {
      return 'Süresi doldu';
    }

    const millisecondsPerDay =
      1000 * 60 * 60 * 24;

    const remainingDays =
      Math.ceil(difference / millisecondsPerDay);

    return `${remainingDays} gün kaldı`;
  }

  returnBook(book: BorrowedBook): void {
    this.errorMessage.set('');
    this.successMessage.set('');

    this.returningBookId.set(book.bookId);

    this.borrowService
      .returnBook(book.bookId)
      .pipe(
        finalize(() => {
          this.returningBookId.set(null);
        })
      )
      .subscribe({
        next: result => {
          if (!result.success) {
            this.errorMessage.set(result.message);
            return;
          }

          this.successMessage.set(result.message);
          this.loadMyBooks();
        },
        error: error => {
          this.errorMessage.set(
            error.error?.message ??
            'Kitap iade edilirken bir hata oluştu.'
          );
        }
      });
  }

  goToBooks(): void {
    this.router.navigate(['/books']);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}