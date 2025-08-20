import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  OpenBankingService,
  VakifAccountRow
} from '../../core/services/open-banking.service';
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

  // MyBank & genel hesaplar
  accounts = signal<AccountDto[]>([]);
  // VakıfBank özel hesap listesi
  vakifAccounts = signal<VakifAccountRow[]>([]);

  // son işlemler
  recent = signal<TransactionDto[]>([]);

  // ✅ seçilen banka vakıf mı?
  vakifMi = computed(() => this.normalizeCode(this.selectedBank()) === 'vakif');

  /** Banka kodunu normalize et */
  private normalizeCode(code: any): string {
    if (code === 0 || String(code).toLowerCase() === 'vakif') return 'vakif';
    if (code === 1 || String(code).toLowerCase() === 'mybank') return 'mybank';
    return String(code ?? '').toLowerCase();
  }

  // seçili bankanın adı (UI için)
  selectedBankName = computed(() => {
    const raw = this.selectedBank();
    const norm = this.normalizeCode(raw);
    const found = this.banks().find(b => this.normalizeCode(b.code) === norm);
    return found?.name ?? (norm ? norm : '');
  });

  ngOnInit() {
    this.loadBanks();
  }

  private loadForBank(code: BankCode) {
    if (this.selectedBank() === code) return;
    console.log('[dashboard] loadForBank =>', code);

    this.selectedBank.set(code);
    this.accounts.set([]);
    this.vakifAccounts.set([]);
    this.recent.set([]);

    if (this.normalizeCode(code) === 'vakif') {
      // VakıfBank: ayrı endpoint
      this.api.getVakifAccountList().subscribe({
        next: list => {
          console.log('[dashboard] vakif account list size:', list?.length ?? 0);
          this.vakifAccounts.set(list ?? []);
        },
        error: err => {
          console.error('[dashboard] vakif/account-list error:', err);
          this.vakifAccounts.set([]);
        },
      });
    } else {
      // MyBank ya da diğer bankalar
      this.api.getAccounts(code).subscribe({
        next: list => {
          console.log('[dashboard] accounts size:', list?.length ?? 0);
          this.accounts.set(list ?? []);
        },
        error: err => {
          console.error('[dashboard] accounts error:', err);
          this.accounts.set([]);
        },
      });
    }

    // ortak: son işlemler
    this.api.getRecentTransactions(code, 5).subscribe({
      next: tx => this.recent.set(tx ?? []),
      error: err => {
        console.error('[dashboard] recent-transactions error:', err);
        this.recent.set([]);
      },
    });
  }

  loadBanks() {
    this.api.getLinkedBanks().subscribe({
      next: (list) => {
        this.banks.set(list);
        console.log('[dashboard] linked banks:', list);

        const preferred =
          list.find(b => this.normalizeCode(b.code) === 'mybank' && b.connected)?.code
          ?? list.find(b => this.normalizeCode(b.code) === 'vakif' && b.connected)?.code
          ?? list.find(b => b.connected)?.code
          ?? null;

        if (preferred != null) this.loadForBank(preferred);
      },
      error: err => console.error('[dashboard] getLinkedBanks error:', err),
    });
  }

  selectBank(code: BankCode) {
    this.loadForBank(code);
  }

  openAddBank() {
    const bank = (prompt('Hangi banka eklensin? (vakif / mybank)') || '').toLowerCase().trim();
    if (bank !== 'vakif' && bank !== 'mybank') return;

    this.api.linkNewBank(bank as BankCode).subscribe({
      next: () => this.loadBanks(),
      error: err => console.error('[dashboard] linkNewBank error:', err),
    });
  }

  logout() {
    this.auth.logout();
  }

  trackByIndex(i: number) { return i; }
}
