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
  AuthResult
} from '../models/auth.models';

import {
  ChangePasswordRequest
} from '../models/change-password.models';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  private readonly apiUrl =
    'https://localhost:7008/api/auth';

  constructor(
    private readonly http:
      HttpClient
  ) {
  }

  changePassword(
    request: ChangePasswordRequest
  ): Observable<AuthResult> {

    return this.http.post<AuthResult>(
      `${this.apiUrl}/change-password`,
      request
    );
  }
}