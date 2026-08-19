import {
  Component,
  OnDestroy,
  OnInit,
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
  ButtonModule
} from 'primeng/button';

import {
  TableModule
} from 'primeng/table';

import {
  ArchivedBook
} from '../../../core/models/book.models';

import {
  AuthService
} from '../../../core/services/auth.service';

import {
  BookService
} from '../../../core/services/book.service';

import {
  SignalRService
} from '../../../core/services/signalr.service';

@Component({
  selector: 'app-archived-books',

  imports: [
    ButtonModule,
    TableModule
  ],

  templateUrl:
    './archived-books.component.html',

  styleUrl:
    './archived-books.component.scss'
})
export class ArchivedBooksComponent
  implements OnInit, OnDestroy {

  archivedBooks =
    signal<ArchivedBook[]>([]);

  loading = signal(false);

  restoringBookId =
    signal<number | null>(null);

  errorMessage = signal('');
  successMessage = signal('');

  private booksChangedSubscription:
    Subscription | null = null;

  private successMessageTimeout:
    ReturnType<typeof setTimeout> |
    null = null;

  constructor(
    private readonly bookService:
      BookService,

    private readonly authService:
      AuthService,

    private readonly signalRService:
      SignalRService,

    private readonly router:
      Router
  ) {
  }

  ngOnInit(): void {
    this.booksChangedSubscription =
      this.signalRService
        .booksChanged$
        .subscribe(() => {
          this.loadArchivedBooks();
        });

    void this.signalRService
      .startConnection();

    this.loadArchivedBooks();
  }

  ngOnDestroy(): void {
    this.booksChangedSubscription
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

  loadArchivedBooks(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.bookService
      .getArchived()
      .pipe(
        finalize(() => {
          this.loading.set(false);
        })
      )
      .subscribe({
        next: books => {
          this.archivedBooks.set(
            books
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
            'Arşivlenmiş kitaplar yüklenirken bir hata oluştu.'
          );
        }
      });
  }

  restoreBook(
    book: ArchivedBook
  ): void {
    this.errorMessage.set('');

    this.restoringBookId.set(
      book.id
    );

    this.bookService
      .restore(book.id)
      .pipe(
        finalize(() => {
          this.restoringBookId.set(
            null
          );
        })
      )
      .subscribe({
        next: result => {
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

          if (
            error.status === 404
          ) {
            this.errorMessage.set(
              error.error?.message ??
              'Arşivlenmiş kitap bulunamadı.'
            );

            return;
          }

          this.errorMessage.set(
            error.error?.message ??
            'Kitap arşivden geri alınırken bir hata oluştu.'
          );
        }
      });
  }

  formatArchivedAt(
    archivedAt: string | null
  ): string {
    if (!archivedAt) {
      return '-';
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
    ).format(
      new Date(archivedAt)
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