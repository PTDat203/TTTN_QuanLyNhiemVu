import { NgModule } from '@angular/core';

import { SharedModule } from '../../shared/shared.module';
import { GiaHanComponent } from './gia-han/gia-han.component';
import { KiemTraKetQuaComponent } from './kiem-tra-ket-qua/kiem-tra-ket-qua.component';
import { NhiemVuChiTietComponent } from './nhiem-vu-chi-tiet/nhiem-vu-chi-tiet.component';
import { NhiemVuCuaToiComponent } from './nhiem-vu-cua-toi/nhiem-vu-cua-toi.component';
import { NhiemVuRoutingModule } from './nhiem-vu-routing.module';
import { XuLyNhiemVuComponent } from './xu-ly-nhiem-vu/xu-ly-nhiem-vu.component';

/**
 * §3.3 - nhom nguoi thuc hien (M08, M09, M10) + M07 cua nguoi giao.
 * §3.5: he goc tach 3 tab VIECGIAODV / VIECDVGIAO / VIECPH; app moi GOP
 * vao M08 bang mot bo loc "Vai tro: Toi giao / Toi lam".
 */
@NgModule({
  declarations: [
    NhiemVuCuaToiComponent,
    NhiemVuChiTietComponent,
    XuLyNhiemVuComponent,
    KiemTraKetQuaComponent,
    GiaHanComponent
  ],
  imports: [SharedModule, NhiemVuRoutingModule]
})
export class NhiemVuModule {}
