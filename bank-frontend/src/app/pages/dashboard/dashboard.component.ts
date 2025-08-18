import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OpenBankingService } from '../../core/services/open-banking.service';
import { AccountDto, BankCode, BankDto, TransactionDto } from '../../core/models/open-banking.models';

@Component({
  standalone: true,
  selector: 'app-dashboard',
  imports: [CommonModule, FormsModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css'],
})
export class DashboardComponent implements OnInit {
  private api = inject(OpenBankingService);

  banks = signal<BankDto[]>([]);
  selectedBank = signal<BankCode>('vakif');
  accounts = signal<AccountDto[]>([]);
  recent = signal<TransactionDto[]>([]);
  loading = signal<boolean>(false);

  // Modal state
  showAddModal = signal<boolean>(false);
  addBankCode: BankCode = 'vakif';

  // Toplam TRY bakiyesi
  totalTRY = computed(() =>
    this.accounts().reduce((s, a) => s + (a.currency === 'TRY' ? a.balance : 0), 0)
  );

  ngOnInit() {
    this.loadBanks();
  }

  loadBanks() {
    this.loading.set(true);
    this.api.getLinkedBanks().subscribe({
      next: (bs) => {
        this.banks.set(bs);
        // Varsayılan seçim: bağlı ilk banka; yoksa 'vakif'
        const connected = bs.find((b) => b.connected)?.code as BankCode | undefined;
        if (connected) this.selectedBank.set(connected);
        this.loadBankData();
      },
      error: () => this.loading.set(false),
    });
  }

  onSelectBank(code: BankCode) {
    if (this.selectedBank() !== code) {
      this.selectedBank.set(code);
      this.loadBankData();
    }
  }

  loadBankData() {
    const bank = this.selectedBank();
    this.loading.set(true);
    this.api.getAccounts(bank).subscribe({
      next: (accs) => {
        this.accounts.set(accs);
        this.api.getRecentTransactions(bank, 5).subscribe({
          next: (tx) => {
            this.recent.set(tx);
            this.loading.set(false);
          },
          error: () => this.loading.set(false),
        });
      },
      error: () => this.loading.set(false),
    });
  }

  openAddBank() {
    this.showAddModal.set(true);
  }
  closeAddBank() {
    this.showAddModal.set(false);
  }

  confirmAddBank() {
    this.api.linkBank({ bankCode: this.addBankCode }).subscribe({
      next: () => {
        this.closeAddBank();
        this.loadBanks();
      },
      error: () => this.closeAddBank(),
    });
  }
}
