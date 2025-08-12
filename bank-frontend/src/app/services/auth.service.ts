import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'token';
  router = inject(Router);

  get isLoggedIn() { return !!localStorage.getItem(this.TOKEN_KEY); }
  get token() { return localStorage.getItem(this.TOKEN_KEY); }

  login(email: string, password: string) {
    return new Promise<boolean>(r => setTimeout(() => {
      localStorage.setItem(this.TOKEN_KEY, 'mock-jwt-token'); r(true);
    }, 500));
  }

  register(name: string, email: string, password: string) {
    return new Promise<boolean>(r => setTimeout(() => {
      localStorage.setItem(this.TOKEN_KEY, 'mock-jwt-token'); r(true);
    }, 600));
  }

  logout() { localStorage.removeItem(this.TOKEN_KEY); this.router.navigateByUrl('/login'); }
}
