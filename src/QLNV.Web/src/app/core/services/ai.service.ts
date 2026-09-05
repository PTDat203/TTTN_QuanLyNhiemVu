import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  AiGoiYLog,
  CauHinhAi,
  GhiKetQuaGoiYRequest,
  GoiYRequest,
  GoiYResponse,
  Guid,
  KetQuaPhanTrang,
  KiemTraCauHinhAi,
  SoSanhBaseline,
  ThongKeAi
} from '../models';
import { duongDan, thamSo } from './api.util';

/**
 * §5.8 H1-H4 - AI goi y nguoi thuc hien. TRONG TAM cua de tai.
 * Man M06 (popup goi y) va M13 (nhat ky) dung service nay.
 */
@Injectable({ providedIn: 'root' })
export class AiService {
  constructor(private readonly http: HttpClient) {}

  /**
   * H1 - POST /api/v1/ai/goi-y-nguoi-thuc-hien (§9.6).
   * Tra top-N ung vien kem 5 thanh phan diem va toi da 4 dong ly do tieng Viet.
   */
  goiYNguoiThucHien(yeuCau: GoiYRequest): Observable<GoiYResponse> {
    return this.http.post<GoiYResponse>(duongDan('/ai/goi-y-nguoi-thuc-hien'), yeuCau);
  }

  /**
   * H2 - POST /api/v1/ai/goi-y/{goiyId}/ket-qua.
   * BAT BUOC goi sau khi nguoi giao quyet dinh, KE CA khi ho bo qua goi y
   * (`useridDaChon = null`) - neu khong, §9.8 khong tinh duoc ty le chap nhan.
   */
  ghiKetQuaGoiY(goiyId: Guid, yeuCau: GhiKetQuaGoiYRequest): Observable<void> {
    return this.http.post<void>(duongDan(`/ai/goi-y/${goiyId}/ket-qua`), yeuCau);
  }

  /** H3 - GET /api/v1/ai/thong-ke (§9.8) - Precision@1/@3, MRR, ty le chap nhan, Gini. */
  thongKe(): Observable<ThongKeAi> {
    return this.http.get<ThongKeAi>(duongDan('/ai/thong-ke'));
  }

  /** GET /api/v1/ai/so-sanh-baseline - doi chieu voi cac chien luoc don gian. */
  soSanhBaseline(): Observable<SoSanhBaseline[]> {
    return this.http.get<SoSanhBaseline[]>(duongDan('/ai/so-sanh-baseline'));
  }

  /** GET /api/v1/ai/nhat-ky - nhat ky goi y (M13). Chi QUAN_TRI (§6.2 dong 24). */
  nhatKy(trang = 1, kichThuoc = 20): Observable<KetQuaPhanTrang<AiGoiYLog>> {
    return this.http.get<KetQuaPhanTrang<AiGoiYLog>>(duongDan('/ai/nhat-ky'), {
      params: thamSo({ trang, kichThuoc })
    });
  }

  /** H4 - GET /api/v1/ai/cau-hinh - bo trong so w1..w5 va cac hang so. */
  docCauHinh(): Observable<CauHinhAi> {
    return this.http.get<CauHinhAi>(duongDan('/ai/cau-hinh'));
  }

  /** H4 - PUT /api/v1/ai/cau-hinh - luu bo trong so. Chi QUAN_TRI. */
  luuCauHinh(cauHinh: CauHinhAi): Observable<CauHinhAi> {
    return this.http.put<CauHinhAi>(duongDan('/ai/cau-hinh'), cauHinh);
  }

  /** POST /api/v1/ai/cau-hinh/kiem-tra - kiem tra tong trong so truoc khi luu. */
  kiemTraCauHinh(cauHinh: CauHinhAi): Observable<KiemTraCauHinhAi> {
    return this.http.post<KiemTraCauHinhAi>(duongDan('/ai/cau-hinh/kiem-tra'), cauHinh);
  }
}
