import { Routes } from '@angular/router';
import { authGuard } from './guards/auth-guard';
import { LoginComponent } from './pages/login/login.component';
import { RegisterComponent } from './pages/register/register.component';

export const routes: Routes = [
  // Giriş & Kayıt (korumasız)
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },

  // Login sonrası: Bağlan sayfası
  {
    path: 'connect',
    loadComponent: () =>
      import('./pages/connect/connect.component').then(m => m.ConnectComponent),
    canActivate: [authGuard]
  },

  // (Varsa) accounts sayfası
  {
    path: 'accounts',
    loadComponent: () =>
      import('./pages/accounts/accounts.component').then(m => m.AccountsComponent),
    canActivate: [authGuard]
  },

  // Varsayılan ve 404
  { path: '', pathMatch: 'full', redirectTo: 'connect' },
  { path: '**', redirectTo: 'connect' }
];
