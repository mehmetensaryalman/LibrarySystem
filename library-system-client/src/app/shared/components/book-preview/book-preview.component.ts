import {
  Component,
  Input,
  OnDestroy,
  signal
} from '@angular/core';

import {
  finalize,
  Subscription
} from 'rxjs';

import {
  ButtonModule
} from 'primeng/button';

import {
  DialogModule
} from 'primeng/dialog';

import {
  Book,
  BookPreview
} from '../../../core/models/book.models';

import {
  BookService
} from '../../../core/services/book.service';

@Component({
  selector: 'app-book-preview',

  imports: [
    ButtonModule,
    DialogModule
  ],

  templateUrl:
    './book-preview.component.html',

  styleUrl:
    './book-preview.component.scss'
})
export class BookPreviewComponent
  implements OnDestroy {

  @Input()
  book!: Book;

  previewDialogVisible =
    false;

  readonly loading =
    signal(false);

  readonly errorMessage =
    signal('');

  readonly preview =
    signal<BookPreview | null>(
      null
    );

  readonly coverImageFailed =
    signal(false);

  private previewSubscription:
    Subscription | null = null;

  constructor(
    private readonly bookService:
      BookService
  ) {
  }

  ngOnDestroy(): void {
    this.previewSubscription
      ?.unsubscribe();
  }

  openPreview(): void {
    this.previewDialogVisible =
      true;

    this.loadPreview();
  }

  closePreview(): void {
    this.previewDialogVisible =
      false;

    this.previewSubscription
      ?.unsubscribe();

    this.previewSubscription =
      null;
  }

  retry(): void {
    this.loadPreview();
  }

  onCoverImageError(): void {
    this.coverImageFailed.set(
      true
    );
  }

  private loadPreview(): void {
    if (!this.book) {
      return;
    }

    this.previewSubscription
      ?.unsubscribe();

    this.loading.set(true);
    this.errorMessage.set('');
    this.preview.set(null);

    this.coverImageFailed.set(
      false
    );

    this.previewSubscription =
      this.bookService
        .getPreview(
          this.book.id
        )
        .pipe(
          finalize(() => {
            this.loading.set(false);
          })
        )
        .subscribe({
          next: preview => {
            this.preview.set(
              preview
            );
          },

          error: error => {
            console.error(
              'Kitap önizlemesi alınamadı:',
              error
            );

            if (
              error.status === 404
            ) {
              this.errorMessage.set(
                error.error?.message ??
                'Kitap bulunamadı.'
              );

              return;
            }

            if (
              error.status === 401
            ) {
              this.errorMessage.set(
                'Kitap önizlemesini görüntülemek için yeniden giriş yapmanız gerekiyor.'
              );

              return;
            }

            this.errorMessage.set(
              error.error?.message ??
              'Kitap önizlemesi yüklenirken bir hata oluştu.'
            );
          }
        });
  }
}