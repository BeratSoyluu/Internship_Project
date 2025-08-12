import { RouterModule, Routes } from '@angular/router';
import { LoginComponent } from './pages/login/login.component';
import { VakifbankLoginComponent } from './pages/vakifbank-login/vakifbank-login.component';

const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'vakifbank-login', component: VakifbankLoginComponent },
  { path: '', redirectTo: '/login', pathMatch: 'full' }
];

export const AppRoutingModule = RouterModule.forRoot(routes);
