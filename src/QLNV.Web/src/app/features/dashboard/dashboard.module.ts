import { NgModule } from '@angular/core';

import { SharedModule } from '../../shared/shared.module';
import { TongQuanComponent } from './tong-quan/tong-quan.component';

/**
 * §3.1 M02 - Bang dieu khien.
 *
 * Module nay nap NGAY (khong lazy) vi la man mac dinh o duong dan `/`.
 * Route duoc khai truc tiep trong `app-routing.module.ts` de tranh
 * xung dot giua route rong nap luoi va cac route anh em.
 */
@NgModule({
  declarations: [TongQuanComponent],
  imports: [SharedModule],
  exports: [TongQuanComponent]
})
export class DashboardModule {}
