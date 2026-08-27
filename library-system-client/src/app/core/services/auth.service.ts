import {
  Injectable
} from '@angular/core';

import {
  HttpClient
} from '@angular/common/http';

import {
  Observable,
  Subject,
  tap
} from 'rxjs';

import {
  AuthResult,
  LoginRequest,
  RegisterRequest
} from '../models/auth.models';

import {
  ForgotPasswordRequest,
  ResetPasswordRequest
} from '../models/password-reset.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl =
    'https://localhost:7008/api/auth';

  private readonly tokenKey =
    'library_token';

  private readonly logoutSubject =
    new Subject<void>();

  readonly logout$ =
    this.logoutSubject.asObservable();

  constructor(
    private readonly http:
      HttpClient
  ) {
  }

  register(
    request: RegisterRequest
  ): Observable<AuthResult> {

    return this.http
      .post<AuthResult>(
        `${this.apiUrl}/register`,
        request
      );
  }

  login(
    request: LoginRequest,
    rememberMe: boolean = false
  ): Observable<AuthResult> {

    return this.http
      .post<AuthResult>(
        `${this.apiUrl}/login`,
        request
      )
      .pipe(
        tap(result => {
          if (
            result.success &&
            result.token
          ) {
            this.storeToken(
              result.token,
              rememberMe
            );
          }
        })
      );
  }

  forgotPassword(
    request: ForgotPasswordRequest
  ): Observable<AuthResult> {

    return this.http
      .post<AuthResult>(
        `${this.apiUrl}/forgot-password`,
        request
      );
  }

  resetPassword(
    request: ResetPasswordRequest
  ): Observable<AuthResult> {

    return this.http
      .post<AuthResult>(
        `${this.apiUrl}/reset-password`,
        request
      );
  }

  getToken(): string | null {
    return (
      localStorage.getItem(
        this.tokenKey
      ) ??
      sessionStorage.getItem(
        this.tokenKey
      )
    );
  }

  isLoggedIn(): boolean {
    const token =
      this.getToken();

    if (!token) {
      return false;
    }

    const expirationTime =
      this.getTokenExpirationTime(
        token
      );

    if (
      expirationTime === null ||
      expirationTime <= Date.now()
    ) {
      this.logout();

      return false;
    }

    return true;
  }

  logout(): void {
    this.clearStoredToken();

    this.logoutSubject.next();
  }

  getRoles(): string[] {
    const token =
      this.getToken();

    if (!token) {
      return [];
    }

    try {
      const parts =
        token.split('.');

      if (parts.length !== 3) {
        return [];
      }

      let payload =
        parts[1]
          .replace(/-/g, '+')
          .replace(/_/g, '/');

      const padding =
        payload.length % 4;

      if (padding) {
        payload += '='.repeat(
          4 - padding
        );
      }

      const decodedPayload =
        JSON.parse(
          atob(payload)
        );

      const roleClaim =
        decodedPayload[
          'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
        ];

      if (!roleClaim) {
        return [];
      }

      return Array.isArray(
        roleClaim
      )
        ? roleClaim
        : [roleClaim];
    } catch {
      return [];
    }
  }

  isAdmin(): boolean {
    return this
      .getRoles()
      .includes('Admin');
  }

  private storeToken(
    token: string,
    rememberMe: boolean
  ): void {

    this.clearStoredToken();

    const storage =
      rememberMe
        ? localStorage
        : sessionStorage;

    storage.setItem(
      this.tokenKey,
      token
    );
  }

  private clearStoredToken():
    void {

    localStorage.removeItem(
      this.tokenKey
    );

    sessionStorage.removeItem(
      this.tokenKey
    );
  }

  private getTokenExpirationTime(
    token: string
  ): number | null {

    try {
      const parts =
        token.split('.');

      if (parts.length !== 3) {
        return null;
      }

      let payload =
        parts[1]
          .replace(/-/g, '+')
          .replace(/_/g, '/');

      const padding =
        payload.length % 4;

      if (padding) {
        payload += '='.repeat(
          4 - padding
        );
      }

      const decodedPayload =
        JSON.parse(
          atob(payload)
        );

      if (
        typeof decodedPayload.exp !==
        'number'
      ) {
        return null;
      }

      return (
        decodedPayload.exp *
        1000
      );
    } catch {
      return null;
    }
  }
}