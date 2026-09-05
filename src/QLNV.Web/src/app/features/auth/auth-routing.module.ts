import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { DangNhapComponent } from './dang-nhap/dang-nhap.component';

/**
 * Nhom man XAC THUC - nap luoi (lazy) tai duong dan `/login`.
 * KHONG boc trong `MainLayoutComponent` (man dang nhap khong co sidebar).
 */
const routes: Routes = [
  // M01 - /login
  { path: '', component: DangNhapComponent, title: 'Đăng nhập' }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AuthRoutingModule {}
