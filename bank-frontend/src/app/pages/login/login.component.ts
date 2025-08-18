import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  loading = false;
  error = '';

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  submit() {
    if (this.form.invalid || this.loading) return;

    this.loading = true;
    this.error = '';
    this.form.disable({ emitEvent: false });

    const body = {
      email: (this.form.value.email ?? '').toString().trim().toLowerCase(),
      password: (this.form.value.password ?? '').toString()
    };

    this.http.post<any>('/api/auth/login', body, { withCredentials: true })
      .pipe(finalize(() => {
        this.loading = false;
        this.form.enable({ emitEvent: false });
      }))
      .subscribe({
        next: (res) => {
          // --- 1) Token'ı hangi adla gelirse gelsin yakala
          const token =
            res?.token ??
            res?.access_token ??
            res?.jwt ??
            res?.jwtToken ??
            res?.data?.token ??
            null;

          if (token) {
            // Hem eski hem yeni anahtara yaz: guard/interceptor kırılmasın
            localStorage.setItem('access_token', token);
            localStorage.setItem('auth.token', token);
            const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/dashboard';
            this.router.navigateByUrl(returnUrl);
            return;
          }

          // --- 2) Token yoksa: muhtemelen cookie ile giriş oldu → me endpoint'i ile doğrula
          this.http.get('/api/auth/me', { withCredentials: true })
            .subscribe({
              next: () => {
                const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/dashboard';
                this.router.navigateByUrl(returnUrl);
              },
              error: () => {
                this.error = 'Giriş başarısız. (Token yok ve oturum doğrulanamadı)';
                localStorage.removeItem('access_token');
                localStorage.removeItem('auth.token');
              }
            });
        },
        error: (e: HttpErrorResponse) => {
          this.error = e?.error?.message || e?.message || 'Giriş başarısız.';
          localStorage.removeItem('access_token');
          localStorage.removeItem('auth.token');
        }
      });
  }


}
