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
  private readonly apiUrl =
    'https://localhost:7008/api/auth';

  private readonly tokenKey = 'library_token';

  constructor(private readonly http: HttpClient) {
  }

  register(
    request: RegisterRequest
  ): Observable<AuthResult> {
    return this.http.post<AuthResult>(
      `${this.apiUrl}/register`,
      request
    );
  }

  login(
    request: LoginRequest
  ): Observable<AuthResult> {
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
    const token = this.getToken();

    if (!token) {
      return false;
    }

    const expirationTime =
      this.getTokenExpirationTime(token);

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
    localStorage.removeItem(this.tokenKey);
  }

  getRoles(): string[] {
  const token = this.getToken();

  if (!token) {
    return [];
  }

  try {
    const parts = token.split('.');

    if (parts.length !== 3) {
      return [];
    }

    let payload = parts[1]
      .replace(/-/g, '+')
      .replace(/_/g, '/');

    const padding = payload.length % 4;

    if (padding) {
      payload += '='.repeat(4 - padding);
    }

    const decodedPayload = JSON.parse(
      atob(payload)
    );

    const roleClaim =
      decodedPayload[
        'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
      ];

    if (!roleClaim) {
      return [];
    }

    return Array.isArray(roleClaim)
      ? roleClaim
      : [roleClaim];
  } catch {
    return [];
  }
}

  isAdmin(): boolean {
    return this.getRoles().includes('Admin');
  }

  private getTokenExpirationTime(
    token: string
  ): number | null {
    try {
      const parts = token.split('.');

      if (parts.length !== 3) {
        return null;
      }

      let payload = parts[1]
        .replace(/-/g, '+')
        .replace(/_/g, '/');

      const padding =
        payload.length % 4;

      if (padding) {
        payload += '='.repeat(4 - padding);
      }

      const decodedPayload =
        JSON.parse(atob(payload));

      if (
        typeof decodedPayload.exp !== 'number'
      ) {
        return null;
      }

      return decodedPayload.exp * 1000;
    } catch {
      return null;
    }
  }
}