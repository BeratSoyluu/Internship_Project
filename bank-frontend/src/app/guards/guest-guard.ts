import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, map, of } from 'rxjs';

export const guestGuard: CanActivateFn = (_r, _s) => {
  const router = inject(Router);
  const http = inject(HttpClient);

  const token = localStorage.getItem('access_token') || localStorage.getItem('auth.token');
  if (token) return router.createUrlTree(['/dashboard']);

  return http.get('/api/auth/me', { withCredentials: true }).pipe(
    map(() => router.createUrlTree(['/dashboard'])),
    catchError(() => of(true))
  );
};
