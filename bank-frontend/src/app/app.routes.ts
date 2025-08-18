import { Routes } from '@angular/router';
import { authGuard } from './guards/auth-guard';
import { LoginComponent } from './pages/login/login.component';
import { RegisterComponent } from './pages/register/register.component';

export const routes: Routes = [
  // Giriş & Kayıt (korumasız)
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },

  // Login sonrası ekranlar (korumalı)
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./pages/dashboard/dashboard.component')
        .then(m => m.DashboardComponent),   // ✅ isim düzeltildi
    canActivate: [authGuard]
  },
  {
    path: 'connect',
    loadComponent: () =>
      import('./pages/connect/connect.component')
        .then(m => m.ConnectComponent),
    canActivate: [authGuard]
  },
  {
    path: 'accounts',
    loadComponent: () =>
      import('./pages/accounts/accounts.component')
        .then(m => m.AccountsComponent),
    canActivate: [authGuard]
  },

  // Varsayılan route: login
  { path: '', pathMatch: 'full', redirectTo: 'login' },

  // 404 → login
  { path: '**', redirectTo: 'login' }
];
