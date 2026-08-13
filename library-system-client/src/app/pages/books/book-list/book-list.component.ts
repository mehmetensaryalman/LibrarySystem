import {
  Component,
  OnInit,
  signal
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  finalize,
  timeout
} from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';

import { Book } from '../../../core/models/book.models';
import { AuthService } from '../../../core/services/auth.service';
import { BookService } from '../../../core/services/book.service';
import { BorrowService } from '../../../core/services/borrow.service';

@Component({
  selector: 'app-book-list',
  imports: [
    FormsModule,
    ButtonModule,
    ConfirmDialogModule,
    DialogModule,
    InputNumberModule,
    InputTextModule,
    TableModule
  ],
  providers: [
    ConfirmationService
  ],
  templateUrl: './book-list.component.html',
  styleUrl: './book-list.component.scss'
})
export class BookListComponent implements OnInit {
  books = signal<Book[]>([]);
  loading = signal(false);

  borrowingBookId =
    signal<number | null>(null);

  deletingBookId =
    signal<number | null>(null);

  isAdmin = signal(false);

  errorMessage = signal('');
  successMessage = signal('');

  createDialogVisible = false;
  creatingBook = false;
  createDialogErrorMessage = '';

  newBookName = '';
  newBookAuthor = '';
  newBookStock: number | null = 0;

  editDialogVisible = false;
  updatingBook = false;
  editDialogErrorMessage = '';

  editingBookId: number | null = null;
  editBookName = '';
  editBookAuthor = '';
  editBookStock: number | null = 0;

  private readonly authorNamePattern =
    /^(?=.*\p{L})[\p{L}\p{M}.'’\- ]+$/u;

  constructor(
    private readonly bookService: BookService,
    private readonly borrowService: BorrowService,
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly confirmationService:
      ConfirmationService
  ) {
  }

  ngOnInit(): void {
    this.isAdmin.set(
      this.authService.isAdmin()
    );

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
            this.errorMessage.set(
              result.message
            );
            return;
          }

          this.successMessage.set(
            result.message
          );

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

  openEditDialog(book: Book): void {
    this.errorMessage.set('');
    this.successMessage.set('');
    this.editDialogErrorMessage = '';

    this.editingBookId = book.id;
    this.editBookName = book.name;
    this.editBookAuthor = book.author;
    this.editBookStock = book.stock;

    this.editDialogVisible = true;
  }

  closeEditDialog(): void {
    this.editDialogVisible = false;
    this.editDialogErrorMessage = '';

    this.editingBookId = null;
    this.editBookName = '';
    this.editBookAuthor = '';
    this.editBookStock = 0;
  }

  updateBook(): void {
    this.editDialogErrorMessage = '';

    if (this.editingBookId === null) {
      return;
    }

    const name =
      this.editBookName.trim();

    const author =
      this.editBookAuthor.trim();

    const stock =
      this.editBookStock;

    if (!name) {
      this.editDialogErrorMessage =
        'Kitap adı zorunludur.';
      return;
    }

    if (!author) {
      this.editDialogErrorMessage =
        'Yazar adı zorunludur.';
      return;
    }

    if (!this.isValidAuthorName(author)) {
      this.editDialogErrorMessage =
        'Yazar adı sayı veya geçersiz karakter içeremez.';
      return;
    }

    if (stock === null) {
      this.editDialogErrorMessage =
        'Stok zorunludur.';
      return;
    }

    if (!Number.isInteger(stock)) {
      this.editDialogErrorMessage =
        'Stok tam sayı olmalıdır.';
      return;
    }

    if (
      stock < 0 ||
      stock > 1000
    ) {
      this.editDialogErrorMessage =
        'Stok 0 ile 1000 arasında olmalıdır.';
      return;
    }

    this.updatingBook = true;

    this.bookService
      .update(
        this.editingBookId,
        {
          name,
          author,
          stock
        }
      )
      .pipe(
        finalize(() => {
          this.updatingBook = false;
        })
      )
      .subscribe({
        next: () => {
          this.editDialogVisible = false;
          this.editDialogErrorMessage = '';

          this.successMessage.set(
            'Kitap başarıyla güncellendi.'
          );

          this.editingBookId = null;

          this.loadBooks();
        },

        error: error => {
          if (error.status === 403) {
            this.editDialogErrorMessage =
              'Bu işlem için Admin yetkisi gereklidir.';
            return;
          }

          if (error.status === 404) {
            this.editDialogErrorMessage =
              error.error?.message ??
              'Kitap bulunamadı.';
            return;
          }

          this.editDialogErrorMessage =
            error.error?.message ??
            'Kitap güncellenirken bir hata oluştu.';
        }
      });
  }

  openCreateDialog(): void {
    this.errorMessage.set('');
    this.successMessage.set('');
    this.createDialogErrorMessage = '';

    this.newBookName = '';
    this.newBookAuthor = '';
    this.newBookStock = 0;

    this.createDialogVisible = true;
  }

  closeCreateDialog(): void {
    this.createDialogVisible = false;
    this.createDialogErrorMessage = '';

    this.newBookName = '';
    this.newBookAuthor = '';
    this.newBookStock = 0;
  }

  createBook(): void {
    this.createDialogErrorMessage = '';

    const name =
      this.newBookName.trim();

    const author =
      this.newBookAuthor.trim();

    const stock =
      this.newBookStock;

    if (!name) {
      this.createDialogErrorMessage =
        'Kitap adı zorunludur.';
      return;
    }

    if (!author) {
      this.createDialogErrorMessage =
        'Yazar adı zorunludur.';
      return;
    }

    if (!this.isValidAuthorName(author)) {
      this.createDialogErrorMessage =
        'Yazar adı sayı veya geçersiz karakter içeremez.';
      return;
    }

    if (stock === null) {
      this.createDialogErrorMessage =
        'Stok zorunludur.';
      return;
    }

    if (!Number.isInteger(stock)) {
      this.createDialogErrorMessage =
        'Stok tam sayı olmalıdır.';
      return;
    }

    if (
      stock < 0 ||
      stock > 1000
    ) {
      this.createDialogErrorMessage =
        'Stok 0 ile 1000 arasında olmalıdır.';
      return;
    }

    this.creatingBook = true;

    this.bookService
      .create({
        name,
        author,
        stock
      })
      .pipe(
        finalize(() => {
          this.creatingBook = false;
        })
      )
      .subscribe({
        next: book => {
          this.createDialogVisible = false;
          this.createDialogErrorMessage = '';

          this.successMessage.set(
            `"${book.name}" başarıyla eklendi.`
          );

          this.loadBooks();
        },

        error: error => {
          if (error.status === 403) {
            this.createDialogErrorMessage =
              'Bu işlem için Admin yetkisi gereklidir.';
            return;
          }

          this.createDialogErrorMessage =
            error.error?.message ??
            'Kitap eklenirken bir hata oluştu.';
        }
      });
  }

  private isValidAuthorName(
    author: string
  ): boolean {
    return this.authorNamePattern.test(
      author
    );
  }

  confirmDelete(book: Book): void {
    this.confirmationService.confirm({
      header: 'Kitabı Kaldır',

      message:
        `"${book.name}" kitabını katalogdan kaldırmak istediğinize emin misiniz?`,

      acceptLabel: 'Evet',
      rejectLabel: 'Vazgeç',

      accept: () => {
        this.deleteBook(book);
      }
    });
  }

  private deleteBook(book: Book): void {
    this.errorMessage.set('');
    this.successMessage.set('');

    this.deletingBookId.set(book.id);

    this.bookService
      .delete(book.id)
      .pipe(
        finalize(() => {
          this.deletingBookId.set(null);
        })
      )
      .subscribe({
        next: result => {
          this.successMessage.set(
            result.message
          );

          this.loadBooks();
        },

        error: error => {
          if (error.status === 403) {
            this.errorMessage.set(
              'Bu işlem için Admin yetkisi gereklidir.'
            );
            return;
          }

          if (error.status === 409) {
            this.errorMessage.set(
              error.error?.message ??
              'Bu kitap şu anda kaldırılamaz.'
            );
            return;
          }

          this.errorMessage.set(
            error.error?.message ??
            'Kitap kaldırılırken bir hata oluştu.'
          );
        }
      });
  }

  goToAdminPanel(): void {
    this.router.navigate([
      '/admin'
    ]);
  }

  goToMyBooks(): void {
    this.router.navigate([
      '/my-books'
    ]);
  }

  logout(): void {
    this.authService.logout();

    this.router.navigate([
      '/login'
    ]);
  }
}