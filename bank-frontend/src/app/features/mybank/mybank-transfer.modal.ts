import { Component, EventEmitter, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors, FormGroup } from '@angular/forms';

import { OpenBankingService, TransferCreate } from '../../core/services/open-banking.service';

@Component({
  selector: 'mybank-transfer-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="mb-modal-backdrop" (click)="onClose()"></div>
    <div class="mb-modal" (click)="$event.stopPropagation()">
      <h3>Para Transferi</h3>

      <form [formGroup]="f" (ngSubmit)="submit()">
        <label>Alıcı Adı</label>
        <input formControlName="toName" placeholder="Ali Veli" />

        <label>Alıcı IBAN</label>
        <input formControlName="toIban" placeholder="TR____________________" />

        <label>Tutar (TRY)</label>
        <input type="number" step="0.01" formControlName="amount" />

        <label>Açıklama (opsiyonel)</label>
        <input formControlName="description" />

        <div class="error" *ngIf="errorMsg">{{ errorMsg }}</div>

        <div class="actions">
          <button type="button" class="btn ghost" (click)="onClose()">İptal</button>
          <button type="submit" class="btn primary" [disabled]="f.invalid || loading">
            {{ loading ? 'Gönderiliyor...' : 'Gönder' }}
          </button>
        </div>
      </form>
    </div>
  `,
  styles: [`
    .mb-modal-backdrop{position:fixed;inset:0;background:#0006}
    .mb-modal{
      position:fixed;left:50%;top:50%;transform:translate(-50%,-50%);
      background:#fff;border-radius:12px;min-width:360px;max-width:90vw;
      padding:16px;box-shadow:0 10px 30px #0003;display:flex;flex-direction:column;gap:8px
    }
    form{display:flex;flex-direction:column;gap:8px}
    label{font-size:.9rem;color:#333}
    input{padding:8px;border:1px solid #dcdcdc;border-radius:8px}
    .actions{display:flex;gap:8px;justify-content:flex-end;margin-top:8px}
    .btn{padding:8px 12px;border-radius:8px;border:none;cursor:pointer}
    .btn.primary{background:#2563eb;color:#fff}
    .btn.ghost{background:#f3f4f6}
    .error{color:#b91c1c}
  `]
})
export class MyBankTransferModal {
  @Output() close = new EventEmitter<void>();
  @Output() submitted = new EventEmitter<void>();

  private api = inject(OpenBankingService);
  private fb = inject(FormBuilder);

  loading = false;
  errorMsg = '';

  // ✅ Form: ibanValidator artık class metodu; “used before initialization” olmaz
  f: FormGroup = this.fb.group({
    toName: ['', [Validators.required, Validators.minLength(2)]],
    toIban: ['', [Validators.required, this.ibanValidator.bind(this)]],
    amount: [null as unknown as number, [Validators.required, Validators.min(0.01)]],
    description: ['']
  });

  onClose() { this.close.emit(); }

  submit() {
    if (this.f.invalid) return;
    this.loading = true;
    this.errorMsg = '';

    const v = this.f.value as { toName?: string; toIban?: string; amount?: number; description?: string };
    const body: TransferCreate = {
      toName: (v.toName ?? '').trim(),
      toIban: (v.toIban ?? '').replace(/\s+/g,'').toUpperCase(),
      amount: Number(v.amount ?? 0),
      description: v.description ?? null
    };

    this.api.createMyBankTransfer(body).subscribe({
      next: () => {
        this.loading = false;
        this.submitted.emit(); // parent yenilesin
        this.onClose();
      },
      error: (err) => {
        this.loading = false;
        this.errorMsg = err?.error?.message || 'Transfer sırasında hata oluştu';
      }
    });
  }

  // ✅ IBAN checksum validator (class method, tipli, implicit any yok)
  ibanValidator(control: AbstractControl): ValidationErrors | null {
    const rawVal = control.value as string | null | undefined;
    const raw = (rawVal ?? '').replace(/\s+/g,'').toUpperCase();
    if (!raw) return null; // boşsa diğer required zaten yakalar

    if (!/^[A-Z0-9]+$/.test(raw)) return { iban: true };
    if (raw.length < 15 || raw.length > 34) return { iban: true };

    // Mod 97
    const re = raw.slice(4) + raw.slice(0, 4);
    const conv = re.split('').map((ch: string) =>
      /[A-Z]/.test(ch) ? (ch.charCodeAt(0) - 55).toString() : ch
    ).join('');

    let m = 0;
    for (const d of conv) {
      const digit = d.charCodeAt(0) - 48; // '0' => 0
      if (digit < 0 || digit > 9) return { iban: true }; // güvenlik
      m = (m * 10 + digit) % 97;
    }
    return m === 1 ? null : { iban: true };
  }
}
