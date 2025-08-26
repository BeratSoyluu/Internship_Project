import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { OpenBankingService } from '../../core/services/open-banking.service';
import { VakifAccountRow } from './models/vakif-account-row';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { VbAccountDetailsDialogComponent } from './vb-account-details.dialog';
import { VbAccountDetail } from './models/vb-account-detail.model';
import { VbTransaction } from './models/vb-transaction.model';

@Component({
  selector: 'vb-accounts',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    DecimalPipe,
    MatDialogModule,
  ],
  templateUrl: './vb-accounts.component.html',
  styleUrls: ['./vb-accounts.component.css'],
})
export class VbAccountsComponent implements OnInit {
  private api = inject(OpenBankingService);
  private dialog = inject(MatDialog);

  loading = signal(true);
  rows = signal<VakifAccountRow[]>([]);
  error = signal<string | null>(null);

  // ✅ Yeni eklenen state’ler
  txOpen = signal(false);
  txList = signal<VbTransaction[]>([]);

  ngOnInit(): void {
    this.api.getVakifAccountRows().subscribe({
      next: (data) => {
        this.rows.set(data ?? []);
        this.loading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.error.set('Hesap listesi alınırken hata oluştu');
        this.loading.set(false);
      },
    });
  }

  openDetails(row: VakifAccountRow) {
    if (!row?.accountNumber) return;

    this.api.getVakifAccountDetails(row.accountNumber).subscribe({
      next: (raw: any) => {
        const detail: VbAccountDetail = {
          paraBirimi:      raw?.currency ?? row.currency ?? 'TL',
          sonIslemTarihi:  raw?.lastTransactionDate ?? row.lastTransactionDate,
          hesapDurumu:     raw?.statusCode ?? row.status ?? 'A',
          acilisTarihi:    raw?.openDate,
          iban:            raw?.iban ?? row.iban,
          musteriNo:       raw?.customerNo,
          bakiye:          raw?.balance ?? row.balance,
          hesapTuru:       raw?.accountTypeCode ?? row.accountType ?? 2,
          subeKodu:        raw?.branchCode,
          hesapNo:         raw?.accountNumber ?? row.accountNumber,
        };

        this.dialog.open(VbAccountDetailsDialogComponent, {
          data: detail,
        });
      },
      error: (err: any) => {
        console.error('Detay alınamadı', err);
        this.error.set('Hesap detayları alınamadı');
      },
    });
  }

  // ✅ Artık dialog değil, kendi HTML modalını kullanıyoruz
  openTransactions(row: VakifAccountRow) {
    if (!row?.accountNumber) return;

    this.api.getVakifAccountTransactions(row.accountNumber).subscribe({
      next: (list) => {
        this.txList.set(list);
        this.txOpen.set(true);
      },
      error: (err) => console.error('VB TX error', err),
    });
  }

  closeTransactions() {
    this.txOpen.set(false);
    this.txList.set([]);
  }

  trackByIndex(i: number) { return i; }
}
