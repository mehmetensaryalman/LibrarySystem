import {
  Component,
  OnInit,
  signal
} from '@angular/core';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';

import {
  ArchivedBook
} from '../../../core/models/book.models';

import {
  AuthService
} from '../../../core/services/auth.service';

import {
  BookService
} from '../../../core/services/book.service';

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
  implements OnInit {

  archivedBooks =
    signal<ArchivedBook[]>([]);

  loading = signal(false);

  restoringBookId =
    signal<number | null>(null);

  errorMessage = signal('');
  successMessage = signal('');

  constructor(
    private readonly bookService:
      BookService,

    private readonly authService:
      AuthService,

    private readonly router:
      Router
  ) {
  }

  ngOnInit(): void {
    this.loadArchivedBooks();
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
          this.archivedBooks.set(books);
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
            'Arşivlenmiş kitaplar yüklenirken bir hata oluştu.'
          );
        }
      });
  }

  restoreBook(
    book: ArchivedBook
  ): void {
    this.errorMessage.set('');
    this.successMessage.set('');

    this.restoringBookId.set(
      book.id
    );

    this.bookService
      .restore(book.id)
      .pipe(
        finalize(() => {
          this.restoringBookId.set(null);
        })
      )
      .subscribe({
        next: result => {
          this.successMessage.set(
            result.message
          );

          this.loadArchivedBooks();
        },

        error: error => {
          if (error.status === 403) {
            this.errorMessage.set(
              'Bu işlem için Admin yetkisi gereklidir.'
            );
            return;
          }

          if (error.status === 404) {
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