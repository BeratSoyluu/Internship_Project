import { Component, Input, Output, EventEmitter, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-account-detail-modal',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="modal-backdrop" (click)="close()" aria-hidden="true"></div>

    <div
      class="modal"
      role="dialog"
      aria-modal="true"
      aria-labelledby="accDetailTitle"
      (click)="$event.stopPropagation()"
    >
      <header class="modal-header">
        <h2 id="accDetailTitle" class="title">
          {{ title || 'Hesap Detayı' }}
        </h2>
        <button class="icon-btn" (click)="close()" aria-label="Kapat">✕</button>
      </header>

      <section class="modal-body">
        <ng-content></ng-content>
      </section>
    </div>
  `,
  styles: [`
    .modal-backdrop {
      position: fixed; inset: 0; background: rgba(0,0,0,.35); backdrop-filter: blur(2px); z-index: 40;
    }
    .modal {
      position: fixed; inset: 0; display: grid; place-items: center; z-index: 50;
    }
    .modal > * {
      background: #fff; width: min(720px, 92vw); max-height: 84vh; overflow: auto;
      border-radius: 20px; box-shadow: 0 10px 40px rgba(0,0,0,.2);
    }
    .modal-header {
      display:flex; align-items:center; justify-content:space-between;
      padding: 16px 20px; border-bottom: 1px solid #eee;
      position: sticky; top: 0; background: #fff; z-index: 1;
    }
    .title { margin: 0; font-size: 20px; font-weight: 700; }
    .icon-btn { border: 0; background: transparent; font-size: 18px; cursor: pointer; }
    .modal-body { padding: 16px 20px; }
  `]
})
export class AccountDetailModalComponent {
  @Input() title = '';
  @Output() closed = new EventEmitter<void>();

  close() { this.closed.emit(); }

  // ESC ile kapatma
  @HostListener('document:keydown.escape') onEsc() { this.close(); }
}
