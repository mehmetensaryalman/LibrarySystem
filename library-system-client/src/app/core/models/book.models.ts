export interface Book {
  id: number;
  name: string;
  author: string;
  stock: number;
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
}

export interface RestoreBookResponse {
  message: string;
  book: ArchivedBook;
}

export interface BookFilter {
  search?: string;
  inStock?: boolean | null;
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