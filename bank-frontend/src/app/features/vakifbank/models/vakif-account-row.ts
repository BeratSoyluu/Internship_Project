// src/app/features/vakifbank/models/vakif-account-row.ts
export interface VakifAccountRow {
  accountNumber: string;
  iban: string;
  balance: number;
  currency: string;
  lastTransactionDate?: string;
  status?: string;
  accountType?: string | number;
  branchCode?: string;        // ✅ eklendi
}
