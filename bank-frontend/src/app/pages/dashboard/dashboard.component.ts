import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { Router } from '@angular/router';

import { OpenBankingService } from '../../core/services/open-banking.service';
import { VakifAccountRow } from '../../features/vakifbank/models/vakif-account-row';
import { AuthService } from '../../services/auth.service';
import { BankDto, BankCode, AccountDto, TransactionDto } from '../../core/models/open-banking.models';
import { VbAccountDetail } from '../../features/vakifbank/models/vb-account-detail.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, DatePipe, DecimalPipe],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css'],
})
export class DashboardComponent implements OnInit {
  private api = inject(OpenBankingService);
  private auth = inject(AuthService);
  private router = inject(Router);

  banks = signal<BankDto[]>([]);
  selectedBank = signal<BankCode | null>(null);
  accounts = signal<AccountDto[]>([]);
  vakifAccounts = signal<VakifAccountRow[]>([]);
  recent = signal<TransactionDto[]>([]);

  // Modal state
  detailOpen = signal(false);
  vbDetail = signal<VbAccountDetail | null>(null);

  vakifMi = computed(() => this.normalizeCode(this.selectedBank()) === 'vakif');

  private normalizeCode(code: any): string {
    if (code === 0 || String(code).toLowerCase() === 'vakif') return 'vakif';
    if (code === 1 || String(code).toLowerCase() === 'mybank') return 'mybank';
    return String(code ?? '').toLowerCase();
  }

  mapStatus(code?: 'A' | 'K' | string | null): string {
    const c = (code || '').toString().toUpperCase();
    if (c === 'A') return 'Açık';
    if (c === 'K') return 'Kapalı';
    return '-';
  }
  mapAccountType(code?: number | string | null): string {
    const n = Number(code ?? 0);
    switch (n) {
      case 1: return 'Vadeli Türk Parası Mevduat Hesabı';
      case 2: return 'Vadesiz Türk Parası Mevduat Hesabı';
      case 3: return 'Vadeli Yabancı Para Mevduat Hesabı';
      case 4: return 'Vadesiz Yabancı Para Mevduat Hesabı';
      default: return '-';
    }
  }

  selectedBankName = computed(() => {
    const raw = this.selectedBank();
    const norm = this.normalizeCode(raw);
    const found = this.banks().find(b => this.normalizeCode(b.code) === norm);
    return found?.name ?? (norm ? norm : '');
  });

  ngOnInit() { this.loadBanks(); }

  private loadForBank(code: BankCode) {
    if (this.selectedBank() === code) return;

    this.selectedBank.set(code);
    this.accounts.set([]);
    this.vakifAccounts.set([]);
    this.recent.set([]);

    if (this.normalizeCode(code) === 'vakif') {
      this.api.getVakifAccountList().subscribe({
        next: list => this.vakifAccounts.set(list ?? []),
        error: _ => this.vakifAccounts.set([]),
      });
    } else {
      this.api.getAccounts(code).subscribe({
        next: list => this.accounts.set(list ?? []),
        error: _ => this.accounts.set([]),
      });
    }

    this.api.getRecentTransactions(code, 5).subscribe({
      next: tx => this.recent.set(tx ?? []),
      error: _ => this.recent.set([]),
    });
  }

  loadBanks() {
    this.api.getLinkedBanks().subscribe({
      next: (list) => {
        this.banks.set(list ?? []);
        const preferred =
          list.find(b => this.normalizeCode(b.code) === 'mybank' && b.connected)?.code
          ?? list.find(b => this.normalizeCode(b.code) === 'vakif' && b.connected)?.code
          ?? list.find(b => b.connected)?.code
          ?? null;
        if (preferred != null) this.loadForBank(preferred);
      },
      error: err => console.error(err),
    });
  }

  selectBank(code: BankCode) { this.loadForBank(code); }

  openAddBank() {
    const bank = (prompt('Hangi banka eklensin? (vakif / mybank)') || '').toLowerCase().trim();
    if (bank !== 'vakif' && bank !== 'mybank') return;
    this.api.linkNewBank(bank as BankCode).subscribe({ next: () => this.loadBanks() });
  }

  // DETAY MODALI — normalize edilmiş servisi kullan
  openVbDetails(acc: VakifAccountRow) {
  this.vbDetail.set(null);
  this.detailOpen.set(true);

  this.api.getVakifAccountDetailsNormalized(acc.accountNumber).subscribe({
    next: (d) => {
      const fallbackType = Number(acc.accountType);
      const merged: VbAccountDetail = {
        ...d,
        // boş/0 ise satırdan doldur
        paraBirimi:     d.paraBirimi || acc.currency,
        sonIslemTarihi: d.sonIslemTarihi || acc.lastTransactionDate,
        iban:           d.iban || acc.iban,
        bakiye:         d.bakiye && d.bakiye !== 0 ? d.bakiye : (acc.balance ?? 0),
        hesapTuru:      (d.hesapTuru && [1,2,3,4].includes(+d.hesapTuru as any))
                         ? d.hesapTuru
                         : (([1,2,3,4].includes(fallbackType as any) ? fallbackType : 2) as 1|2|3|4),
        hesapNo:        d.hesapNo || acc.accountNumber,
      };
      this.vbDetail.set(merged);
    },
    error: () => {
      this.vbDetail.set({
        paraBirimi: acc.currency || 'TL',
        sonIslemTarihi: acc.lastTransactionDate || '',
        hesapDurumu: 'A',
        acilisTarihi: '',
        iban: acc.iban || '',
        musteriNo: '',
        bakiye: acc.balance ?? 0,
        hesapTuru: ([1,2,3,4].includes(Number(acc.accountType) as any) ? Number(acc.accountType) : 2) as 1|2|3|4,
        subeKodu: '',
        hesapNo: acc.accountNumber,
      });
    }
  });
}



  closeDetail() { this.detailOpen.set(false); }

  openVbTransactions(acc: VakifAccountRow) {
    this.router.navigate(['/vakifbank/accounts', acc.accountNumber, 'transactions']);
  }

  logout() { this.auth.logout(); }
  trackByIndex(i: number) { return i; }
}
