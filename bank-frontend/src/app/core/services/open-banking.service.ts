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

@Injectable({ providedIn: 'root' })
export class OpenBankingService {
  private http = inject(HttpClient);
  private readonly base = '/api/open-banking';

  getLinkedBanks(): Observable<BankDto[]> {
    return this.http.get<any[]>(`${this.base}/linked-banks`).pipe(
      map(list => list.map(this.normalizeBank))
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

  getAccounts(bank: BankCode): Observable<AccountDto[]> {
    const params = new HttpParams().set('bank', bank);
    return this.http.get<AccountDto[]>(`${this.base}/accounts`, { params });
  }

  getRecentTransactions(bank: BankCode, take = 5): Observable<TransactionDto[]> {
    const params = new HttpParams()
      .set('bank', bank)
      .set('take', String(take));
    return this.http.get<TransactionDto[]>(`${this.base}/recent-transactions`, { params });
  }

  // --- helpers ---
  private normalizeBank = (raw: any): BankDto => ({
    id: String(raw.id),
    name: String(raw.name),
    code: raw.code as BankCode,
    balanceTRY: Number(raw.balanceTRY ?? raw.balanceTry ?? 0),
    connected: Boolean(raw.connected ?? raw.isConnected ?? raw.linked ?? false),
  });
}
