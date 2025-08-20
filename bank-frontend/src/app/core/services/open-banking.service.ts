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

  // -------- Banks --------
  getLinkedBanks(): Observable<BankDto[]> {
  return this.http.get<any[]>(`${this.base}/linked-banks`).pipe(
    map(list => list.map(raw => ({
      id: String(raw.id ?? raw.Id),
      name: String(raw.name ?? raw.Name),
      code: (raw.code ?? raw.Code) as BankCode,
      balanceTRY: Number(
        raw.balanceTRY ?? raw.balanceTry ?? raw.BalanceTRY ?? raw.balance_trY ?? 0
      ),
      connected: Boolean(raw.connected ?? raw.isConnected ?? raw.linked ?? raw.Connected ?? false),
    })))
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
    map(list => list.map(raw => {
      const iban = String(raw.iban ?? raw.ibanMasked ?? raw.Iban ?? raw.IbanMasked ?? '');
      return {
        id: String(raw.id ?? raw.Id),
        bankCode: (raw.bankCode ?? raw.bank ?? raw.BankCode ?? raw.Bank) as BankCode,
        name: String(raw.name ?? raw.Name ?? 'Vadesiz Hesap'),
        iban,
        ibanMasked: String(raw.ibanMasked ?? raw.IbanMasked ?? iban),
        balance: Number(raw.balance ?? raw.Balance ?? 0),            // 👈 kritik
        currency: String(raw.currency ?? raw.Currency ?? 'TRY'),     // 👈 kritik
      } as AccountDto;
    }))
  );
}

  // Alternatif: OpenBanking /accounts listesinden ilk elemanı tek hesap gibi al
  getSingleMyBankFromOpenBanking(): Observable<AccountDto | null> {
    return this.getAccounts('mybank').pipe(
      map(list => (list?.length ? list[0] : null))
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
    id: String(raw.id),
    name: String(raw.name),
    code: raw.code as BankCode,
    balanceTRY: Number(raw.balanceTRY ?? raw.balanceTry ?? 0),
    connected: Boolean(raw.connected ?? raw.isConnected ?? raw.linked ?? false),
  });
}
