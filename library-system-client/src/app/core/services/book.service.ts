import { Injectable } from '@angular/core';
import {
  HttpClient,
  HttpParams
} from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  ArchivedBook,
  Book,
  BookFilter,
  CreateBookRequest,
  DeleteBookResponse,
  PagedResult,
  RestoreBookResponse,
  UpdateBookRequest
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

  getPaged(
    filter: BookFilter
  ): Observable<PagedResult<Book>> {
    let params =
      new HttpParams()
        .set(
          'pageNumber',
          filter.pageNumber.toString()
        )
        .set(
          'pageSize',
          filter.pageSize.toString()
        );

    const search =
      filter.search?.trim();

    if (search) {
      params = params.set(
        'search',
        search
      );
    }

    if (
      filter.inStock !== null &&
      filter.inStock !== undefined
    ) {
      params = params.set(
        'inStock',
        filter.inStock.toString()
      );
    }

    return this.http.get<
      PagedResult<Book>
    >(
      `${this.apiUrl}/paged`,
      {
        params
      }
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
    return this.http
      .delete<DeleteBookResponse>(
        `${this.apiUrl}/${bookId}`
      );
  }

  getArchived():
    Observable<ArchivedBook[]> {
    return this.http
      .get<ArchivedBook[]>(
        `${this.apiUrl}/archived`
      );
  }

  restore(
    bookId: number
  ): Observable<RestoreBookResponse> {
    return this.http
      .post<RestoreBookResponse>(
        `${this.apiUrl}/${bookId}/restore`,
        {}
      );
  }
}