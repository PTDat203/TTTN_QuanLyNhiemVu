import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { NbEvaIconsModule } from '@nebular/eva-icons';
import {
  NbDatepickerModule,
  NbDialogModule,
  NbMenuModule,
  NbSidebarModule,
  NbThemeModule,
  NbToastrModule
} from '@nebular/theme';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { CoreModule } from './core/core.module';
import { DashboardModule } from './features/dashboard/dashboard.module';
import { LayoutModule } from './layout/layout.module';

/**
 * Module goc.
 *
 * §7.2 - Nebular 11 + Bootstrap 4.3.1 + FontAwesome 6 (nap qua `angular.json`).
 * §7.2.1 - KHONG co goi `@progress/*` (Kendo UI): luoi du lieu tu viet o
 * `shared/data-table`.
 */
@NgModule({
  declarations: [AppComponent],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    HttpClientModule,
    FormsModule,

    // Nebular - cac module can `forRoot()` chi khai o day.
    NbThemeModule.forRoot({ name: 'default' }),
    NbSidebarModule.forRoot(),
    NbMenuModule.forRoot(),
    NbDialogModule.forRoot(),
    NbToastrModule.forRoot({ duration: 4000, destroyByClick: true }),
    NbDatepickerModule.forRoot(),
    NbEvaIconsModule,

    CoreModule,
    LayoutModule,
    DashboardModule,
    AppRoutingModule
  ],
  bootstrap: [AppComponent]
})
export class AppModule {}
