import { NgModule } from '@angular/core';

import { SharedModule } from '../../shared/shared.module';
import { AiLogComponent } from './ai-log/ai-log.component';
import { DanhMucComponent } from './danh-muc/danh-muc.component';
import { NguoiDungComponent } from './nguoi-dung/nguoi-dung.component';
import { QuanTriRoutingModule } from './quan-tri-routing.module';

/** §3.4 - man quan tri toi thieu: M11, M12, M13. Chi QUAN_TRI. */
@NgModule({
  declarations: [NguoiDungComponent, DanhMucComponent, AiLogComponent],
  imports: [SharedModule, QuanTriRoutingModule]
})
export class QuanTriModule {}
