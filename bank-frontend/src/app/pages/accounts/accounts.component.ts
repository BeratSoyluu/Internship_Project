import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-accounts',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './accounts.component.html',
  styles: [`
    .wrap { max-width: 800px; margin: 40px auto; padding: 16px; display: grid; gap: 12px; }
    button { padding: 8px 12px; border: 0; border-radius: 8px; cursor: pointer; }
  `]
})
export class AccountsComponent {
  auth = inject(AuthService);

  logout() {
    this.auth.logout();
  }
}
