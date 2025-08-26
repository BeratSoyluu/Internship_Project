// src/app/services/auth.service.ts
import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs/operators';
import { Observable, Subject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'token';
  private readonly API_URL = '/api/Auth'; // proxy.conf.json ile backend'e gider

  private http = inject(HttpClient);
  private router = inject(Router);

  // Uygulama genelinde auth durumunu dinlemek istersen
  readonly authChanged$ = new Subject<boolean>();

  // Getter'lar
  get isLoggedIn(): boolean {
    return !!this.token;
  }

  get token(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  // ---------------- helpers ----------------
  private saveToken(token: string) {
    localStorage.setItem(this.TOKEN_KEY, token);
    this.authChanged$.next(true);
  }

  private clearAuth() {
    try { localStorage.removeItem(this.TOKEN_KEY); } catch {}
    try { sessionStorage.clear(); } catch {}
    this.authChanged$.next(false);
  }

  // ---------------- API ----------------
  login(email: string, password: string): Observable<any> {
    return this.http.post<any>(`${this.API_URL}/login`, { email, password }).pipe(
      tap(res => {
        if (res?.token) this.saveToken(res.token);
      })
    );
  }

  register(firstName: string, lastName: string, email: string, password: string): Observable<any> {
    return this.http.post<any>(`${this.API_URL}/register`, {
      firstName, lastName, email, password
    }).pipe(
      tap(res => {
        if (res?.token) this.saveToken(res.token);
      })
    );
  }

  logout(): void {
    this.clearAuth();

    // 1) Angular navigasyon: geçerli history kaydını değiştirir
    this.router.navigate(['/login'], { replaceUrl: true }).catch(() => {});

    // 2) Emniyet kemeri: tam sayfa replace (bazı tarayıcıların BFCache/geri davranışlarını kırar)
    const tree = this.router.createUrlTree(['/login']);
    const url = this.router.serializeUrl(tree);
    setTimeout(() => {
      try { window.location.replace(url); } catch {}
    }, 0);
  }
}
