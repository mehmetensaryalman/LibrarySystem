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
  AdminBorrowRequest,
  BorrowedBook,
  BorrowPenaltyStatus,
  MyBorrowRequest,
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

  cancelBorrowRequest(
    borrowRequestId: number
  ): Observable<OperationResult> {
    return this.http
      .delete<OperationResult>(
        `${this.apiUrl}/borrow/requests/${borrowRequestId}`
      );
  }

  getMyPendingBorrowRequests():
    Observable<MyBorrowRequest[]> {
    return this.http
      .get<MyBorrowRequest[]>(
        `${this.apiUrl}/borrow/my-pending-requests`
      );
  }

  requestReturn(
    bookId: number
  ): Observable<OperationResult> {
    return this.http
      .post<OperationResult>(
        `${this.apiUrl}/borrow/${bookId}/return-request`,
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

  getPendingBorrowRequestsForAdmin():
    Observable<AdminBorrowRequest[]> {
    return this.http
      .get<AdminBorrowRequest[]>(
        `${this.apiUrl}/admin/borrow-requests`
      );
  }

  approveBorrowRequest(
    borrowRequestId: number
  ): Observable<OperationResult> {
    return this.http
      .put<OperationResult>(
        `${this.apiUrl}/admin/borrow-requests/${borrowRequestId}/approve`,
        {}
      );
  }

  rejectBorrowRequest(
    borrowRequestId: number,
    reason: string | null
  ): Observable<OperationResult> {
    return this.http
      .put<OperationResult>(
        `${this.apiUrl}/admin/borrow-requests/${borrowRequestId}/reject`,
        {
          reason
        }
      );
  }

  getAllBorrowsForAdmin():
    Observable<AdminBorrow[]> {
    return this.http
      .get<AdminBorrow[]>(
        `${this.apiUrl}/admin/borrows`
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
}