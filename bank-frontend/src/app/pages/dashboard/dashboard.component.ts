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

  private loadForBank(code: BankCode) {
    if (this.selectedBank() === code) return;

    this.selectedBank.set(code);
    this.accounts.set([]);
    this.recent.set([]);

    // MyBank için tek "Vadesiz" hesabı göster
    const anyApiAsAny = this.api as any;
    if (code === 'mybank' && typeof anyApiAsAny.getMyBankAccount === 'function') {
      anyApiAsAny.getMyBankAccount().subscribe({
        next: (acc: AccountDto) => this.accounts.set(acc ? [acc] : []),
        error: () => this.fallbackAccounts(code),
      });
    } else {
      this.fallbackAccounts(code);
    }

    this.api.getRecentTransactions(code, 5).subscribe({
      next: tx => this.recent.set(tx ?? []),
      error: () => this.recent.set([]),
    });
  }

  private fallbackAccounts(code: BankCode) {
    this.api.getAccounts(code).subscribe({
      next: a => this.accounts.set(a ?? []),
      error: () => this.accounts.set([]),
    });
  }

  loadBanks() {
    this.api.getLinkedBanks().subscribe({
      next: (list) => {
        this.banks.set(list);

        // Önce mybank (bağlıysa) seç, yoksa ilk bağlı banka
        const preferred =
          list.find(b => b.code === 'mybank' && b.connected)?.code
          ?? list.find(b => b.connected)?.code
          ?? null;

        if (preferred) this.loadForBank(preferred);
      },
    });
  }

  selectBank(code: BankCode) {
    this.loadForBank(code);
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
