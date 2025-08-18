// src/main.ts
import { bootstrapApplication } from '@angular/platform-browser';
import { AppComponent } from './app/app';
import { appConfig } from './app/app.config';

import { ApplicationConfig, mergeApplicationConfig } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authInterceptor } from './app/core/interceptors/auth.interceptor';
import { provideRouter } from '@angular/router';
import { routes } from './app/app.routes';

const extraConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),                         // rotaları verir
    provideHttpClient(withInterceptors([authInterceptor])) // JWT interceptor’u ekler
  ]
};

bootstrapApplication(
  AppComponent,
  mergeApplicationConfig(appConfig, extraConfig)   // mevcut appConfig ile birleştir
).catch(err => console.error(err));
