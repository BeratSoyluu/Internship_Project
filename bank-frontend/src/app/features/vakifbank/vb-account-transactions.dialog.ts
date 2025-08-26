import { Component, Inject } from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { VbTransaction } from './models/vb-transaction.model';

@Component({
  standalone: true,
  selector: 'vb-account-transactions-dialog',
  imports: [CommonModule, DatePipe, DecimalPipe],
  template: `
    <div class="vb-modal">
      <h2 class="vb-title">Hesap Hareketleri</h2>

      <div class="tx-list">
        <div class="tx-pill"
             *ngFor="let tx of data.transactions"
             [ngClass]="{ 'tx-in': (tx.amount ?? 0) > 0, 'tx-out': (tx.amount ?? 0) < 0 }">
          <div class="tx-date">{{ tx.transactionDate | date:'yyyy-MM-dd HH:mm:ss' }}</div>
          <div class="tx-desc">{{ tx.description || 'İşlem' }}</div>
          <div class="tx-amount">{{ tx.amount | number:'1.2-2' }} {{ tx.currency || 'TRY' }}</div>
        </div>
      </div>

      <div class="vb-actions">
        <button class="vb-btn" (click)="close()">Kapat</button>
      </div>
    </div>
  `,
  styles: [`
    .vb-modal { background:#fff; border-radius:14px; box-shadow:0 10px 35px rgba(0,0,0,.2);
                width:min(860px, 92vw); padding:20px; }
    .vb-title { margin:0 0 12px; font-size:22px; font-weight:700; }
    .tx-list { display:flex; flex-direction:column; gap:10px; max-height:70vh; overflow:auto; }
    .tx-pill { border-radius:14px; padding:12px 16px; display:grid;
               grid-template-columns: 1fr 2fr auto; align-items:center; }
    .tx-in  { background: linear-gradient(180deg, #34d399, #10b981); color:#072b14; }
    .tx-out { background: linear-gradient(180deg, #fda4a4, #ef4444); color:#2a0909; }
    .tx-date { font-weight:600; opacity:.9; }
    .tx-desc { padding:0 10px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap; }
    .tx-amount { font-weight:700; text-align:right; }
    .vb-actions { display:flex; justify-content:flex-end; margin-top:14px; }
    .vb-btn { padding:8px 14px; border-radius:10px; border:0; background:#eee; cursor:pointer; }
  `]
})
export class VbAccountTransactionsDialogComponent {
  constructor(
    private ref: MatDialogRef<VbAccountTransactionsDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { transactions: VbTransaction[] }
  ) {}
  close() { this.ref.close(); }
}
