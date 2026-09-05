import { Component, Input } from '@angular/core';
import { NbDialogRef } from '@nebular/theme';

/** Ket qua tra ve khi dong hop thoai. `undefined` = nguoi dung dong bang phim ESC. */
export interface KetQuaXacNhan {
  dongY: boolean;
  /** Ly do / phan hoi nguoi dung nhap, khi `batBuocLyDo` hoac `hienLyDo` bat. */
  lyDo?: string;
}

/**
 * Hop thoai xac nhan dung chung.
 *
 * §10.7 - he goc KHONG bat buoc nhap ly do o bat ky cho nao; app moi bat buoc
 * ly do cho: tu choi nhiem vu (D2), nghiem thu CHUA_DAT (E1). Bat `batBuocLyDo`
 * de o nhap hien ra va nut dong y bi khoa cho toi khi nhap du.
 */
@Component({
  selector: 'qlnv-xac-nhan-dialog',
  templateUrl: './xac-nhan-dialog.component.html',
  styleUrls: ['./xac-nhan-dialog.component.scss']
})
export class XacNhanDialogComponent {
  @Input() tieuDe = 'Xác nhận';
  @Input() thongBao = 'Bạn có chắc chắn muốn thực hiện thao tác này?';
  @Input() nhanDongY = 'Đồng ý';
  @Input() nhanHuy = 'Huỷ';

  /** `nbStatus` cua nut dong y: primary | success | warning | danger | info | basic. */
  @Input() trangThai: 'primary' | 'success' | 'warning' | 'danger' | 'info' | 'basic' = 'primary';

  /** Hien o nhap ly do. */
  @Input() hienLyDo = false;

  /** Bat buoc nhap ly do (tu dong bat `hienLyDo`). */
  @Input() batBuocLyDo = false;

  @Input() nhanLyDo = 'Lý do';
  @Input() goiYLyDo = 'Nhập lý do...';

  /** So ky tu toi thieu cua ly do khi bat buoc. */
  @Input() doDaiLyDoToiThieu = 3;

  lyDo = '';

  constructor(private readonly dialogRef: NbDialogRef<XacNhanDialogComponent>) {}

  get canNhapLyDo(): boolean {
    return this.hienLyDo || this.batBuocLyDo;
  }

  get khoaDongY(): boolean {
    if (!this.batBuocLyDo) return false;
    return this.lyDo.trim().length < this.doDaiLyDoToiThieu;
  }

  dongY(): void {
    if (this.khoaDongY) return;
    const kq: KetQuaXacNhan = { dongY: true };
    if (this.canNhapLyDo) kq.lyDo = this.lyDo.trim();
    this.dialogRef.close(kq);
  }

  huy(): void {
    this.dialogRef.close({ dongY: false } as KetQuaXacNhan);
  }
}
