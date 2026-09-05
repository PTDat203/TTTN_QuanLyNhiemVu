import { Injectable } from '@angular/core';
import { NbDialogService } from '@nebular/theme';
import { Observable, map } from 'rxjs';

import { KetQuaXacNhan, XacNhanDialogComponent } from './xac-nhan-dialog.component';

/** Tham so mo hop thoai xac nhan. */
export interface ThamSoXacNhan {
  tieuDe?: string;
  thongBao: string;
  nhanDongY?: string;
  nhanHuy?: string;
  trangThai?: 'primary' | 'success' | 'warning' | 'danger' | 'info' | 'basic';
  hienLyDo?: boolean;
  batBuocLyDo?: boolean;
  nhanLyDo?: string;
  goiYLyDo?: string;
  doDaiLyDoToiThieu?: number;
}

/**
 * Mo hop thoai xac nhan va tra ve ket qua.
 *
 * @example
 * ```ts
 * this.xacNhan.hoi({
 *   tieuDe: 'Thu hồi nhiệm vụ',
 *   thongBao: 'Nhiệm vụ sẽ chuyển sang trạng thái Đã thu hồi (97) và không thể khôi phục.',
 *   trangThai: 'danger',
 *   batBuocLyDo: true,
 *   nhanLyDo: 'Lý do thu hồi'
 * }).subscribe(kq => { if (kq.dongY) { ... kq.lyDo ... } });
 * ```
 */
@Injectable({ providedIn: 'root' })
export class XacNhanService {
  constructor(private readonly dialog: NbDialogService) {}

  hoi(thamSo: ThamSoXacNhan): Observable<KetQuaXacNhan> {
    const ref = this.dialog.open<XacNhanDialogComponent>(XacNhanDialogComponent, {
      context: thamSo as Partial<XacNhanDialogComponent>,
      closeOnBackdropClick: false,
      autoFocus: true
    });

    // Dong bang ESC => `undefined`; quy ve `{ dongY: false }` de noi goi khong phai kiem null.
    return ref.onClose.pipe(map((kq: KetQuaXacNhan | undefined) => kq ?? { dongY: false }));
  }
}
