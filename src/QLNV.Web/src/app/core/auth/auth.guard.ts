import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';

import { AuthService } from '../services/auth.service';

/**
 * §6.4 - chan moi route can dang nhap.
 * He goc chi co `canActivate` cho `/login` va cac route `embed`; day la lo hong.
 */
export const authGuard: CanActivateFn = (_route, state): boolean | UrlTree => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.daDangNhap) return true;

  return router.createUrlTree(['/login'], { queryParams: { quayLai: state.url } });
};
