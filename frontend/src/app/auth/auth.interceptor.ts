import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

/**
 * Attaches the bearer token to API calls and signs the user out on a 401, so an
 * expired token cannot leave the UI showing a logged-in shell it cannot fill.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const token = authService.token;

  const authorized = token
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;

  return next(authorized).pipe(
    catchError((error: HttpErrorResponse) => {
      // Only a rejected token should force a sign-out; a 401 from the login endpoint
      // itself just means wrong credentials and is handled by the form.
      if (error.status === 401 && token && !request.url.includes('/auth/')) {
        authService.logout();
        router.navigate(['/prijava']);
      }

      return throwError(() => error);
    }),
  );
};
