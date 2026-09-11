import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './api.service';

/** Chặn route khi chưa đăng nhập. */
export const guardDangNhap: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.daDangNhap()) return true;

  router.navigate(['/dang-nhap']);
  return false;
};

/**
 * Chặn route chỉ dành cho người được giao việc cho người khác (Giám đốc, trưởng phòng,
 * trưởng nhóm).
 *
 * Đây chỉ là lớp che giao diện cho đỡ rối. Quyền thật vẫn do backend quyết định —
 * mọi endpoint đều tự kiểm vai trò và phạm vi, nên chặn ở frontend không phải biện pháp bảo mật.
 */
export const guardGiaoViec: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.coTheGiaoViec()) return true;

  router.navigate(['/nhiem-vu']);
  return false;
};
