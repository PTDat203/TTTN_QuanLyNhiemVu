import { Component, Input } from '@angular/core';

import { dinhDang, moTaHan, quaHan as tinhQuaHan, sapHetHan as tinhSapHetHan, soNgayConLai } from '../../core/ngay.util';

/**
 * Badge han xu ly (§3.3 M08):
 *  - DO   "Hết hạn N ngày"     khi so ngay con lai < 0;
 *  - VANG "Còn N ngày"          khi 0 <= so ngay con lai <= nguong (mac dinh 3);
 *  - XAM  ngay thuong           khi con nhieu thoi gian;
 *  - "Không đặt hạn"            khi `hanxulyth = null`.
 *
 * Uu tien dung `soNgayConLai` / `quaHan` / `sapHetHan` do BE tinh (dung moc
 * "hom nay" cua may chu); chi tu tinh khi khong duoc truyen vao.
 */
@Component({
  selector: 'qlnv-han-badge',
  templateUrl: './han-badge.component.html',
  styleUrls: ['./han-badge.component.scss']
})
export class HanBadgeComponent {
  /** `hanxulyth` dang "yyyy-MM-dd". */
  @Input() han: string | null = null;

  /** So ngay con lai do BE tinh san (`soNgayConLai`). */
  @Input() soNgay: number | null = null;

  /** Co qua han do BE tinh san (`quaHan`). */
  @Input() quaHan: boolean | null = null;

  /** Co sap het han do BE tinh san (`sapHetHan`). */
  @Input() sapHetHan: boolean | null = null;

  /** Nguong "sap het han", mac dinh 3 ngay (§2.5). */
  @Input() nguong = 3;

  /** Hien ngay dd/MM/yyyy canh badge. */
  @Input() hienNgay = true;

  /** Chi hien badge khi qua han / sap het han (dung trong bang chat). */
  @Input() chiKhiCanhBao = false;

  get so(): number | null {
    if (this.soNgay !== null && this.soNgay !== undefined) return this.soNgay;
    return soNgayConLai(this.han);
  }

  get laQuaHan(): boolean {
    if (this.quaHan !== null && this.quaHan !== undefined) return this.quaHan;
    const s = this.so;
    return s !== null ? s < 0 : tinhQuaHan(this.han);
  }

  get laSapHetHan(): boolean {
    if (this.laQuaHan) return false;
    if (this.sapHetHan !== null && this.sapHetHan !== undefined) return this.sapHetHan;
    const s = this.so;
    return s !== null ? s >= 0 && s <= this.nguong : tinhSapHetHan(this.han, this.nguong);
  }

  get hienBadge(): boolean {
    if (!this.chiKhiCanhBao) return true;
    return this.laQuaHan || this.laSapHetHan;
  }

  get ngayChu(): string {
    return dinhDang(this.han);
  }

  get chuBadge(): string {
    const s = this.so;
    if (s === null) return 'Không đặt hạn';
    if (s < 0) return `Hết hạn ${Math.abs(s)} ngày`;
    if (s === 0) return 'Đến hạn hôm nay';
    if (this.laSapHetHan) return `Sắp hết hạn - còn ${s} ngày`;
    return moTaHan(this.han);
  }

  get lopMau(): string {
    if (this.so === null) return 'mau-trong';
    if (this.laQuaHan) return 'mau-do';
    if (this.laSapHetHan) return 'mau-vang';
    return 'mau-xam';
  }
}
