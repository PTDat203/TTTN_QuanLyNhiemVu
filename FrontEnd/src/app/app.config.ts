import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    provideRouter(routes),
    // Interceptor gắn JWT vào mọi yêu cầu và xử lý 401 tập trung.
    provideHttpClient(withInterceptors([authInterceptor])),
  ],
};
