import { Routes } from '@angular/router';
import { authGuard } from './guards/auth-guard';
import { LoginComponent } from './pages/login/login.component';
import { RegisterComponent } from './pages/register/register.component';

export const routes: Routes = [
  // --- Giriş & Kayıt (korumasız) ---
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },

  // --- Login sonrası ekranlar (korumalı) ---
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./pages/dashboard/dashboard.component')
        .then(m => m.DashboardComponent),
    canActivate: [authGuard],
  },
  {
    path: 'connect',
    loadComponent: () =>
      import('./pages/connect/connect.component')
        .then(m => m.ConnectComponent),
    canActivate: [authGuard],
  },
  {
    path: 'accounts',
    loadComponent: () =>
      import('./pages/accounts/accounts.component')
        .then(m => m.AccountsComponent),
    canActivate: [authGuard],
  },

  // --- VakıfBank özel URL'ler (aynı sayfaya gider) ---
  // /vakifbank/accounts/:accountNumber/details  → AccountsComponent
  {
    path: 'vakifbank/accounts/:accountNumber/details',
    loadComponent: () =>
      import('./pages/accounts/accounts.component')
        .then(m => m.AccountsComponent),
    canActivate: [authGuard],
  },
  // /vakifbank/accounts/:accountNumber/transactions → AccountsComponent
  {
    path: 'vakifbank/accounts/:accountNumber/transactions',
    loadComponent: () =>
      import('./pages/accounts/accounts.component')
        .then(m => m.AccountsComponent),
    canActivate: [authGuard],
  },

  // --- Varsayılan & 404 ---
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: '**', redirectTo: 'login' },
];
