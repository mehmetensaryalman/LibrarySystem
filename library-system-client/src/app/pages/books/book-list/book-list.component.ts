import {
  Component,
  OnDestroy,
  OnInit,
  signal
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import {
  debounceTime,
  distinctUntilChanged,
  finalize,
  Subject,
  Subscription,
  timeout
} from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { PaginatorModule } from 'primeng/paginator';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';

import {
  Book
} from '../../../core/models/book.models';

import {
  AuthService
} from '../../../core/services/auth.service';

import {
  BookService
} from '../../../core/services/book.service';

import {
  BorrowService
} from '../../../core/services/borrow.service';

import {
  SignalRService
} from '../../../core/services/signalr.service';

type StockFilterValue =
  'all' |
  'inStock' |
  'outOfStock';

@Component({
  selector: 'app-book-list',

  imports: [
    FormsModule,
    ButtonModule,
    ConfirmDialogModule,
    DialogModule,
    InputNumberModule,
    InputTextModule,
    PaginatorModule,
    SelectModule,
    TableModule
  ],

  providers: [
    ConfirmationService
  ],

  templateUrl:
    './book-list.component.html',

  styleUrl:
    './book-list.component.scss'
})
export class BookListComponent
  implements OnInit, OnDestroy {

  books = signal<Book[]>([]);
  loading = signal(false);

  borrowingBookId =
    signal<number | null>(null);

  deletingBookId =
    signal<number | null>(null);

  isAdmin = signal(false);

  errorMessage = signal('');
  successMessage = signal('');

  searchText = '';

  stockFilter:
    StockFilterValue = 'all';

  readonly stockFilterOptions: Array<{
    label: string;
    value: StockFilterValue;
  }> = [
    {
      label: 'Tüm Kitaplar',
      value: 'all'
    },
    {
      label: 'Stokta Olanlar',
      value: 'inStock'
    },
    {
      label: 'Stoğu Tükenenler',
      value: 'outOfStock'
    }
  ];

  pageNumber = signal(1);
  pageSize = signal(5);

  first = signal(0);

  totalCount = signal(0);
  totalPages = signal(0);

  readonly rowsPerPageOptions = [
    5,
    10
  ];

  createDialogVisible = false;
  creatingBook = false;
  createDialogErrorMessage = '';

  newBookName = '';
  newBookAuthor = '';
  newBookStock:
    number | null = 0;

  editDialogVisible = false;
  updatingBook = false;
  editDialogErrorMessage = '';

  editingBookId:
    number | null = null;

  editBookName = '';
  editBookAuthor = '';

  editBookStock:
    number | null = 0;

  private appliedSearch = '';

  private readonly searchChanges =
    new Subject<string>();

  private booksChangedSubscription:
    Subscription | null = null;

  private searchSubscription:
    Subscription | null = null;

  private successMessageTimeout:
    ReturnType<typeof setTimeout> |
    null = null;

  private readonly authorNamePattern =
    /^(?=.*\p{L})[\p{L}\p{M}.'’\- ]+$/u;

  constructor(
    private readonly bookService:
      BookService,

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
    this.isAdmin.set(
      this.authService.isAdmin()
    );

    this.searchSubscription =
      this.searchChanges
        .pipe(
          debounceTime(400),
          distinctUntilChanged()
        )
        .subscribe(value => {
          this.applySearch(value);
        });

    this.booksChangedSubscription =
      this.signalRService
        .booksChanged$
        .subscribe(() => {
          this.loadBooks();
        });

    void this.signalRService
      .startConnection();

    this.loadBooks();
  }

  ngOnDestroy(): void {
    this.booksChangedSubscription
      ?.unsubscribe();

    this.searchSubscription
      ?.unsubscribe();

    this.searchChanges.complete();

    if (
      this.successMessageTimeout !==
      null
    ) {
      clearTimeout(
        this.successMessageTimeout
      );
    }
  }

  loadBooks(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.bookService
      .getPaged({
        search:
          this.appliedSearch ||
          undefined,

        inStock:
          this.getInStockFilter(),

        pageNumber:
          this.pageNumber(),

        pageSize:
          this.pageSize()
      })
      .pipe(
        timeout(10000),

        finalize(() => {
          this.loading.set(false);
        })
      )
      .subscribe({
        next: result => {
          this.totalCount.set(
            result.totalCount
          );

          this.totalPages.set(
            result.totalPages
          );

          if (
            result.totalCount === 0
          ) {
            this.books.set([]);

            this.pageNumber.set(1);
            this.first.set(0);

            return;
          }

          if (
            result.items.length === 0 &&
            this.pageNumber() >
              result.totalPages
          ) {
            const lastPage =
              Math.max(
                result.totalPages,
                1
              );

            this.pageNumber.set(
              lastPage
            );

            this.first.set(
              (lastPage - 1) *
              this.pageSize()
            );

            this.loadBooks();

            return;
          }

          this.books.set(
            result.items
          );

          this.pageNumber.set(
            result.pageNumber
          );

          this.pageSize.set(
            result.pageSize
          );

          this.first.set(
            (
              result.pageNumber - 1
            ) *
            result.pageSize
          );
        },

        error: error => {
          console.error(
            'Kitap listesi alınamadı:',
            error
          );

          if (
            error.name ===
            'TimeoutError'
          ) {
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

  onSearchChange(
    value: string
  ): void {
    this.searchChanges.next(
      value
    );
  }

  applySearchImmediately(): void {
    this.applySearch(
      this.searchText
    );
  }

  onStockFilterChange(
    value: StockFilterValue
  ): void {
    this.stockFilter =
      value;

    this.resetPagination();

    this.loadBooks();
  }

  onPageChange(
    event: {
      first?: number;
      rows?: number;
    }
  ): void {
    const first =
      event.first ?? 0;

    const rows =
      event.rows ??
      this.pageSize();

    this.first.set(
      first
    );

    this.pageSize.set(
      rows
    );

    this.pageNumber.set(
      Math.floor(
        first / rows
      ) + 1
    );

    this.loadBooks();
  }

  clearFilters(): void {
    this.searchText = '';
    this.appliedSearch = '';

    this.stockFilter =
      'all';

    this.searchChanges.next('');

    this.resetPagination();

    this.loadBooks();
  }

  hasActiveFilters(): boolean {
    return (
      this.appliedSearch.length >
        0 ||
      this.stockFilter !==
        'all'
    );
  }

  borrowBook(
    book: Book
  ): void {
    this.errorMessage.set('');

    if (book.stock <= 0) {
      this.errorMessage.set(
        'Bu kitap stokta bulunmuyor.'
      );

      return;
    }

    this.borrowingBookId.set(
      book.id
    );

    this.borrowService
      .borrow(book.id)
      .pipe(
        finalize(() => {
          this.borrowingBookId.set(
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
            'Kitap ödünç alınırken bir hata oluştu.'
          );
        }
      });
  }

  openEditDialog(
    book: Book
  ): void {
    this.errorMessage.set('');
    this.editDialogErrorMessage = '';

    this.editingBookId =
      book.id;

    this.editBookName =
      book.name;

    this.editBookAuthor =
      book.author;

    this.editBookStock =
      book.stock;

    this.editDialogVisible =
      true;
  }

  closeEditDialog(): void {
    this.editDialogVisible =
      false;

    this.editDialogErrorMessage =
      '';

    this.editingBookId =
      null;

    this.editBookName = '';
    this.editBookAuthor = '';
    this.editBookStock = 0;
  }

  updateBook(): void {
    this.editDialogErrorMessage =
      '';

    if (
      this.editingBookId ===
      null
    ) {
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

    if (
      !this.isValidAuthorName(
        author
      )
    ) {
      this.editDialogErrorMessage =
        'Yazar adı sayı veya geçersiz karakter içeremez.';

      return;
    }

    if (stock === null) {
      this.editDialogErrorMessage =
        'Stok zorunludur.';

      return;
    }

    if (
      !Number.isInteger(stock)
    ) {
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
          this.updatingBook =
            false;
        })
      )
      .subscribe({
        next: () => {
          this.editDialogVisible =
            false;

          this.editDialogErrorMessage =
            '';

          this.showSuccessMessage(
            'Kitap başarıyla güncellendi.'
          );

          this.editingBookId =
            null;
        },

        error: error => {
          if (
            error.status === 403
          ) {
            this.editDialogErrorMessage =
              'Bu işlem için Admin yetkisi gereklidir.';

            return;
          }

          if (
            error.status === 404
          ) {
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
    this.createDialogErrorMessage =
      '';

    this.newBookName = '';
    this.newBookAuthor = '';
    this.newBookStock = 0;

    this.createDialogVisible =
      true;
  }

  closeCreateDialog(): void {
    this.createDialogVisible =
      false;

    this.createDialogErrorMessage =
      '';

    this.newBookName = '';
    this.newBookAuthor = '';
    this.newBookStock = 0;
  }

  createBook(): void {
    this.createDialogErrorMessage =
      '';

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

    if (
      !this.isValidAuthorName(
        author
      )
    ) {
      this.createDialogErrorMessage =
        'Yazar adı sayı veya geçersiz karakter içeremez.';

      return;
    }

    if (stock === null) {
      this.createDialogErrorMessage =
        'Stok zorunludur.';

      return;
    }

    if (
      !Number.isInteger(stock)
    ) {
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
          this.creatingBook =
            false;
        })
      )
      .subscribe({
        next: book => {
          this.createDialogVisible =
            false;

          this.createDialogErrorMessage =
            '';

          this.showSuccessMessage(
            `"${book.name}" başarıyla eklendi.`
          );
        },

        error: error => {
          if (
            error.status === 403
          ) {
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

  confirmDelete(
    book: Book
  ): void {
    this.confirmationService
      .confirm({
        header:
          'Kitabı Kaldır',

        message:
          `"${book.name}" kitabını katalogdan kaldırmak istediğinize emin misiniz?`,

        acceptLabel:
          'Evet',

        rejectLabel:
          'Vazgeç',

        accept: () => {
          this.deleteBook(
            book
          );
        }
      });
  }

  private deleteBook(
    book: Book
  ): void {
    this.errorMessage.set('');

    this.deletingBookId.set(
      book.id
    );

    this.bookService
      .delete(book.id)
      .pipe(
        finalize(() => {
          this.deletingBookId.set(
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
            error.status === 409
          ) {
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

  private applySearch(
    value: string
  ): void {
    const normalizedSearch =
      value.trim();

    if (
      normalizedSearch ===
      this.appliedSearch
    ) {
      return;
    }

    this.appliedSearch =
      normalizedSearch;

    this.resetPagination();

    this.loadBooks();
  }

  private resetPagination(): void {
    this.pageNumber.set(1);
    this.first.set(0);
  }

  private getInStockFilter():
    boolean | null {

    if (
      this.stockFilter ===
      'inStock'
    ) {
      return true;
    }

    if (
      this.stockFilter ===
      'outOfStock'
    ) {
      return false;
    }

    return null;
  }

  private isValidAuthorName(
    author: string
  ): boolean {
    return this.authorNamePattern
      .test(author);
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