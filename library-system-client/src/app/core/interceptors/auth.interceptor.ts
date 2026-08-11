import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (
  request,
  next
) => {
  const token = localStorage.getItem('library_token');

  const isAuthRequest =
    request.url.includes('/api/auth/');

  const isPublicBooksRequest =
    request.method === 'GET' &&
    request.url.endsWith('/api/books');

  if (
    !token ||
    isAuthRequest ||
    isPublicBooksRequest
  ) {
    return next(request);
  }

  const authenticatedRequest = request.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });

  return next(authenticatedRequest);
};