import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { OpenBankingService, RecentTxDto } from '../../core/services/open-banking.service';
import { VakifAccountRow } from '../../features/vakifbank/models/vakif-account-row';
import { AuthService } from '../../services/auth.service';
import {
  BankDto,
  BankCode,
  AccountDto,
  TransactionDto,
  CurrencyCode,
  CreateMyBankAccountDto,
} from '../../core/models/open-banking.models';
import { VbAccountDetail } from '../../features/vakifbank/models/vb-account-detail.model';
import { VbTransaction } from '../../features/vakifbank/models/vb-transaction.model';

// 🔽 Modal bileşeni (yolunu projenizdeki konuma göre güncelleyin)
import { AccountDetailModalComponent } from '../../shared/account-detail-modal.component';

type AddForm = {
  name: string;
  currency: CurrencyCode;
};

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, AccountDetailModalComponent],
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

  // VakıfBank için eski TransactionDto listesi
  recent = signal<TransactionDto[]>([]);

  // ✅ MyBank için özel “recent” listesi
  myRecent = signal<RecentTxDto[]>([]);
  myRecentTotal = signal(0);

  // =========================
  // Döviz / Altın -> TL kurları (örnek değerler)
  // =========================
  // İleride bunları API'den çekebilirsin. Şimdilik sabit.
  private rates: Record<string, number> = {
    TRY: 1,
    USD: 41,   // 1 USD = 33 TRY
    EUR: 18,   // 1 EUR = 36 TRY
    XAU: 4548 , // 1 Gram Altın = 2500 TRY
    GBP: 55,   // ihtiyaç olursa
  };

  // Tutarı verilen para biriminden TL'ye çevirir
  toTry(amount: number | undefined | null, currency?: string | null): number {
    const cur = (currency || 'TRY').toUpperCase();
    const rate = this.rates[cur] ?? 1;
    return (Number(amount) || 0) * rate;
  }

  // MyBank toplamını TL cinsinden hesaplar
  myBankTotal = computed(() => {
    let total = 0;
    for (const acc of this.accounts()) {
      const cur = (acc.currency || 'TRY').toUpperCase();
      const rate = this.rates[cur] ?? 1;
      total += (acc.balance ?? 0) * rate;
    }
    return total;
  });

  // =========================
  // Detay Modali
  // =========================
  detailOpen = signal(false);
  // VakıfBank detay modeli
  vbDetail = signal<VbAccountDetail | null>(null);
  // MyBank detay modeli (AccountDto'yu göstereceğiz)
  myDetail = signal<AccountDto | null>(null);

  // =========================
  // Hareketler Modali (Vakıf)
  // =========================
  txOpen = signal(false);
  txLoading = signal(false);
  txError = signal<string | null>(null);
  txItems = signal<VbTransaction[]>([]);
  txAccNo = signal<string>('');
  txFrom = signal<string>(''); // 'YYYY-MM-DD'
  txTo = signal<string>('');   // 'YYYY-MM-DD'
  private txExpanded = signal<Record<string, boolean>>({});

  // =========================
  // Banka Kodları
  // =========================
  vakifMi = computed(() => this.normalizeCode(this.selectedBank()) === 'vakif');
  mybankMi = computed(() => this.normalizeCode(this.selectedBank()) === 'mybank');

  public isMyBank(code: BankCode | string | number): boolean {
    return this.normalizeCode(code) === 'mybank';
  }
  public isVakif(code: BankCode | string | number): boolean {
    return this.normalizeCode(code) === 'vakif';
  }

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
    this.myRecent.set([]);
    this.myRecentTotal.set(0);

    // modal state temizle
    this.vbDetail.set(null);
    this.myDetail.set(null);
    this.detailOpen.set(false);

    if (this.normalizeCode(code) === 'vakif') {
      this.api.getVakifAccountList().subscribe({
        next: list => this.vakifAccounts.set(list ?? []),
        error: _ => this.vakifAccounts.set([]),
      });

      // VakıfBank tarafı: eski endpoint
      this.api.getRecentTransactions(code, 5).subscribe({
        next: tx => this.recent.set(tx ?? []),
        error: _ => this.recent.set([]),
      });
    }
    else if (this.normalizeCode(code) === 'mybank') {
      this.api.getAccounts(code).subscribe({
        next: list => this.accounts.set(list ?? []),
        error: _ => this.accounts.set([]),
      });

      // ✅ MyBank tarafı: yeni endpoint
      this.api.getMyBankRecent(5, 0).subscribe({
        next: res => {
          this.myRecent.set(res.items ?? []);
          this.myRecentTotal.set(res.total ?? 0);
        },
        error: _ => {
          this.myRecent.set([]);
          this.myRecentTotal.set(0);
        }
      });
    }
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

  // ✅ Üstteki “+ Banka Ekle” butonu burada
  openAddBank() {
    const bank = (prompt('Hangi banka eklensin? (vakif / mybank)') || '').toLowerCase().trim();
    if (bank !== 'vakif' && bank !== 'mybank') return;

    this.api.linkNewBank(bank as BankCode).subscribe({
      next: () => this.loadBanks(),
      error: (err) => console.error('Banka eklenemedi', err)
    });
  }

  // =========================
  // DETAY MODALİ — VakıfBank
  // =========================
  openVbDetails(acc: VakifAccountRow) {
    this.myDetail.set(null);     // diğer tip temizlensin
    this.vbDetail.set(null);
    this.detailOpen.set(true);

    this.api.getVakifAccountDetailsNormalized(acc.accountNumber).subscribe({
      next: (d) => {
        const fallbackType = Number(acc.accountType);
        const merged: VbAccountDetail = {
          ...d,
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

  // =========================
  // DETAY MODALİ — MyBank
  // =========================
  openMyDetails(acc: AccountDto) {
    // MyBank için detayı doğrudan karttaki bilgiden gösterebiliriz
    // (istersen burada ayrıca /detail endpoint'i çağırabilirsin)
    this.vbDetail.set(null);     // diğer tip temizlensin
    this.myDetail.set(acc);
    this.detailOpen.set(true);
  }

  closeDetail() {
    this.detailOpen.set(false);
    this.vbDetail.set(null);
    this.myDetail.set(null);
  }

  // =========================
  // HAREKETLER MODALI (Vakıf)
  // =========================
  private ymd(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  private setQuickRange(preset: '7g'|'1a'|'3a'|'6a'|'1y'|'2y') {
    const now = new Date();
    const from = new Date(now);

    switch (preset) {
      case '7g': from.setDate(from.getDate() - 7); break;
      case '1a': from.setMonth(from.getMonth() - 1); break;
      case '3a': from.setMonth(from.getMonth() - 3); break;
      case '6a': from.setMonth(from.getMonth() - 6); break;
      case '1y': from.setFullYear(from.getFullYear() - 1); break;
      case '2y': from.setFullYear(from.getFullYear() - 2); break;
    }

    this.txFrom.set(this.ymd(from));
    this.txTo.set(this.ymd(now));
  }

  private loadTransactions() {
    const accNo = this.txAccNo();
    if (!accNo) return;

    this.txLoading.set(true);
    this.txError.set(null);
    this.txItems.set([]);
    this.txExpanded.set({});

    this.api.getVakifAccountTransactionsNormalized(accNo, {
      from: this.txFrom(),
      to:   this.txTo(),
      take: 100
    }).subscribe({
      next: (rows) => {
        this.txItems.set(rows ?? []);
        this.txLoading.set(false);
      },
      error: (err) => {
        console.error('[transactions]', err);
        this.txError.set('Hesap hareketleri alınamadı');
        this.txLoading.set(false);
      },
    });
  }

  openVbTransactions(acc: VakifAccountRow) {
    this.txAccNo.set(acc.accountNumber);
    this.setQuickRange('1a');
    this.txOpen.set(true);
    this.loadTransactions();
  }
  closeTx() { this.txOpen.set(false); }

  setTxRange7g() { this.setQuickRange('7g'); this.loadTransactions(); }
  setTxRange1a() { this.setQuickRange('1a'); this.loadTransactions(); }
  setTxRange3a() { this.setQuickRange('3a'); this.loadTransactions(); }
  setTxRange6a() { this.setQuickRange('6a'); this.loadTransactions(); }
  setTxRange1y() { this.setQuickRange('1y'); this.loadTransactions(); }
  setTxRange2y() { this.setQuickRange('2y'); this.loadTransactions(); }
  onTxFromChange(v: string) { this.txFrom.set(v); }
  onTxToChange(v: string)   { this.txTo.set(v); }
  reloadTx() { this.loadTransactions(); }
  toggleTx(id: string) {
    const map = { ...this.txExpanded() };
    map[id] = !map[id];
    this.txExpanded.set(map);
  }
  isExpanded(id: string): boolean { return !!this.txExpanded()[id]; }

  // =========================
  // Para Transferi (MyBank) — MODAL
  // =========================
  // Modal state & form
  private sanitizeIban(v: string) {
    return (v || '').replace(/\s+/g, '').toUpperCase();
  }

  transferOpen = signal(false);
  transferError = signal<string | null>(null);
  transferForm = signal<{ toName: string; toIban: string; amount: number; description?: string | null }>({
    toName: '',
    toIban: '',
    amount: 0,
    description: ''
  });

  openTransfer() {
    this.transferError.set(null);
    this.transferForm.set({ toName: '', toIban: '', amount: 0, description: '' });
    this.transferOpen.set(true);
  }
  closeTransfer() { this.transferOpen.set(false); }

  setTrName(v: string)     { this.transferForm.update(f => ({ ...f, toName: (v || '').trim() })); }
  setTrIban(v: string)     { this.transferForm.update(f => ({ ...f, toIban: this.sanitizeIban(v || '') })); }
  setTrAmount(v: any)      { const n = Number(v); this.transferForm.update(f => ({ ...f, amount: Number.isFinite(n) ? n : 0 })); }
  setTrDesc(v: string)     { this.transferForm.update(f => ({ ...f, description: (v ?? '') })); }

  private refreshMyBankSummaries() {
    // Hesaplar
    const code = this.selectedBank();
    if (code && this.normalizeCode(code) === 'mybank') {
      this.api.getAccounts(code).subscribe({
        next: list => this.accounts.set(list ?? []),
        error: _ => this.accounts.set([]),
      });
      // Son işlemler
      this.api.getMyBankRecent(5, 0).subscribe({
        next: res => {
          this.myRecent.set(res.items ?? []);
          this.myRecentTotal.set(res.total ?? 0);
        },
        error: _ => {
          this.myRecent.set([]);
          this.myRecentTotal.set(0);
        }
      });
    }
  }

  submitTransfer() {
    const form = this.transferForm();
    // Basit frontend validasyonları
    if (!form.toName || form.toName.trim().length < 2) {
      this.transferError.set('Alıcı adı giriniz.');
      return;
    }
    const iban = this.sanitizeIban(form.toIban);
    if (!iban || iban.length < 15 || iban.length > 34) {
      this.transferError.set('Geçerli bir IBAN giriniz.');
      return;
    }
    if (!form.amount || form.amount <= 0) {
      this.transferError.set('Tutar 0’dan büyük olmalı.');
      return;
    }

    this.transferError.set(null);

    this.api.createMyBankTransfer({
      toName: form.toName.trim(),
      toIban: iban,
      amount: Number(form.amount),
      description: form.description ?? null
    }).subscribe({
      next: _res => {
        // Başarılı
        this.closeTransfer();
        this.refreshMyBankSummaries();
      },
      error: err => {
        const msg = err?.error?.message || 'Transfer sırasında hata oluştu.';
        this.transferError.set(msg);
      }
    });
  }

  // (Eski) route tabanlı transfer tetikleyici — dursun, istersen silebilirsin
  transfer(fromAccountId?: string) {
    if (!this.mybankMi()) {
      const my = this.banks().find(b => this.normalizeCode(b.code) === 'mybank' && b.connected)?.code;
      if (my) this.selectBank(my);
    }
    this.router.navigate(['/mybank/transfers'], {
      queryParams: { from: fromAccountId || 'mybank' }
    });
  }

  // =========================
  // MyBank — Hesap Kartı Seçimi
  // =========================
  private mySelectedIban = signal<string | null>(null);
  isMyAccountSelected(iban?: string | null) { return !!iban && this.mySelectedIban() === iban; }
  selectMyAccount(acc: AccountDto) { this.mySelectedIban.set(acc?.iban ?? null); }

  // =========================
  // MyBank — Hesap Ekle
  // =========================
  addAccOpen = signal(false);
  addError = signal<string | null>(null);
  addForm = signal<AddForm>({ name: '', currency: 'TRY' });

  openAddMyAccount() {
    this.addError.set(null);
    this.addForm.set({ name: '', currency: 'TRY' });
    this.addAccOpen.set(true);
  }
  closeAddMyAccount() { this.addAccOpen.set(false); }

  setAddName(v: string)     { this.addForm.update(f => ({ ...f, name: v })); }
  setAddCurrency(v: string) { this.addForm.update(f => ({ ...f, currency: v as CurrencyCode })); }

  submitAddMyAccount() {
    const form = this.addForm();
    this.addError.set(null);

    const name = (form.name || '').trim();
    if (!name) { this.addError.set('Hesap adı giriniz.'); return; }
    if (!form.currency) { this.addError.set('Para birimi seçiniz.'); return; }

    const payload: CreateMyBankAccountDto = {
      name,
      currency: form.currency,
    };

    this.api.createMyBankAccount(payload).subscribe({
      next: (created) => {
        const bank = this.selectedBank();
        if (bank != null) {
          this.api.getAccounts(bank).subscribe({
            next: list => this.accounts.set(list ?? []),
            error: _ => this.accounts.set([]),
          });
        }
        this.mySelectedIban.set(created.iban);
        this.addForm.set({ name: '', currency: 'TRY' });
        this.addAccOpen.set(false);
      },
      error: () => this.addError.set('Hesap eklenemedi. Lütfen tekrar deneyin.')
    });
  }

  currencyIcon(code?: string | null): string {
    const c = (code || 'TRY').toUpperCase();
    if (c === 'USD') return '$';
    if (c === 'EUR') return '€';
    if (c === 'GBP') return '£';
    if (c === 'XAU') return '🥇';
    return '₺';
  }

  // =========================
  // Genel
  // =========================
  logout() { this.auth.logout(); }
  trackByIndex(i: number) { return i; }
}
