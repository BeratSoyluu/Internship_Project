// src/app/features/vakifbank/models/vb-transaction.model.ts
export type VbTransaction = {
  transactionId?: string;
  transactionDate?: string | Date;
  description?: string;
  amount?: number;
  currency?: string;
  transactionCode?: string;
  balance?: number;
  transactionName?: string;
};
