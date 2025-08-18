import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AccountDto, BankDto, LinkBankRequest, TransactionDto, BankCode } from '../models/open-banking.models';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class OpenBankingService {
  private http = inject(HttpClient);
  // İstersen environment'tan oku:
  private base = '/api/open-banking';

  getLinkedBanks(): Observable<BankDto[]> {
    return this.http.get<BankDto[]>(`${this.base}/linked-banks`);
  }

  linkBank(payload: LinkBankRequest): Observable<BankDto> {
    return this.http.post<BankDto>(`${this.base}/link`, payload);
  }

  getAccounts(bank: BankCode): Observable<AccountDto[]> {
    return this.http.get<AccountDto[]>(`${this.base}/accounts`, { params: { bank } });
  }

  getRecentTransactions(bank: BankCode, take = 5): Observable<TransactionDto[]> {
    return this.http.get<TransactionDto[]>(`${this.base}/recent-transactions`, { params: { bank, take } as any });
  }
}
