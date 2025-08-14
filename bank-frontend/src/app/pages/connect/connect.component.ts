import { Component, inject } from '@angular/core';
import { CommonModule, NgIf } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-connect',
  standalone: true,
  imports: [CommonModule, NgIf],
  template: `
    <h2 class="text-xl font-semibold mb-4">Bağlantılar</h2>

    <div class="grid gap-3 max-w-lg">
      <button
        (click)="vakifbank()"
        [disabled]="!!loading"
        class="bg-indigo-600 text-white px-4 py-3 rounded"
        [attr.aria-busy]="loading === 'vakifbank' ? 'true' : null"
      >
        {{ loading === 'vakifbank' ? 'Yönlendiriliyor...' : "VakıfBank'a Bağlan" }}
      </button>

      <button
        (click)="mybank()"
        [disabled]="!!loading"
        class="bg-emerald-600 text-white px-4 py-3 rounded"
        [attr.aria-busy]="loading === 'mybank' ? 'true' : null"
      >
        {{ loading === 'mybank' ? 'Yönlendiriliyor...' : "MyBank'e Bağlan" }}
      </button>

      <div *ngIf="error" class="text-red-600 text-sm mt-2">{{ error }}</div>
      <div *ngIf="info" class="text-slate-600 text-sm mt-2">{{ info }}</div>
    </div>
  `
})
export class ConnectComponent {
  private http = inject(HttpClient);

  loading: 'vakifbank' | 'mybank' | null = null;
  error = '';
  info = '';

  vakifbank() {
    this.handleConnect('/api/vakifbank/connect', 'vakifbank', 'VakıfBank bağlantı hatası');
  }

  mybank() {
    this.handleConnect('/api/mybank/connect', 'mybank', 'MyBank bağlantı hatası');
  }

  private handleConnect(
    path: string,
    key: 'vakifbank' | 'mybank',
    fallbackError: string
  ) {
    this.error = '';
    this.info = '';
    this.loading = key;

    this.http.post<any>(path, {}, {
      withCredentials: true,
      headers: { 'Accept': 'application/json' }
    })
    .subscribe({
      next: (res) => {
        this.loading = null;
        const url = (typeof res === 'string') ? res : (res?.url ?? res?.redirectUrl ?? '');
        if (url) {
          window.location.assign(url);
        } else if (res?.status === 'pending' && res?.message) {
          this.info = res.message;
        } else {
          this.error = 'Yönlendirme adresi alınamadı.';
        }
      },
      error: (e: HttpErrorResponse) => {
        this.loading = null;
        const serverMsg =
          (e?.error && (e.error.message || e.error.error || e.error.detail)) ||
          e?.message;
        this.error = serverMsg || fallbackError;
      }
    });
  }
}
