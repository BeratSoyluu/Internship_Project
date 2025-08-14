import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private router = inject(Router);

  loading = false;
  error = '';

  form = this.fb.group({
    fullName: ['', Validators.required],
    phone: ['', Validators.required],                 // istersen pattern ekleyebilirsin
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  submit() {
    if (this.form.invalid) return;
    this.loading = true; this.error = '';

    this.http.post('/api/auth/register', this.form.value, { withCredentials: true })
      .subscribe({
        next: () => this.router.navigateByUrl('/login'),
        error: (e: HttpErrorResponse) => {
          this.error = e?.error?.message || e?.message || 'Kayıt başarısız.';
          this.loading = false;
        },
        complete: () => this.loading = false
      });
  }
}
