import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { authGuard } from './core/auth/auth.guard';
import { vaiTroGuard } from './core/auth/vai-tro.guard';
import { VaiTro } from './core/models';
import { TongQuanComponent } from './features/dashboard/tong-quan/tong-quan.component';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';

/**
 * §3 - so do dieu huong.
 *
 * Cau truc: mot route rong boc `MainLayoutComponent` (co `authGuard`),
 * ben trong la cac nhom man nap luoi. Man dang nhap nam NGOAI khung nay.
 *
 * §6.4 - `/quan-tri/*` duoc chan bang `vaiTroGuard`. He goc khong co
 * `canActivate` cho bat ky route quan tri nao; day la lo hong phai khac phuc.
 */
const routes: Routes = [
  {
    // M01 - man dang nhap, khong co sidebar
    path: 'login',
    loadChildren: () => import('./features/auth/auth.module').then((m) => m.AuthModule)
  },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      {
        // M02 - Bang dieu khien. Nap ngay (khong lazy) de route rong khong
        // nuot cac route anh em ben duoi.
        path: '',
        pathMatch: 'full',
        component: TongQuanComponent,
        title: 'Tổng quan'
      },
      {
        // M03, M04, M05, M06
        path: 'van-ban',
        loadChildren: () =>
          import('./features/van-ban/van-ban.module').then((m) => m.VanBanModule)
      },
      {
        // M07, M08, M09, M10
        path: 'nhiem-vu',
        loadChildren: () =>
          import('./features/nhiem-vu/nhiem-vu.module').then((m) => m.NhiemVuModule)
      },
      {
        // M11, M12, M13 - CHI QUAN_TRI (§6.2 dong 21, 23, 24)
        path: 'quan-tri',
        canActivate: [vaiTroGuard],
        data: { vaiTroChoPhep: [VaiTro.QuanTri] },
        loadChildren: () =>
          import('./features/quan-tri/quan-tri.module').then((m) => m.QuanTriModule)
      }
    ]
  },
  { path: '**', redirectTo: '' }
];

@NgModule({
  imports: [
    // Tieu de tab lay tu thuoc tinh `title` cua route (co san tu Angular 14).
    RouterModule.forRoot(routes, { scrollPositionRestoration: 'top' })
  ],
  exports: [RouterModule]
})
export class AppRoutingModule {}
