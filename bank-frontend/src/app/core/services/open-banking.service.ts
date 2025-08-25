import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import {
  AccountDto,
  BankDto,
  LinkBankRequest,
  TransactionDto,
  BankCode,
  CreateMyBankAccountDto,
  CurrencyCode,
} from '../models/open-banking.models';
import { Observable, of } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';

import { VakifAccountRow } from '../../features/vakifbank/models/vakif-account-row';
import { VbAccountDetail } from '../../features/vakifbank/models/vb-account-detail.model';
import { VbTransaction } from '../../features/vakifbank/models/vb-transaction.model';

@Injectable({ providedIn: 'root' })
export class OpenBankingService {
  private http = inject(HttpClient);

  // Proxy: /api -> http://localhost:5047
  private readonly base = '/api/open-banking';
  private readonly vakifBase = `${this.base}/vakif`;

  // Vakıf endpoint fallback'ları
  private readonly txUrlPrimary = `${this.vakifBase}/accountTransactions`;   // camelCase
  private readonly txUrlAlt     = `${this.vakifBase}/account-transactions`;  // kebab-case

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

  /** ISO'ya çevir (timezone yoksa +03:00 varsay) */
  private toISODate = (v: any): string | null => {
    if (!v) return null;
    const s = String(v);
    const noTz = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}$/.test(s);
    const s2 = noTz ? `${s}+03:00` : s;
    const d = new Date(s2);
    return isNaN(d.getTime()) ? this.toStr(v) : d.toISOString();
  };

  private pad(n: number) { return String(n).padStart(2, '0'); }
  private formatVbDate(value: string | Date, endOfDay = false): string {
    const d = value instanceof Date ? value : new Date(value);
    const y = d.getFullYear();
    const m = this.pad(d.getMonth() + 1);
    const day = this.pad(d.getDate());
    const hh = endOfDay ? '23' : '00';
    const mi = endOfDay ? '59' : '00';
    const ss = endOfDay ? '59' : '00';
    return `${y}-${m}-${day}T${hh}:${mi}:${ss}+03:00`;
  }

  // Tekilleştirilmiş normalizer
  private normalizeAccount = (raw: any): AccountDto => {
    const iban = this.toStr(raw.iban ?? raw.ibanMasked ?? raw.Iban ?? raw.IbanMasked);
    return {
      id: this.toStr(raw.id ?? raw.Id),
      bankCode: (raw.bankCode ?? raw.bank ?? raw.BankCode ?? raw.Bank) as BankCode,
      name: this.toStr(raw.name ?? raw.Name ?? 'Vadesiz Hesap'),
      iban,
      ibanMasked: this.toStr(raw.ibanMasked ?? raw.IbanMasked ?? iban),
      balance: this.toNum(raw.balance ?? raw.Balance),
      currency: (this.toStr(raw.currency ?? raw.Currency ?? 'TRY') as CurrencyCode),
    };
  };

  private normalizeBank = (raw: any): BankDto => ({
    id: this.toStr(raw.id ?? raw.Id),
    name: this.toStr(raw.name ?? raw.Name),
    code: (raw.code ?? raw.Code) as BankCode,
    balanceTRY: this.toNum(raw.balanceTRY ?? raw.balanceTry ?? raw.BalanceTRY ?? raw.balance_trY),
    connected: this.toBool(raw.connected ?? raw.isConnected ?? raw.linked ?? raw.Connected),
  });

  // -------------------- Banks --------------------
  getLinkedBanks(): Observable<BankDto[]> {
    return this.http.get<any[]>(`${this.base}/linked-banks`).pipe(
      map((list) => (list ?? []).map(this.normalizeBank))
    );
  }

  linkBank(payload: LinkBankRequest): Observable<BankDto> {
    return this.http.post<any>(`${this.base}/link`, payload).pipe(map(this.normalizeBank));
  }

  linkNewBank(bank: BankCode): Observable<BankDto> {
    return this.linkBank({ bankCode: bank });
  }

  // -------------------- Accounts (genel) --------------------
  getAccounts(bank: BankCode): Observable<AccountDto[]> {
    const params = new HttpParams().set('bank', bank);
    return this.http.get<any[]>(`${this.base}/accounts`, { params }).pipe(
      map((list) => (list ?? []).map(this.normalizeAccount))
    );
  }

  // MyBank için tek hesap örneği (gerekirse)
  getSingleMyBankFromOpenBanking(): Observable<AccountDto | null> {
    return this.getAccounts('mybank').pipe(map((list) => (list?.length ? list[0] : null)));
  }

  // -------------------- MyBank: Yeni hesap oluştur (DB'ye kaydet) --------------------
  createMyBankAccount(payload: CreateMyBankAccountDto): Observable<AccountDto> {
    const body = {
      name: payload.name,
      Name: payload.name,
      currency: payload.currency,
      Currency: payload.currency,
      bankCode: 'mybank',
      BankCode: 'mybank',
    };

    const try1 = this.http.post<any>(`${this.base}/mybank/accounts`, body).pipe(map(this.normalizeAccount));
    const try2 = this.http.post<any>(`${this.base}/mybank/create-account`, body).pipe(map(this.normalizeAccount));
    const try3 = this.http.post<any>(`${this.base}/accounts`, body, {
      params: new HttpParams().set('bank', 'mybank'),
    }).pipe(map(this.normalizeAccount));

    return try1.pipe(
      catchError(() => try2),
      catchError(() => try3)
    );
  }

  // -------------------- VakıfBank: Account List --------------------
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
          accountType: this.toStr(raw.accountType ?? raw.AccountType ?? raw.accountTypeName ?? raw.AccountTypeName ?? ''),
          accountNumber: this.toStr(raw.accountNumber ?? raw.AccountNumber ?? raw.accountNo ?? raw.AccountNo),
        })) as VakifAccountRow[];
      })
    );
  }

  getVakifAccountRows(): Observable<VakifAccountRow[]> {
    return this.getVakifAccountList();
  }

  // -------------------- VakıfBank: Detay & Hareketler --------------------
  getVakifAccountDetails(accountNumber: string): Observable<any> {
    const params = new HttpParams().set('accountNumber', accountNumber);
    return this.http.get<any>(`${this.base}/vakif/account-details`, { params });
  }

  getVakifAccountDetailsNormalized(accountNumber: string): Observable<VbAccountDetail> {
    const params = new HttpParams().set('accountNumber', accountNumber);
    return this.http.get<any>(`${this.base}/vakif/account-details`, { params }).pipe(
      map((res: any): VbAccountDetail => {
        const info = res?.Data?.AccountInfo ?? {};
        const status = String(info.AccountStatus ?? '').toUpperCase();
        const hesapDurumu: 'A' | 'K' = status === 'K' ? 'K' : 'A';
        const t = Number(info.AccountType ?? 0);
        const hesapTuru: 1 | 2 | 3 | 4 = ([1, 2, 3, 4] as const).includes(t as any) ? (t as any) : 2;

        const acilisIso = String(info.OpeningDate ?? '');
        const sonIslemIso = String(info.LastTransactionDate ?? '');

        return {
          paraBirimi: String(info.CurrencyCode ?? 'TL'),
          sonIslemTarihi: sonIslemIso ? (new Date(sonIslemIso) as any) : '',
          hesapDurumu,
          acilisTarihi: acilisIso ? (new Date(acilisIso) as any) : '',
          iban: String(info.IBAN ?? ''),
          musteriNo: String(info.CustomerNumber ?? ''),
          bakiye: this.toNum(info.RemainingBalance ?? info.Balance, 0),
          hesapTuru,
          subeKodu: String(info.BranchCode ?? ''),
          hesapNo: String(info.AccountNumber ?? ''),
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

  // -------------------- VakıfBank: Hareketler --------------------
  getVakifAccountTransactions(
    accountNumber: string,
    opts?: { from?: string | Date; to?: string | Date; take?: number }
  ): Observable<TransactionDto[]> {
    const now = new Date();
    const fromDef = new Date(now);
    fromDef.setMonth(fromDef.getMonth() - 1);

    const body: any = {
      AccountNumber: accountNumber,
      StartDate: this.formatVbDate(opts?.from ?? fromDef, false),
      EndDate: this.formatVbDate(opts?.to ?? now, true),
    };

    const tryPost = (url: string) =>
      this.http.post<TransactionDto[]>(url, body).pipe(
        tap({
          next: res => console.log('[VB TX] POST', url, res),
          error: err => console.log('[VB TX] ERROR', url, err),
        })
      );

    return tryPost(this.txUrlPrimary).pipe(
      catchError(() => of([] as TransactionDto[]))
    );
  }

  getVakifAccountTransactionsNormalized(
    accountNumber: string,
    opts?: { from?: string | Date; to?: string | Date; take?: number }
  ): Observable<VbTransaction[]> {
    const pad = (n: number) => String(n).padStart(2, '0');
    const toApiDate = (v: string | Date, endOfDay = false): string => {
      const d = new Date(v);
      if (endOfDay) d.setHours(23,59,59,0); else d.setHours(0,0,0,0);
      const yyyy = d.getFullYear(), MM = pad(d.getMonth()+1), dd = pad(d.getDate());
      const HH = pad(d.getHours()),  mm = pad(d.getMinutes()), ss = pad(d.getSeconds());
      return `${yyyy}-${MM}-${dd}T${HH}:${mm}:${ss}+03:00`;
    };

    const now = new Date();
    const fromDef = new Date(now); fromDef.setMonth(fromDef.getMonth() - 1);

    const body: any = {
      AccountNumber: accountNumber,
      StartDate: toApiDate(opts?.from ?? fromDef, false),
      EndDate:   toApiDate(opts?.to   ?? now,     true),
    };

    const mapResponse = (res: any): VbTransaction[] => {
      const list: any[] =
        Array.isArray(res) ? res :
        Array.isArray(res?.Data?.AccountTransactions) ? res.Data.AccountTransactions :
        Array.isArray(res?.Data?.Transactions)        ? res.Data.Transactions :
        Array.isArray(res?.transactions)              ? res.transactions :
        Array.isArray(res?.Transactions)              ? res.Transactions : [];

      return list.map((raw: any, idx: number) => {
        const toNum = (x:any, f=0)=>{ const n=Number(x); return Number.isFinite(n)?n:f; };
        const toStr = (x:any, f='')=> (x==null?f:String(x));
        const txId = toStr(raw.TransactionId ?? raw.transactionId ?? raw.Id ?? raw.id ?? '', `idx-${idx}`);
        const dateRaw = raw.TransactionDate ?? raw.transactionDate ?? raw.TransactionDateTime ?? raw.transactionDateTime ?? '';
        const debit  = toNum(raw.Debit  ?? raw.debit,  0);
        const credit = toNum(raw.Credit ?? raw.credit, 0);
        let amount   = toNum(raw.Amount ?? raw.amount ?? raw.TransactionAmount ?? raw.transactionAmount, 0);
        if (!amount && (debit || credit)) amount = credit - debit;

        return {
          transactionId:   txId,
          transactionDate: this.toISODate(dateRaw) ?? toStr(dateRaw),
          description:     toStr(raw.Description ?? raw.description ?? ''),
          amount,
          currency:        toStr(raw.CurrencyCode ?? raw.currencyCode ?? raw.Currency ?? raw.currency ?? 'TRY'),
          transactionCode: toStr(raw.TransactionCode ?? raw.transactionCode ?? ''),
          balance:         toNum(raw.Balance ?? raw.balance ?? raw.RemainingBalance ?? raw.remainingBalance, 0),
          transactionName: toStr(raw.TransactionName ?? raw.transactionName ?? ''),
        } as VbTransaction;
      });
    };

    const postAndLog = (url: string) =>
      this.http.post<any>(url, body).pipe(
        map(mapResponse),
        catchError(() => of([] as VbTransaction[]))
      );

    return postAndLog(this.txUrlPrimary).pipe(
      catchError(() => postAndLog(this.txUrlAlt))
    );
  }

  // -------------------- Recent Transactions (genel özet) --------------------
  getRecentTransactions(bank: BankCode, take = 5): Observable<TransactionDto[]> {
    const params = new HttpParams().set('bank', bank).set('take', String(take));
    return this.http.get<TransactionDto[]>(`${this.base}/recent-transactions`, { params });
  }

  // -------------------- MyBank: Recent Transactions --------------------
  getMyBankRecent(take = 10, skip = 0) {
    return this.http.get<{ total: number; items: RecentTxDto[] }>(
      '/api/mybank/transactions/recent',
      { params: { take, skip } as any }
    );
  }

  // -------------------- MyBank: Para Transferi --------------------
  /** MyBank transfer: isim + IBAN + tutar backend'e gider */
  createMyBankTransfer(body: TransferCreate) {
    // Bu endpoint open-banking altında değil; o yüzden absolute path kullandık.
    return this.http.post<TransferDto>('/api/mybank/transfers', body);
  }
}

// DTO arayüzü
export interface RecentTxDto {
  id: number;
  accountId: number;
  accountName: string;
  transactionDate: string; // ISO string
  description?: string;
  direction: 'IN' | 'OUT';
  amount: number;
  balanceAfter?: number;
  currency: string;
}

// === MyBank transfer DTO'ları ===
export interface TransferCreate {
  toIban: string;
  toName: string;
  amount: number;
  description?: string | null;
}

export interface TransferDto {
  id: number;
  fromAccountId: number;
  fromAccountNumber: string;
  toIban: string;
  toName: string;
  amount: number;
  currency: string;
  description?: string | null;
  status: string;
  bankReference?: string | null;
  requestedAt: string;
  completedAt?: string | null;
}
