import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './api.service';

/**
 * Gắn JWT vào mọi yêu cầu và xử lý 401 tập trung.
 *
 * Đặt ở một chỗ để không service nào phải tự nhớ gắn header — quên một chỗ là
 * endpoint đó lặng lẽ trả 401 mà không rõ vì sao.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const token = auth.token;

  // Không gắn token vào chính lời gọi đăng nhập: lúc đó chưa có token,
  // và nếu còn token cũ đã hết hạn thì gắn vào chỉ tổ gây 401 nhầm.
  const daXacThuc = token && !req.url.includes('/auth/login');
  const yeuCau = daXacThuc
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(yeuCau).pipe(
    catchError((loi: HttpErrorResponse) => {
      if (loi.status === 401 && !req.url.includes('/auth/login')) {
        // Token hết hạn hoặc không hợp lệ: xoá phiên và đưa về màn đăng nhập.
        localStorage.clear();
        auth.nguoiDung.set(null);
        router.navigate(['/dang-nhap']);
      }
      return throwError(() => loi);
    })
  );
};
