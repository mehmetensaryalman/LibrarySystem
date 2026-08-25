export interface Book {
  id: number;
  name: string;
  author: string;
  stock: number;
}

export interface BookPreview {
  id: number;
  name: string;
  author: string;
  coverImageUrl: string | null;
  pageCount: number | null;
  summary: string | null;
  infoUrl: string | null;
  source: string | null;
  metadataFound: boolean;
}

export interface CreateBookRequest {
  name: string;
  author: string;
  stock: number;
}

export interface DeleteBookResponse {
  message: string;
}

export interface UpdateBookRequest {
  name: string;
  author: string;
  stock: number;
}

export interface ArchivedBook {
  id: number;
  name: string;
  author: string;
  stock: number;
  isArchived: boolean;
  archivedAt: string | null;
}

export interface RestoreBookResponse {
  message: string;
  book: ArchivedBook;
}

export type BookSortBy =
  'newest' |
  'nameAsc';

export interface BookFilter {
  search?: string;
  inStock?: boolean | null;
  sortBy: BookSortBy;
  pageNumber: number;
  pageSize: number;
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}