import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OpenBankingService } from '../../core/services/open-banking.service';
import { VakifAccountRow } from './models/vakif-account-row';
import { Router } from '@angular/router';

@Component({
  selector: 'vb-accounts',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './vb-accounts.component.html',
  styleUrls: ['./vb-accounts.component.css'],
})
export class VbAccountsComponent implements OnInit {
  private api = inject(OpenBankingService);
  private router = inject(Router);

  loading = signal(true);
  rows = signal<VakifAccountRow[]>([]);
  error = signal<string | null>(null);

  ngOnInit(): void {
    // A: hazır rows dönen endpoint
    this.api.getVakifAccountRows().subscribe({
      next: (data) => {
        this.rows.set(data ?? []);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Hesap listesi alınırken hata oluştu');
        this.loading.set(false);
        console.error(err);
      }
    });

    // Eğer B seçeneğini kullanacaksan:
    // this.api.getVakifAccountRowsFromDto().subscribe( ... );
  }

  openDetails(row: VakifAccountRow) {
    // istediğin rotayı kullan
    // ör: /vakifbank/accounts/:no/details
    this.router.navigate(['/vakifbank/accounts', row.accountNumber, 'details']);
  }

  openTransactions(row: VakifAccountRow) {
    // ör: /vakifbank/accounts/:no/transactions
    this.router.navigate(['/vakifbank/accounts', row.accountNumber, 'transactions']);
  }
}
