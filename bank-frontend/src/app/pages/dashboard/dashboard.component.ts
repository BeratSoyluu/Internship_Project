import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OpenBankingService } from '../../core/services/open-banking.service';
import { AuthService } from '../../services/auth.service';
import { BankDto, BankCode, AccountDto, TransactionDto } from '../../core/models/open-banking.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css'],
})
export class DashboardComponent implements OnInit {
  private api = inject(OpenBankingService);
  private auth = inject(AuthService);

  banks = signal<BankDto[]>([]);
  selectedBank = signal<BankCode | null>(null);
  accounts = signal<AccountDto[]>([]);
  recent = signal<TransactionDto[]>([]);

  // seçili bankanın adı (UI için)
  selectedBankName = computed(() => {
    const code = this.selectedBank();
    const b = this.banks().find(x => x.code === code);
    return b?.name ?? '';
  });

  ngOnInit() {
    this.loadBanks();
  }

  loadBanks() {
    this.api.getLinkedBanks().subscribe({
      next: (list) => this.banks.set(list),
    });
  }

  selectBank(code: BankCode) {
    if (this.selectedBank() === code) return;
    this.selectedBank.set(code);
    this.accounts.set([]);
    this.recent.set([]);

    this.api.getAccounts(code).subscribe(a => this.accounts.set(a));
    this.api.getRecentTransactions(code, 5).subscribe(tx => this.recent.set(tx));
  }

  openAddBank() {
    // Basit akış: prompt ile banka seçelim; sonra backend’e gönderelim.
    const bank = (prompt('Hangi banka eklensin? (vakif / mybank)') || '').toLowerCase().trim();
    if (bank !== 'vakif' && bank !== 'mybank') return;

    this.api.linkNewBank(bank as BankCode).subscribe({
      next: () => this.loadBanks()
    });
  }

  logout() {
    this.auth.logout(); // seni login sayfasına döndürüyor
  }
}
