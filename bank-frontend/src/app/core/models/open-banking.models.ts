export type BankCode = 'vakif' | 'mybank';

export interface BankDto {
  id: string;
  name: string;        // "VakıfBank" | "MyBank"
  code: BankCode;
  balanceTRY: number;  // özet bakiye
  connected: boolean;  // bağlı mı
}

export interface AccountDto {
  id: string;
  bankCode: BankCode;
  name: string;
  iban: string;             // ✅ maskesiz IBAN (backend artık bunu döndürüyor)
  ibanMasked?: string;      // ✅ opsiyonel, gerekirse tutulur
  balance: number;
  currency: string;
}

export interface TransactionDto {
  id: string;
  bankCode: BankCode;
  date: string;        // ISO string
  description: string;
  amount: number;      // + gelir, - gider
  currency: 'TRY' | 'USD' | 'EUR';
}

export interface LinkBankRequest {
  bankCode: BankCode;
}
