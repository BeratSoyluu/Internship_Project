import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common'; // ✅ pipes & directives
import { OpenBankingService } from '../../core/services/open-banking.service';
import { VakifAccountRow } from './models/vakif-account-row';
import { MatDialog, MatDialogModule } from '@angular/material/dialog'; // ✅ dialog module
import { VbAccountDetailsDialogComponent } from './vb-account-details.dialog';
import { VbAccountDetail } from './models/vb-account-detail.model';

@Component({
  selector: 'vb-accounts',
  standalone: true,
  imports: [
    CommonModule,     // *ngIf, *ngFor
    DatePipe,         // |date
    DecimalPipe,      // |number
    MatDialogModule,  // MatDialog
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
        // normalize
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

  openTransactions(row: VakifAccountRow) {
    console.log('TODO: Hesap hareketleri modalı', row?.accountNumber);
  }

  trackByIndex(i: number) { return i; }
}
