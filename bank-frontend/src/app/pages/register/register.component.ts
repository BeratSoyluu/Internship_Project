import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.html'
})
export class RegisterComponent {
  fb = inject(FormBuilder);
  auth = inject(AuthService);
  router = inject(Router);

  loading = false;

  form = this.fb.group({
    username: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    balance: [null] // opsiyonel
  });

  async onSubmit() {
    if (this.form.invalid) return;
    this.loading = true;

    // şimdilik mock register; backend gelince balance/username de göndeririz
    const ok = await this.auth.register(
      this.form.value.username || '',
      this.form.value.email || '',
      this.form.value.password || ''
    );

    this.loading = false;
    if (ok) this.router.navigateByUrl('/accounts');
  }
}
