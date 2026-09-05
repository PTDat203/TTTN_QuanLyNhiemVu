import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { NbToastrService } from '@nebular/theme';

import { VaiTro } from '../models';
import { AuthService } from '../services/auth.service';

/**
 * §6.4 - guard theo VAI TRO. Dung cho `/quan-tri/*` (§6.2 dong 21, 23, 24)
 * va cho cac nhom man chi danh cho ben giao / ben lam.
 *
 * Khai bao o route:
 * ```ts
 * {
 *   path: 'quan-tri',
 *   canActivate: [authGuard, vaiTroGuard],
 *   data: { vaiTroChoPhep: [VaiTro.QuanTri] },
 *   loadChildren: () => import('./features/quan-tri/quan-tri.module').then(m => m.QuanTriModule)
 * }
 * ```
 * Khong khai `data.vaiTroChoPhep` => mac dinh CHI QUAN_TRI.
 */
export const vaiTroGuard: CanActivateFn = (route, state): boolean | UrlTree => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const toastr = inject(NbToastrService);

  if (!auth.daDangNhap) {
    return router.createUrlTree(['/login'], { queryParams: { quayLai: state.url } });
  }

  const choPhep = (route.data?.['vaiTroChoPhep'] as string[] | undefined) ?? [VaiTro.QuanTri];
  const vaiTroCuaToi = auth.nguoiDungHienTai?.vaitro ?? '';

  if (choPhep.includes(vaiTroCuaToi)) return true;

  toastr.danger('Bạn không có quyền truy cập chức năng này.', 'Không đủ quyền');
  return router.createUrlTree(['/']);
};
