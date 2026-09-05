import { Component, Input } from '@angular/core';

import {
  mauTrangThaiNv,
  mauTrangThaiPh,
  nhanTrangThaiGiaHan,
  nhanTrangThaiNv,
  nhanTrangThaiPh
} from '../../core/trang-thai/trang-thai.const';

/**
 * CHIP KEP HAI TRUC - hien `trangthai` (truc A) va `trangthaiDvXuly` (truc B)
 * CANH NHAU, kem ma so.
 *
 * §2: hai truc trang thai song song la nguon nham lan lon nhat cua he goc,
 * nen giao dien PHAI cho nhin thay ca hai cung luc, khong duoc gop lam mot.
 * Truc C (`trangthaixulygiahan`) chi hien khi khac null.
 */
@Component({
  selector: 'qlnv-trang-thai-chip',
  templateUrl: './trang-thai-chip.component.html',
  styleUrls: ['./trang-thai-chip.component.scss']
})
export class TrangThaiChipComponent {
  /** Truc A - `trangthai` (§2.1): 1|2|3|5|6|7|13|97. */
  @Input() trangthai: number | null = null;

  /** Truc B - `trangthaiDvXuly` (§2.2): null|10|11|12. */
  @Input() trangthaiDvXuly: number | null = null;

  /** Truc C - `trangthaixulygiahan` (§2.3). Chi hien khi khac null. */
  @Input() trangthaixulygiahan: number | null = null;

  /** Hien ma so trong ngoac, vi du "Đang triển khai (2)". Mac dinh bat. */
  @Input() hienMa = true;

  /** Hien chip truc B ngay ca khi bang null ("Chưa gửi báo cáo"). */
  @Input() hienTrucBKhiRong = true;

  /** Kich thuoc chip. */
  @Input() kichThuoc: 'nho' | 'vua' = 'vua';

  /** Xep doc thay vi ngang (dung trong o bang hep). */
  @Input() xepDoc = false;

  /** Nhan tieng Viet cua truc A - uu tien nhan do BE tra kem (`tenTrangThai`). */
  @Input() nhanTrucA: string | null = null;

  /** Nhan tieng Viet cua truc B (`tenTrangThaiDvXuly`). */
  @Input() nhanTrucB: string | null = null;

  get nhanA(): string {
    return this.nhanTrucA || nhanTrangThaiNv(this.trangthai);
  }

  get nhanB(): string {
    return this.nhanTrucB || nhanTrangThaiPh(this.trangthaiDvXuly);
  }

  get nhanC(): string {
    return nhanTrangThaiGiaHan(this.trangthaixulygiahan);
  }

  get mauA(): string {
    return mauTrangThaiNv(this.trangthai);
  }

  get mauB(): string {
    return this.trangthaiDvXuly === null ? 'xam' : mauTrangThaiPh(this.trangthaiDvXuly);
  }

  get hienB(): boolean {
    return this.trangthaiDvXuly !== null || this.hienTrucBKhiRong;
  }

  get hienC(): boolean {
    return this.trangthaixulygiahan !== null && this.trangthaixulygiahan !== undefined;
  }

  get maB(): string {
    return this.trangthaiDvXuly === null ? '—' : String(this.trangthaiDvXuly);
  }
}
