import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Keeps the application behind a sign-in, redirecting anonymous visitors to the login page. */
export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.isSignedIn() ? true : router.createUrlTree(['/prijava']);
};

/**
 * Keeps the admin screens out of an ordinary user's way. Signed-in non-admins are sent to
 * the analysis page rather than the login page, since signing in again would not help them.
 */
export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isSignedIn()) {
    return router.createUrlTree(['/prijava']);
  }

  return authService.isAdmin() ? true : router.createUrlTree(['/analiza']);
};

/**
 * The mirror image: keeps administrators out of the business-facing screens. An admin
 * account maintains the catalog and the corpus; it has no price list of its own to manage.
 */
export const businessGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isSignedIn()) {
    return router.createUrlTree(['/prijava']);
  }

  return authService.isAdmin() ? router.createUrlTree(['/admin/katalog']) : true;
};
