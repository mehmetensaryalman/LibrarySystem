import { Component, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { finalize, timeout } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { Book } from '../../../core/models/book.models';
import { AuthService } from '../../../core/services/auth.service';
import { BookService } from '../../../core/services/book.service';
import { BorrowService } from '../../../core/services/borrow.service';

@Component({
  selector: 'app-book-list',
  imports: [
    ButtonModule,
    TableModule
  ],
  templateUrl: './book-list.component.html',
  styleUrl: './book-list.component.scss'
})
export class BookListComponent implements OnInit {
  books = signal<Book[]>([]);
  loading = signal(false);

  borrowingBookId = signal<number | null>(null);

  errorMessage = signal('');
  successMessage = signal('');

  constructor(
    private readonly bookService: BookService,
    private readonly borrowService: BorrowService,
    private readonly authService: AuthService,
    private readonly router: Router
  ) {
  }

  ngOnInit(): void {
    this.loadBooks();
  }

  loadBooks(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.bookService
      .getAll()
      .pipe(
        timeout(10000),
        finalize(() => {
          this.loading.set(false);
        })
      )
      .subscribe({
        next: books => {
          this.books.set(books);
        },
        error: error => {
          console.error(
            'Kitap listesi alınamadı:',
            error
          );

          if (error.name === 'TimeoutError') {
            this.errorMessage.set(
              'Kitap listesi 10 saniye içinde yüklenemedi.'
            );
            return;
          }

          if (error.status) {
            this.errorMessage.set(
              `Kitaplar yüklenirken hata oluştu. HTTP ${error.status}`
            );
            return;
          }

          this.errorMessage.set(
            'Kitaplar yüklenirken bir bağlantı hatası oluştu.'
          );
        }
      });
  }

  borrowBook(book: Book): void {
    this.errorMessage.set('');
    this.successMessage.set('');

    if (book.stock <= 0) {
      this.errorMessage.set(
        'Bu kitap stokta bulunmuyor.'
      );
      return;
    }

    this.borrowingBookId.set(book.id);

    this.borrowService
      .borrow(book.id)
      .pipe(
        finalize(() => {
          this.borrowingBookId.set(null);
        })
      )
      .subscribe({
        next: result => {
          if (!result.success) {
            this.errorMessage.set(result.message);
            return;
          }

          this.successMessage.set(result.message);

          this.loadBooks();
        },
        error: error => {
          this.errorMessage.set(
            error.error?.message ??
            'Kitap ödünç alınırken bir hata oluştu.'
          );
        }
      });
  }

  goToMyBooks(): void {
    this.router.navigate(['/my-books']);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}