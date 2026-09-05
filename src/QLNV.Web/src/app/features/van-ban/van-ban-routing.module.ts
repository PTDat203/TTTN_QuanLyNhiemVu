import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { PhanCongComponent } from './phan-cong/phan-cong.component';
import { VanBanDanhSachComponent } from './van-ban-danh-sach/van-ban-danh-sach.component';

/**
 * Nhom man VAN BAN CHI DAO - nap luoi tai `/van-ban`.
 *
 * M04 (tao/sua van ban) va M06 (popup AI goi y) la DIALOG, khong co route:
 *  - `VanBanFormComponent` mo tu M03 bang `NbDialogService`;
 *  - `AiGoiYComponent`     mo tu M05 bang `NbDialogService`.
 */
const routes: Routes = [
  // M03 - /van-ban
  { path: '', component: VanBanDanhSachComponent, title: 'Văn bản chỉ đạo' },

  // M05 - /van-ban/:id/phan-cong  (man quan trong nhat, §3.2)
  { path: ':id/phan-cong', component: PhanCongComponent, title: 'Phân công nhiệm vụ' }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class VanBanRoutingModule {}
