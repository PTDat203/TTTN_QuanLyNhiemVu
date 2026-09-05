import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { NhiemVuChiTietComponent } from './nhiem-vu-chi-tiet/nhiem-vu-chi-tiet.component';
import { NhiemVuCuaToiComponent } from './nhiem-vu-cua-toi/nhiem-vu-cua-toi.component';

/**
 * Nhom man NHIEM VU - nap luoi tai `/nhiem-vu`.
 *
 * M07 / M09 / M10 la DIALOG mo tu M08, khong co route rieng:
 *  - `KiemTraKetQuaComponent` (M07)
 *  - `XuLyNhiemVuComponent`   (M09)
 *  - `GiaHanComponent`        (M10)
 */
const routes: Routes = [
  // M08 - /nhiem-vu
  { path: '', component: NhiemVuCuaToiComponent, title: 'Nhiệm vụ' },

  // Chi tiet mot nhiem vu - /nhiem-vu/:id
  { path: ':id', component: NhiemVuChiTietComponent, title: 'Chi tiết nhiệm vụ' }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class NhiemVuRoutingModule {}
