import { NgModule } from '@angular/core';

import { SharedModule } from '../../shared/shared.module';
import { AiGoiYComponent } from './ai-goi-y/ai-goi-y.component';
import { PhanCongComponent } from './phan-cong/phan-cong.component';
import { VanBanDanhSachComponent } from './van-ban-danh-sach/van-ban-danh-sach.component';
import { VanBanFormComponent } from './van-ban-form/van-ban-form.component';
import { VanBanRoutingModule } from './van-ban-routing.module';

/**
 * §3.2 - nhom nguoi giao: M03, M04, M05, M06.
 * `VanBanFormComponent` va `AiGoiYComponent` la dialog nen khong co route,
 * nhung VAN phai khai o `declarations` de `NbDialogService.open()` dung duoc.
 */
@NgModule({
  declarations: [
    VanBanDanhSachComponent,
    VanBanFormComponent,
    PhanCongComponent,
    AiGoiYComponent
  ],
  imports: [SharedModule, VanBanRoutingModule]
})
export class VanBanModule {}
