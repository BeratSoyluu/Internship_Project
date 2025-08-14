import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrls: ['./login.component.css']   // <- inline styles yerine ayrı CSS
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private router = inject(Router);

  loading = false;
  error = '';

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  submit() {
    if (this.form.invalid) return;
    this.loading = true; this.error = '';

    this.http.post<{ token?: string }>('/api/auth/login', this.form.value, { withCredentials: true })
      .subscribe({
        next: (res) => {
          if (res?.token) localStorage.setItem('auth.token', res.token);
          this.router.navigateByUrl('/connect');
        },
        error: (e: HttpErrorResponse) => {
          this.error = e?.error?.message || e?.message || 'Giriş başarısız.';
          this.loading = false;
        },
        complete: () => this.loading = false
      });
  }
}
