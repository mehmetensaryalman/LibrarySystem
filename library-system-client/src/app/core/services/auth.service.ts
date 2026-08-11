import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import {
  AuthResult,
  LoginRequest,
  RegisterRequest
} from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = 'https://localhost:7008/api/auth';
  private readonly tokenKey = 'library_token';

  constructor(private readonly http: HttpClient) {
  }

  register(request: RegisterRequest): Observable<AuthResult> {
    return this.http.post<AuthResult>(
      `${this.apiUrl}/register`,
      request
    );
  }

  login(request: LoginRequest): Observable<AuthResult> {
    return this.http
      .post<AuthResult>(
        `${this.apiUrl}/login`,
        request
      )
      .pipe(
        tap(result => {
          if (result.success && result.token) {
            localStorage.setItem(
              this.tokenKey,
              result.token
            );
          }
        })
      );
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  isLoggedIn(): boolean {
    return this.getToken() !== null;
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
  }
}