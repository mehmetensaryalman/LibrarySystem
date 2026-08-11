import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Book } from '../models/book.models';

@Injectable({
  providedIn: 'root'
})
export class BookService {
  private readonly apiUrl = 'https://localhost:7008/api/books';

  constructor(private readonly http: HttpClient) {
  }

  getAll(): Observable<Book[]> {
    return this.http.get<Book[]>(this.apiUrl);
  }
}