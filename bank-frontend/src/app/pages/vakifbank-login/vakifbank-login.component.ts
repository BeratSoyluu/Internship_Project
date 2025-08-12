import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-vakifbank-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './vakifbank-login.html'
  // styleUrls satırını kaldırdık; istersen sonra dosya ekleriz
})
export class VakifbankLoginComponent {
  private fb = inject(FormBuilder);
  router = inject(Router);

  loading = false;

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  onSubmit() {
    if (this.form.invalid) return;
    this.loading = true;
    setTimeout(() => {
      this.loading = false;
      this.router.navigateByUrl('/accounts');
    }, 800);
  }
}
