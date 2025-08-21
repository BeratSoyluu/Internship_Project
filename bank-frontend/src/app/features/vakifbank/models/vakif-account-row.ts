export interface VakifAccountRow {
  currency: string;
  lastTransactionDate?: string;
  status?: string;
  iban: string;
  balance: number;
  accountType: string;
  accountNumber: string;
}
