import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AiLogComponent } from './ai-log/ai-log.component';
import { DanhMucComponent } from './danh-muc/danh-muc.component';
import { NguoiDungComponent } from './nguoi-dung/nguoi-dung.component';

/**
 * Nhom man QUAN TRI - nap luoi tai `/quan-tri`.
 *
 * §6.4: toan nhom da duoc chan boi `vaiTroGuard` khai o `app-routing.module.ts`
 * (chi QUAN_TRI). He goc KHONG co `canActivate` cho bat ky route `/admin/...`
 * nao - day la lo hong duoc khac phuc o app moi.
 */
const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'nguoi-dung' },

  // M11
  { path: 'nguoi-dung', component: NguoiDungComponent, title: 'Người dùng' },
  // M12
  { path: 'danh-muc', component: DanhMucComponent, title: 'Danh mục' },
  // M13
  { path: 'ai-log', component: AiLogComponent, title: 'Nhật ký gợi ý AI' }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class QuanTriRoutingModule {}
