import { CommonModule } from '@angular/common';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { NgModule, Optional, SkipSelf } from '@angular/core';

import { AuthInterceptor } from './auth/auth.interceptor';

/**
 * Module ha tang, chi duoc import MOT LAN o `AppModule`.
 *
 * Cac service deu dung `providedIn: 'root'` nen khong khai o day; module nay
 * chi dang ky interceptor va chan viec import lai o module con.
 */
@NgModule({
  imports: [CommonModule, HttpClientModule],
  providers: [
    // Gan Bearer token + xu ly 401 (lam moi token roi phat lai).
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
  ]
})
export class CoreModule {
  constructor(@Optional() @SkipSelf() daNap?: CoreModule) {
    if (daNap) {
      throw new Error('CoreModule chỉ được import một lần, tại AppModule.');
    }
  }
}
