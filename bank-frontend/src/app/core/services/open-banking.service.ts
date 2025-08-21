import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import {
  AccountDto,
  BankDto,
  LinkBankRequest,
  TransactionDto,
  BankCode,
} from '../models/open-banking.models';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';

import { VakifAccountRow } from '../../features/vakifbank/models/vakif-account-row';
import { VbAccountDetail } from '../../features/vakifbank/models/vb-account-detail.model';

@Injectable({ providedIn: 'root' })
export class OpenBankingService {
  private http = inject(HttpClient);
  private readonly base = '/api/open-banking';

  // -------------------- helpers --------------------
  private toStr = (v: any, fallback = ''): string =>
    v === undefined || v === null ? fallback : String(v);

  private toNum = (v: any, fallback = 0): number => {
    if (v === undefined || v === null || v === '') return fallback;
    const n = Number(v);
    return Number.isFinite(n) ? n : fallback;
  };

  private toBool = (v: any, fallback = false): boolean => {
    if (typeof v === 'boolean') return v;
    if (typeof v === 'string') return ['true', '1', 'yes'].includes(v.toLowerCase());
    if (typeof v === 'number') return v !== 0;
    return fallback;
  };

  private toISODate = (v: any): string | null => {
    if (!v) return null;
    // v ISO değilse Date'e çevirmeyi dene
    const d = new Date(v);
    return isNaN(d.getTime()) ? this.toStr(v) : d.toISOString();
  };

  // -------------------- Banks --------------------
  getLinkedBanks(): Observable<BankDto[]> {
    return this.http.get<any[]>(`${this.base}/linked-banks`).pipe(
      map((list) =>
        (list ?? []).map((raw) => ({
          id: this.toStr(raw.id ?? raw.Id),
          name: this.toStr(raw.name ?? raw.Name),
          code: (raw.code ?? raw.Code) as BankCode,
          balanceTRY: this.toNum(
            raw.balanceTRY ?? raw.balanceTry ?? raw.BalanceTRY ?? raw.balance_trY
          ),
          connected: this.toBool(raw.connected ?? raw.isConnected ?? raw.linked ?? raw.Connected),
        }))
      )
    );
  }

  linkBank(payload: LinkBankRequest): Observable<BankDto> {
    return this.http.post<any>(`${this.base}/link`, payload).pipe(map(this.normalizeBank));
  }

  // kullanım kolaylığı
  linkNewBank(bank: BankCode): Observable<BankDto> {
    return this.linkBank({ bankCode: bank });
  }

  // -------------------- Accounts (genel) --------------------
  getAccounts(bank: BankCode): Observable<AccountDto[]> {
    const params = new HttpParams().set('bank', bank);
    return this.http.get<any[]>(`${this.base}/accounts`, { params }).pipe(
      map((list) =>
        (list ?? []).map((raw) => {
          const iban = this.toStr(raw.iban ?? raw.ibanMasked ?? raw.Iban ?? raw.IbanMasked);
          return {
            id: this.toStr(raw.id ?? raw.Id),
            bankCode: (raw.bankCode ?? raw.bank ?? raw.BankCode ?? raw.Bank) as BankCode,
            name: this.toStr(raw.name ?? raw.Name ?? 'Vadesiz Hesap'),
            iban,
            ibanMasked: this.toStr(raw.ibanMasked ?? raw.IbanMasked ?? iban),
            balance: this.toNum(raw.balance ?? raw.Balance),
            currency: this.toStr(raw.currency ?? raw.Currency ?? 'TRY'),
          } as AccountDto;
        })
      )
    );
  }

  /** OpenBanking /accounts listesinden ilk elemanı tek hesap gibi al */
  getSingleMyBankFromOpenBanking(): Observable<AccountDto | null> {
    return this.getAccounts('mybank').pipe(map((list) => (list?.length ? list[0] : null)));
  }

  // -------------------- VakıfBank: Account List (tablo) --------------------
  /**
   * API farklı şekillerde dönebilir:
   *  - DİZİ: [{...}, {...}]
   *  - NESNE: { data: { accounts: [...] } }
   * Hepsini normalize eder.
   */
  getVakifAccountList(): Observable<VakifAccountRow[]> {
    return this.http.get<any>(`${this.base}/vakif/account-list`).pipe(
      map((res) => {
        const list: any[] = Array.isArray(res)
          ? res
          : Array.isArray(res?.data?.accounts)
          ? res.data.accounts
          : Array.isArray(res?.accounts)
          ? res.accounts
          : [];

        return list.map((raw) => ({
          currency: this.toStr(raw.currency ?? raw.Currency ?? 'TL'),
          lastTransactionDate: (raw.lastTransactionDate ?? raw.LastTransactionDate)
            ? this.toStr(raw.lastTransactionDate ?? raw.LastTransactionDate)
            : undefined,
          status: this.toStr(raw.status ?? raw.Status ?? 'Aktif'),
          iban: this.toStr(raw.iban ?? raw.Iban),
          balance: this.toNum(raw.balance ?? raw.Balance),
          accountType: this.toStr(
            raw.accountType ?? raw.AccountType ?? raw.accountTypeName ?? raw.AccountTypeName ?? ''
          ),
          // 🔴 accountNumber anahtarını üret
          accountNumber: this.toStr(
            raw.accountNumber ?? raw.AccountNumber ?? raw.accountNo ?? raw.AccountNo
          ),
        })) as VakifAccountRow[];
      })
    );
  }

  /** Kolay isim: UI'da çağırmak istersen */
  getVakifAccountRows(): Observable<VakifAccountRow[]> {
    return this.getVakifAccountList();
  }

  // -------------------- VakıfBank: Detay & Hareketler --------------------
  /** Hesap detayları – backend'in querystring aldığı sürüm */
  getVakifAccountDetails(accountNumber: string): Observable<any> {
    const params = new HttpParams().set('accountNumber', accountNumber);
    return this.http.get<any>(`${this.base}/vakif/account-details`, { params });
  }

  /**
   * Hesap detayları (normalize edilmiş)
   * UI doğrudan bunu kullanırsa modal tüm alanları güvenle doldurur.
   */

getVakifAccountDetailsNormalized(accountNumber: string): Observable<VbAccountDetail> {
  const params = new HttpParams().set('accountNumber', accountNumber);

  return this.http.get<any>(`${this.base}/vakif/account-details`, { params }).pipe(
    map((res: any): VbAccountDetail => {
      // Postman: { Data: { AccountInfo: { ... } } }
      const info = res?.Data?.AccountInfo ?? {};

      // Durum
      const status = String(info.AccountStatus ?? '').toUpperCase();
      const hesapDurumu: 'A' | 'K' = status === 'K' ? 'K' : 'A';

      // Tür
      const t = Number(info.AccountType ?? 0);
      const hesapTuru: 1 | 2 | 3 | 4 = ([1,2,3,4] as const).includes(t as any) ? (t as any) : 2;

      // Tarihler -> Date (DatePipe daha stabil işler)
      const acilisIso = String(info.OpeningDate ?? '');              // "2022-09-14T00:00:00"
      const sonIslemIso = String(info.LastTransactionDate ?? '');    // "2025-04-09T16:00:01"

      return {
        paraBirimi:     String(info.CurrencyCode ?? 'TL'),
        sonIslemTarihi: sonIslemIso ? new Date(sonIslemIso) as any : '',
        hesapDurumu,
        acilisTarihi:   acilisIso ? new Date(acilisIso) as any : '',
        iban:           String(info.IBAN ?? ''),
        musteriNo:      String(info.CustomerNumber ?? ''),
        bakiye:         this.toNum(info.RemainingBalance ?? info.Balance, 0),
        hesapTuru,
        subeKodu:       String(info.BranchCode ?? ''),
        hesapNo:        String(info.AccountNumber ?? ''),
      };
    }),
    catchError(() =>
      of<VbAccountDetail>({
        paraBirimi: 'TL',
        sonIslemTarihi: '',
        hesapDurumu: 'A',
        acilisTarihi: '',
        iban: '',
        musteriNo: '',
        bakiye: 0,
        hesapTuru: 2,
        subeKodu: '',
        hesapNo: '',
      })
    )
  );
}







  /** Hesap hareketleri – tarih aralığı/limit opsiyonel */
  getVakifAccountTransactions(
    accountNumber: string,
    opts?: { from?: string; to?: string; take?: number }
  ): Observable<TransactionDto[]> {
    let params = new HttpParams().set('accountNumber', accountNumber);
    if (opts?.from) params = params.set('from', opts.from);
    if (opts?.to) params = params.set('to', opts.to);
    if (opts?.take) params = params.set('take', String(opts.take));
    return this.http.get<TransactionDto[]>(`${this.base}/vakif/account-transactions`, { params });
  }

  // -------------------- Recent Transactions (özet) --------------------
  getRecentTransactions(bank: BankCode, take = 5): Observable<TransactionDto[]> {
    const params = new HttpParams().set('bank', bank).set('take', String(take));
    return this.http.get<TransactionDto[]>(`${this.base}/recent-transactions`, { params });
  }

  // -------------------- private mappers --------------------
  private normalizeBank = (raw: any): BankDto => ({
    id: this.toStr(raw.id ?? raw.Id),
    name: this.toStr(raw.name ?? raw.Name),
    code: (raw.code ?? raw.Code) as BankCode,
    balanceTRY: this.toNum(raw.balanceTRY ?? raw.balanceTry ?? raw.BalanceTRY),
    connected: this.toBool(raw.connected ?? raw.isConnected ?? raw.linked ?? raw.Connected),
  });
}
