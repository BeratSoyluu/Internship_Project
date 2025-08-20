import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import {
  AccountDto,
  BankDto,
  LinkBankRequest,
  TransactionDto,
  BankCode,
} from '../models/open-banking.models';
import { Observable, map } from 'rxjs';

/** VakıfBank tablo satırı tipi (TOP-LEVEL, class dışı) */
export interface VakifAccountRow {
  currency: string;
  lastTransactionDate?: string; // ISO string gelebilir
  status?: string;
  iban: string;
  balance: number;
  accountType: string;
  accountNumber: string;
}

@Injectable({ providedIn: 'root' })
export class OpenBankingService {
  private http = inject(HttpClient);
  private readonly base = '/api/open-banking';

  // -------- Banks --------
  getLinkedBanks(): Observable<BankDto[]> {
    return this.http.get<any[]>(`${this.base}/linked-banks`).pipe(
      map(list =>
        list.map(raw => ({
          id: String(raw.id ?? raw.Id),
          name: String(raw.name ?? raw.Name),
          code: (raw.code ?? raw.Code) as BankCode,
          balanceTRY: Number(
            raw.balanceTRY ?? raw.balanceTry ?? raw.BalanceTRY ?? raw.balance_trY ?? 0
          ),
          connected: Boolean(
            raw.connected ?? raw.isConnected ?? raw.linked ?? raw.Connected ?? false
          ),
        }))
      )
    );
  }

  linkBank(payload: LinkBankRequest): Observable<BankDto> {
    return this.http.post<any>(`${this.base}/link`, payload).pipe(
      map(this.normalizeBank)
    );
  }

  // kullanım kolaylığı
  linkNewBank(bank: BankCode): Observable<BankDto> {
    return this.linkBank({ bankCode: bank });
  }

  // -------- Accounts --------
  getAccounts(bank: BankCode): Observable<AccountDto[]> {
    const params = new HttpParams().set('bank', bank);
    return this.http.get<any[]>(`${this.base}/accounts`, { params }).pipe(
      map(list =>
        list.map(raw => {
          const iban = String(
            raw.iban ?? raw.ibanMasked ?? raw.Iban ?? raw.IbanMasked ?? ''
          );
          return {
            id: String(raw.id ?? raw.Id),
            bankCode: (raw.bankCode ?? raw.bank ?? raw.BankCode ?? raw.Bank) as BankCode,
            name: String(raw.name ?? raw.Name ?? 'Vadesiz Hesap'),
            iban,
            ibanMasked: String(raw.ibanMasked ?? raw.IbanMasked ?? iban),
            balance: Number(raw.balance ?? raw.Balance ?? 0),
            currency: String(raw.currency ?? raw.Currency ?? 'TRY'),
          } as AccountDto;
        })
      )
    );
  }

  // Alternatif: OpenBanking /accounts listesinden ilk elemanı tek hesap gibi al
  getSingleMyBankFromOpenBanking(): Observable<AccountDto | null> {
    return this.getAccounts('mybank').pipe(
      map(list => (list?.length ? list[0] : null))
    );
  }

  // -------- VakıfBank: Account List (tablo için) --------
  getVakifAccountList(): Observable<VakifAccountRow[]> {
    return this.http.get<any[]>(`${this.base}/vakif/account-list`).pipe(
      map(list =>
        list.map(raw => ({
          currency: String(raw.currency ?? raw.Currency ?? 'TL'),
          lastTransactionDate:
            (raw.lastTransactionDate ?? raw.LastTransactionDate) ? String(raw.lastTransactionDate ?? raw.LastTransactionDate) : undefined,
          status: String(raw.status ?? raw.Status ?? 'Aktif'),
          iban: String(raw.iban ?? raw.Iban ?? ''),
          balance: Number(raw.balance ?? raw.Balance ?? 0),
          accountType: String(raw.accountType ?? raw.AccountType ?? ''),
          accountNumber: String(raw.accountNumber ?? raw.AccountNumber ?? ''),
        }))
      )
    );
  }

  // -------- Transactions --------
  getRecentTransactions(bank: BankCode, take = 5): Observable<TransactionDto[]> {
    const params = new HttpParams()
      .set('bank', bank)
      .set('take', String(take));
    return this.http.get<TransactionDto[]>(`${this.base}/recent-transactions`, { params });
  }

  // --- helpers ---
  private normalizeBank = (raw: any): BankDto => ({
    id: String(raw.id ?? raw.Id),
    name: String(raw.name ?? raw.Name),
    code: (raw.code ?? raw.Code) as BankCode,
    balanceTRY: Number(raw.balanceTRY ?? raw.balanceTry ?? raw.BalanceTRY ?? 0),
    connected: Boolean(raw.connected ?? raw.isConnected ?? raw.linked ?? raw.Connected ?? false),
  });
}
