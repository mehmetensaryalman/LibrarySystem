import {
  HttpErrorResponse,
  HttpInterceptorFn
} from '@angular/common/http';

import {
  inject
} from '@angular/core';

import {
  Router
} from '@angular/router';

import {
  catchError,
  throwError
} from 'rxjs';

import {
  AuthService
} from '../services/auth.service';

export const authInterceptor:
  HttpInterceptorFn =
    (request, next) => {

      const router =
        inject(Router);

      const authService =
        inject(AuthService);

      const token =
        authService.getToken();

      const publicAuthEndpoints = [
        '/api/auth/register',
        '/api/auth/login',
        '/api/auth/forgot-password',
        '/api/auth/reset-password'
      ];

      const isPublicAuthRequest =
        publicAuthEndpoints.some(
          endpoint =>
            request.url.includes(
              endpoint
            )
        );

      const isPublicBooksRequest =
        request.method === 'GET' &&
        request.url.endsWith(
          '/api/books'
        );

      let outgoingRequest =
        request;

      if (
        token &&
        !isPublicAuthRequest &&
        !isPublicBooksRequest
      ) {
        outgoingRequest =
          request.clone({
            setHeaders: {
              Authorization:
                `Bearer ${token}`
            }
          });
      }

      return next(
        outgoingRequest
      ).pipe(
        catchError(
          (
            error:
              HttpErrorResponse
          ) => {

            if (
              error.status === 401 &&
              !isPublicAuthRequest
            ) {
              authService.logout();

              void router.navigate(
                ['/login']
              );
            }

            return throwError(
              () => error
            );
          }
        )
      );
    };