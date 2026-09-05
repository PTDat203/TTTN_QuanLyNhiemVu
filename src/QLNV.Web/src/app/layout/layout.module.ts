import { NgModule } from '@angular/core';
import { NbLayoutModule, NbMenuModule, NbSidebarModule } from '@nebular/theme';

import { SharedModule } from '../shared/shared.module';
import { MainLayoutComponent } from './main-layout/main-layout.component';

/** Khung ung dung (header + sidebar + footer). Chi `AppModule` import module nay. */
@NgModule({
  declarations: [MainLayoutComponent],
  imports: [SharedModule, NbLayoutModule, NbSidebarModule, NbMenuModule],
  exports: [MainLayoutComponent]
})
export class LayoutModule {}
