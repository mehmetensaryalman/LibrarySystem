import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ArchivedBook,
  Book,
  CreateBookRequest,
  UpdateBookRequest,
  DeleteBookResponse,
  RestoreBookResponse
} from '../models/book.models';

@Injectable({
  providedIn: 'root'
})
export class BookService {
  private readonly apiUrl =
    'https://localhost:7008/api/books';

  constructor(
    private readonly http: HttpClient
  ) {
  }

  getAll(): Observable<Book[]> {
    return this.http.get<Book[]>(
      this.apiUrl
    );
  }

  create(
    request: CreateBookRequest
  ): Observable<Book> {
    return this.http.post<Book>(
      this.apiUrl,
      request
    );
  }

  update(
    bookId: number,
    request: UpdateBookRequest
  ): Observable<Book> {
    return this.http.put<Book>(
      `${this.apiUrl}/${bookId}`,
      request
    );
  }

  delete(
    bookId: number
  ): Observable<DeleteBookResponse> {
    return this.http.delete<DeleteBookResponse>(
      `${this.apiUrl}/${bookId}`
    );
  }

  getArchived(): Observable<ArchivedBook[]> {
  return this.http.get<ArchivedBook[]>(
    `${this.apiUrl}/archived`
  );
}

  restore(
    bookId: number
  ): Observable<RestoreBookResponse> {
    return this.http.post<RestoreBookResponse>(
      `${this.apiUrl}/${bookId}/restore`,
      {}
    );
  }
}