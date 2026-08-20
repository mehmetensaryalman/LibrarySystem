import {
  Injectable
} from '@angular/core';

import {
  HttpClient
} from '@angular/common/http';

import {
  Observable
} from 'rxjs';

import {
  AdminBorrow,
  BorrowedBook,
  BorrowPenaltyStatus,
  OperationResult
} from '../models/borrow.models';

@Injectable({
  providedIn: 'root'
})
export class BorrowService {
  private readonly apiUrl =
    'https://localhost:7008/api';

  constructor(
    private readonly http:
      HttpClient
  ) {
  }

  borrow(
    bookId: number
  ): Observable<OperationResult> {
    return this.http
      .post<OperationResult>(
        `${this.apiUrl}/borrow/${bookId}`,
        {}
      );
  }

  returnBook(
    bookId: number
  ): Observable<OperationResult> {
    return this.http
      .post<OperationResult>(
        `${this.apiUrl}/return/${bookId}`,
        {}
      );
  }

  returnBorrowForAdmin(
    borrowRecordId: number
  ): Observable<OperationResult> {
    return this.http
      .post<OperationResult>(
        `${this.apiUrl}/admin/borrows/${borrowRecordId}/return`,
        {}
      );
  }

  getMyBooks():
    Observable<BorrowedBook[]> {
    return this.http
      .get<BorrowedBook[]>(
        `${this.apiUrl}/borrow/my-books`
      );
  }

  getMyPenaltyStatus():
    Observable<BorrowPenaltyStatus> {
    return this.http
      .get<BorrowPenaltyStatus>(
        `${this.apiUrl}/borrow/my-penalty-status`
      );
  }

  getAllBorrowsForAdmin():
    Observable<AdminBorrow[]> {
    return this.http
      .get<AdminBorrow[]>(
        `${this.apiUrl}/admin/borrows`
      );
  }
}