// src/app/app.ts
import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  // Kök bileşen sadece router-outlet içerir
  template: `<router-outlet></router-outlet>`
})
export class AppComponent {}
