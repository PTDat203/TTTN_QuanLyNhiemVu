import { Component, Input } from '@angular/core';

/**
 * Thanh tien do `mucdoht` (0-100).
 * Dung o M08 (cot tien do) va M09 (form cap nhat tien do - che do chi doc).
 *
 * §9.7: mau trung tinh, KHONG dung do/xanh de tranh ham y "tot/xau";
 * chi khi dat 100% moi to xanh.
 */
@Component({
  selector: 'qlnv-tien-do-bar',
  templateUrl: './tien-do-bar.component.html',
  styleUrls: ['./tien-do-bar.component.scss']
})
export class TienDoBarComponent {
  /** `mucdoht` 0..100. `null` = chua co bao cao tien do nao. */
  @Input() giaTri: number | null = null;

  /** Hien so phan tram ben canh thanh. */
  @Input() hienSo = true;

  /** Do cao thanh, don vi px. */
  @Input() cao = 8;

  /** Ghi de mau thanh (bien CSS hoac ma mau). Bo trong = mau mac dinh. */
  @Input() mau: string | null = null;

  /** Nhan phu hien duoi thanh (vi du "cập nhật 05/09/2026"). */
  @Input() nhanPhu: string | null = null;

  get phanTram(): number {
    const g = this.giaTri;
    if (g === null || g === undefined || !Number.isFinite(Number(g))) return 0;
    return Math.min(100, Math.max(0, Math.round(Number(g))));
  }

  get coDuLieu(): boolean {
    return this.giaTri !== null && this.giaTri !== undefined;
  }

  get mauThanh(): string {
    if (this.mau) return this.mau;
    return this.phanTram >= 100 ? 'var(--qlnv-xanh, #00b887)' : 'var(--qlnv-duong, #3366ff)';
  }
}
