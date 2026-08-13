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