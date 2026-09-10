import { Routes } from '@angular/router';
import { guardDangNhap, guardManager } from './core/guards';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'nhiem-vu' },
  {
    path: 'dang-nhap',
    loadComponent: () => import('./man/dang-nhap').then((m) => m.DangNhapComponent),
  },
  {
    path: 'nhiem-vu',
    canActivate: [guardDangNhap],
    loadComponent: () => import('./man/danh-sach').then((m) => m.DanhSachComponent),
  },
  {
    path: 'nhiem-vu/tao',
    canActivate: [guardDangNhap, guardManager],
    loadComponent: () => import('./man/tao-nhiem-vu').then((m) => m.TaoNhiemVuComponent),
  },
  {
    path: 'nhiem-vu/:id',
    canActivate: [guardDangNhap],
    loadComponent: () => import('./man/chi-tiet').then((m) => m.ChiTietComponent),
  },
  { path: '**', redirectTo: 'nhiem-vu' },
];
