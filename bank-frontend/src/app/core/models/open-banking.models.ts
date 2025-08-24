// Bank kodları
export type BankCode = 'vakif' | 'mybank';

// Ortak para birimi tipi
export type CurrencyCode = 'TRY' | 'USD' | 'EUR' | 'GBP' | 'XAU';

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
  name: string;        // kart başlığı / hesap adı
  iban: string;        // maskesiz IBAN (backend döndürür)
  ibanMasked?: string; // opsiyonel maske
  balance: number;
  currency: CurrencyCode;
}

export interface TransactionDto {
  id: string;
  bankCode: BankCode;
  date: string;          // ISO string
  description: string;
  amount: number;        // + gelir, - gider
  currency: CurrencyCode;
}

export interface LinkBankRequest {
  bankCode: BankCode;
}

export interface CreateMyBankAccountDto {
  name: string;
  currency: CurrencyCode;
}


