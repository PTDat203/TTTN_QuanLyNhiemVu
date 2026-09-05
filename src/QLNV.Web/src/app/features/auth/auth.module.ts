import { NgModule } from '@angular/core';

import { SharedModule } from '../../shared/shared.module';
import { AuthRoutingModule } from './auth-routing.module';
import { DangNhapComponent } from './dang-nhap/dang-nhap.component';

/** §3.1 M01 - dang nhap. */
@NgModule({
  declarations: [DangNhapComponent],
  imports: [SharedModule, AuthRoutingModule]
})
export class AuthModule {}
