import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, map, of } from 'rxjs';

export const authGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const http   = inject(HttpClient);

  // 1) LocalStorage token kontrolü
  const token =
    localStorage.getItem('access_token') ||
    localStorage.getItem('auth.token');

    console.log(token);
  if (token) return true;

  // 2) Token yoksa cookie tabanlı oturum var mı kontrol et
  return http.get('/api/auth/me', { withCredentials: true }).pipe(
    map(() => true),
    catchError(() =>
      of(router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } }))
    )
  );
};
