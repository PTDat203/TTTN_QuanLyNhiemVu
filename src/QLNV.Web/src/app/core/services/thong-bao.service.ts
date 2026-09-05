import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { NbToastrService } from '@nebular/theme';

import { LoiApi } from '../models';

/**
 * Hien thong bao cho nguoi dung. MOI thong bao deu la TIENG VIET CO DAU.
 *
 * `loi()` biet doc goi loi chuan `LoiApiDto` cua backend (`thongBao` + `chiTiet`)
 * nen man hinh chi can `catchError(e => this.tb.loi(e))`.
 */
@Injectable({ providedIn: 'root' })
export class ThongBaoService {
  constructor(private readonly toastr: NbToastrService) {}

  thanhCong(thongBao: string, tieuDe = 'Thành công'): void {
    this.toastr.success(thongBao, tieuDe);
  }

  canhBao(thongBao: string, tieuDe = 'Lưu ý'): void {
    this.toastr.warning(thongBao, tieuDe);
  }

  thongTin(thongBao: string, tieuDe = 'Thông tin'): void {
    this.toastr.info(thongBao, tieuDe);
  }

  /** Hien loi tra ve tu API. Tu doc `thongBao` / `chiTiet` cua `LoiApiDto`. */
  loi(nguon: unknown, tieuDeMacDinh = 'Không thực hiện được'): void {
    this.toastr.danger(this.docThongBao(nguon), tieuDeMacDinh);
  }

  /** Rut thong bao tieng Viet tu mot loi bat ky (dung khi muon tu hien thi). */
  docThongBao(nguon: unknown): string {
    if (typeof nguon === 'string') return nguon;

    if (nguon instanceof HttpErrorResponse) {
      if (nguon.status === 0) {
        return 'Không kết nối được tới máy chủ. Vui lòng kiểm tra kết nối mạng.';
      }

      const goi = nguon.error as Partial<LoiApi> | string | null | undefined;
      if (typeof goi === 'string' && goi.trim()) return goi;

      if (goi && typeof goi === 'object') {
        const chiTiet = Array.isArray(goi.chiTiet) ? goi.chiTiet.filter((x) => !!x) : [];
        if (chiTiet.length > 0) return chiTiet.join(' ');
        if (goi.thongBao) return goi.thongBao;
      }

      return this.theoMaHttp(nguon.status);
    }

    if (nguon instanceof Error && nguon.message) return nguon.message;
    return 'Đã xảy ra lỗi không xác định.';
  }

  /** Ma loi may doc duoc (`MaLoiChung`) de man hinh phan nhanh xu ly. */
  docMaLoi(nguon: unknown): string | null {
    if (nguon instanceof HttpErrorResponse) {
      const goi = nguon.error as Partial<LoiApi> | null | undefined;
      if (goi && typeof goi === 'object' && goi.maLoi) return goi.maLoi;
    }
    return null;
  }

  private theoMaHttp(ma: number): string {
    switch (ma) {
      case 400:
        return 'Dữ liệu gửi lên không hợp lệ.';
      case 401:
        return 'Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn.';
      case 403:
        return 'Bạn không có quyền thực hiện thao tác này.';
      case 404:
        return 'Không tìm thấy dữ liệu.';
      case 409:
        return 'Thao tác không hợp lệ ở trạng thái hiện tại.';
      case 413:
        return 'Tệp tải lên vượt quá dung lượng cho phép.';
      case 500:
        return 'Máy chủ gặp sự cố. Vui lòng thử lại sau.';
      default:
        return 'Thao tác không thực hiện được.';
    }
  }
}
