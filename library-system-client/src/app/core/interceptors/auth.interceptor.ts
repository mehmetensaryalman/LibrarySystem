import {
  HttpErrorResponse,
  HttpInterceptorFn
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import {
  catchError,
  throwError
} from 'rxjs';

export const authInterceptor: HttpInterceptorFn =
  (request, next) => {
    const router = inject(Router);

    const token =
      localStorage.getItem('library_token');

    const isAuthRequest =
      request.url.includes('/api/auth/');

    const isPublicBooksRequest =
      request.method === 'GET' &&
      request.url.endsWith('/api/books');

    let outgoingRequest = request;

    if (
      token &&
      !isAuthRequest &&
      !isPublicBooksRequest
    ) {
      outgoingRequest = request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
    }

    return next(outgoingRequest).pipe(
      catchError(
        (error: HttpErrorResponse) => {
          if (
            error.status === 401 &&
            !isAuthRequest
          ) {
            localStorage.removeItem(
              'library_token'
            );

            void router.navigate(['/login']);
          }

          return throwError(() => error);
        }
      )
    );
  };