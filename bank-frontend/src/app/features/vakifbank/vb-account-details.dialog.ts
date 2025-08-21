import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { VbAccountDetail, HESAP_DURUMU_MAP, HESAP_TURU_MAP } from './models/vb-account-detail.model';

@Component({
  standalone: true,
  selector: 'vb-account-details-dialog',
  imports: [CommonModule],
  template: `
    <div class="vb-modal">
      <h2 class="vb-title">Hesap Detayları</h2>

      <div class="vb-grid">
        <div>Para Birimi</div>          <div>{{ data.paraBirimi }}</div>
        <div>Son İşlem Tarihi</div>     <div>{{ data.sonIslemTarihi | date:'yyyy-MM-dd HH:mm:ss' }}</div>
        <div>Hesap Durumu</div>         <div>{{ durumLabel }}</div>
        <div>Açılış Tarihi</div>        <div>{{ data.acilisTarihi | date:'yyyy-MM-dd' }}</div>
        <div>IBAN</div>                 <div class="mono">{{ data.iban }}</div>
        <div>Müşteri Numarası</div>     <div class="mono">{{ data.musteriNo }}</div>
        <div>Bakiye</div>               <div>{{ data.bakiye | number:'1.2-2' }} {{ data.paraBirimi }}</div>
        <div>Hesap Türü</div>           <div>{{ turLabel }}</div>
        <div>Şube Kodu</div>            <div class="mono">{{ data.subeKodu }}</div>
        <div>Hesap Numarası</div>       <div class="mono">{{ data.hesapNo }}</div>
      </div>

      <div class="vb-actions">
        <button class="vb-btn" (click)="close()">Kapat</button>
      </div>
    </div>
  `,
  styles: [`
    .vb-modal { background:#fff; border-radius:14px; box-shadow:0 10px 35px rgba(0,0,0,.2);
                width:min(720px, 92vw); padding:20px; }
    .vb-title { margin:0 0 12px; font-size:22px; font-weight:700; }
    .vb-grid  { display:grid; grid-template-columns: 220px 1fr; gap:10px 16px; }
    .vb-grid > div:nth-child(odd) { color:#555; }
    .vb-grid > div:nth-child(even){ font-weight:600; }
    .mono { font-family: monospace; }
    .vb-actions { display:flex; justify-content:flex-end; margin-top:14px; }
    .vb-btn { border:0; background:#e9edf7; padding:8px 14px; border-radius:10px; cursor:pointer; }
  `]
})
export class VbAccountDetailsDialogComponent {
  constructor(
    private ref: MatDialogRef<VbAccountDetailsDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: VbAccountDetail
  ) {}

  get durumLabel() { return HESAP_DURUMU_MAP[this.data.hesapDurumu]; }
  get turLabel()   { return HESAP_TURU_MAP[this.data.hesapTuru]; }

  close() { this.ref.close(); }
}
