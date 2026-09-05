import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, filter, catchError, switchMap, take, throwError } from 'rxjs';

import { AuthService } from '../services/auth.service';

/**
 * Gan `Authorization: Bearer <accessToken>` cho moi loi goi API,
 * va xu ly 401 theo trinh tu: LAM MOI TOKEN -> phat lai yeu cau -> that bai thi DANG XUAT.
 *
 * Chi mot lan lam moi chay tai mot thoi diem (`dangLamMoi`); cac yeu cau
 * gap 401 trong luc do se xep hang cho token moi roi phat lai.
 */
@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private dangLamMoi = false;
  private readonly tokenMoi$ = new BehaviorSubject<string | null>(null);

  constructor(private readonly auth: AuthService, private readonly router: Router) {}

  intercept(yeuCau: HttpRequest<unknown>, tiep: HttpHandler): Observable<HttpEvent<unknown>> {
    const token = this.auth.accessToken;
    const daGan = token ? this.gan(yeuCau, token) : yeuCau;

    return tiep.handle(daGan).pipe(
      catchError((loi: unknown) => {
        if (!(loi instanceof HttpErrorResponse) || loi.status !== 401) {
          return throwError(() => loi);
        }

        // Chinh endpoint dang nhap / lam moi tra 401 => khong the cuu, dang xuat luon.
        if (this.laEndpointXacThuc(yeuCau.url)) {
          this.buocDangXuat();
          return throwError(() => loi);
        }

        // Khong co refresh token => khong lam moi duoc.
        if (!this.auth.refreshToken) {
          this.buocDangXuat();
          return throwError(() => loi);
        }

        return this.lamMoiRoiPhatLai(yeuCau, tiep, loi);
      })
    );
  }

  private lamMoiRoiPhatLai(
    yeuCau: HttpRequest<unknown>,
    tiep: HttpHandler,
    loiGoc: HttpErrorResponse
  ): Observable<HttpEvent<unknown>> {
    if (this.dangLamMoi) {
      // Da co mot luot lam moi dang chay - cho token moi roi phat lai.
      return this.tokenMoi$.pipe(
        filter((t): t is string => t !== null),
        take(1),
        switchMap((t) => tiep.handle(this.gan(yeuCau, t)))
      );
    }

    this.dangLamMoi = true;
    this.tokenMoi$.next(null);

    return this.auth.lamMoiToken().pipe(
      switchMap((kq) => {
        this.dangLamMoi = false;
        this.tokenMoi$.next(kq.accessToken);
        return tiep.handle(this.gan(yeuCau, kq.accessToken));
      }),
      catchError((loiLamMoi: unknown) => {
        this.dangLamMoi = false;
        this.buocDangXuat();
        return throwError(() => loiLamMoi ?? loiGoc);
      })
    );
  }

  private gan(yeuCau: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
    return yeuCau.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }

  private laEndpointXacThuc(url: string): boolean {
    return url.includes('/auth/login') || url.includes('/auth/refresh');
  }

  private buocDangXuat(): void {
    this.auth.xoaPhien();
    const dangO = this.router.url;
    if (!dangO.startsWith('/login')) {
      void this.router.navigate(['/login'], { queryParams: { quayLai: dangO } });
    }
  }
}
