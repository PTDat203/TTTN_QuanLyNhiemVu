import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import {
  NbAccordionModule,
  NbActionsModule,
  NbAlertModule,
  NbBadgeModule,
  NbButtonModule,
  NbCardModule,
  NbCheckboxModule,
  NbContextMenuModule,
  NbDatepickerModule,
  NbDialogModule,
  NbFormFieldModule,
  NbIconModule,
  NbInputModule,
  NbListModule,
  NbPopoverModule,
  NbProgressBarModule,
  NbRadioModule,
  NbSelectModule,
  NbSpinnerModule,
  NbTabsetModule,
  NbTagModule,
  NbTooltipModule,
  NbUserModule
} from '@nebular/theme';

import { CotTemplateDirective } from './data-table/cot-template.directive';
import { DataTableComponent } from './data-table/data-table.component';
import { EmptyStateComponent } from './empty-state/empty-state.component';
import { HanBadgeComponent } from './han-badge/han-badge.component';
import { NguoiDungPickerComponent } from './nguoi-dung-picker/nguoi-dung-picker.component';
import { TienDoBarComponent } from './tien-do-bar/tien-do-bar.component';
import { TrangThaiChipComponent } from './trang-thai-chip/trang-thai-chip.component';
import { XacNhanDialogComponent } from './xac-nhan-dialog/xac-nhan-dialog.component';

/** Cac module Nebular dung lai o moi man - gom mot cho de khong khai trung. */
const NEBULAR = [
  NbAccordionModule,
  NbActionsModule,
  NbAlertModule,
  NbBadgeModule,
  NbButtonModule,
  NbCardModule,
  NbCheckboxModule,
  NbContextMenuModule,
  NbDatepickerModule,
  NbFormFieldModule,
  NbIconModule,
  NbInputModule,
  NbListModule,
  NbPopoverModule,
  NbProgressBarModule,
  NbRadioModule,
  NbSelectModule,
  NbSpinnerModule,
  NbTabsetModule,
  NbTagModule,
  NbTooltipModule,
  NbUserModule
];

/** Cac component / directive tu viet, dung chung cho moi man hinh. */
const CUA_QLNV = [
  DataTableComponent,
  CotTemplateDirective,
  TrangThaiChipComponent,
  HanBadgeComponent,
  TienDoBarComponent,
  XacNhanDialogComponent,
  EmptyStateComponent,
  NguoiDungPickerComponent
];

/**
 * Module dung chung.
 *
 * §7.2.1 phuong an 3: KHONG co Kendo UI. Luoi du lieu, chip trang thai,
 * badge han... deu tu viet trong thu muc nay.
 *
 * Moi module man hinh chi can `imports: [SharedModule]` la co du
 * CommonModule + Forms + Router + Nebular + component tu viet.
 */
@NgModule({
  declarations: [...CUA_QLNV],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    ...NEBULAR,
    // Lazy module can forChild() de lay cau hinh dialog; NbDialogService da duoc
    // cung cap o root boi NbDialogModule.forRoot() trong AppModule.
    NbDialogModule.forChild()
  ],
  exports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    ...NEBULAR,
    NbDialogModule,
    ...CUA_QLNV
  ]
})
export class SharedModule {}
