import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  BaoCaoRequest,
  DuyetGiaHanRequest,
  GiaHan,
  GiaHanRequest,
  Guid,
  KetQuaGiaHan,
  KetQuaPhanTrang,
  KiemTraTrungRequest,
  LichSuNhiemVu,
  NghiemThuRequest,
  NhacViecRequest,
  NhiemVu,
  NhiemVuChiTiet,
  NhiemVuLocRequest,
  SuaNhiemVuRequest,
  TaoNhiemVuRequest,
  TaoNhiemVuResponse,
  ThuHoiBaoCaoRequest,
  ThuHoiNhiemVuRequest,
  ThuHoiPhanCongRequest,
  TienDoRequest,
  TiepNhanRequest,
  TrungNoiDung,
  TuChoiRequest,
  TuDien,
  XuLyTuChoiRequest
} from '../models';
import { duongDan, thamSo } from './api.util';

/**
 * §5.3-§5.6 - toan bo nghiep vu nhiem vu: tao & giao (C), thuc hien (D),
 * kiem tra ket qua (E), gia han (F).
 *
 * MOI ham map 1-1 voi mot endpoint. Cac ham hanh dong (T2..T14) deu tra ve
 * BAN GHI NHIEM VU DA CAP NHAT de man hinh ve lai ngay, khong phai goi lai C4.
 */
@Injectable({ providedIn: 'root' })
export class NhiemVuService {
  constructor(private readonly http: HttpClient) {}

  /* ============================================================
     §5.3 - Tao & giao
     ============================================================ */

  /**
   * C1 - POST /api/v1/van-ban/{idvb}/nhiem-vu - tao + giao nhieu nhiem vu 1 lan (M05).
   * Server dat `trangthai = 3`, `trangthaiDvXuly = null` (T1).
   * Neu `trungNoiDung` tra ve khac rong => KHONG luu gi ca; hoi lai nguoi dung
   * roi goi lai voi `boQuaTrungNoidung = true`.
   */
  taoVaGiao(idvb: Guid, yeuCau: TaoNhiemVuRequest): Observable<TaoNhiemVuResponse> {
    return this.http.post<TaoNhiemVuResponse>(duongDan(`/van-ban/${idvb}/nhiem-vu`), yeuCau);
  }

  /** C2 - POST /api/v1/nhiem-vu/kiem-tra-trung - do trung truoc khi luu. Fail-open. */
  kiemTraTrung(yeuCau: KiemTraTrungRequest): Observable<TrungNoiDung[]> {
    return this.http.post<TrungNoiDung[]>(duongDan('/nhiem-vu/kiem-tra-trung'), yeuCau);
  }

  /** C3 - GET /api/v1/nhiem-vu - danh sach theo vai (M08). */
  danhSach(loc: NhiemVuLocRequest): Observable<KetQuaPhanTrang<NhiemVu>> {
    return this.http.get<KetQuaPhanTrang<NhiemVu>>(duongDan('/nhiem-vu'), {
      params: thamSo(loc as unknown as Record<string, unknown>)
    });
  }

  /** C4 - GET /api/v1/nhiem-vu/{id} - chi tiet + phan cong + lich su + tep. */
  chiTiet(id: Guid): Observable<NhiemVuChiTiet> {
    return this.http.get<NhiemVuChiTiet>(duongDan(`/nhiem-vu/${id}`));
  }

  /** C5 - PUT /api/v1/nhiem-vu/{id} - sua noi dung / han. CHI khi `trangthai = 3`. */
  sua(id: Guid, yeuCau: SuaNhiemVuRequest): Observable<NhiemVu> {
    return this.http.put<NhiemVu>(duongDan(`/nhiem-vu/${id}`), yeuCau);
  }

  /** C6 - POST /api/v1/nhiem-vu/{id}/thu-hoi - T14, ve `(97, null)`. DIEM CUOI. */
  thuHoiNhiemVu(id: Guid, yeuCau?: ThuHoiNhiemVuRequest): Observable<NhiemVu> {
    return this.http.post<NhiemVu>(duongDan(`/nhiem-vu/${id}/thu-hoi`), yeuCau ?? {});
  }

  /** C7 - POST /api/v1/nhiem-vu/{id}/thu-hoi-phan-cong - rut phan cong cua mot so nguoi. */
  thuHoiPhanCong(id: Guid, yeuCau: ThuHoiPhanCongRequest): Observable<NhiemVu> {
    return this.http.post<NhiemVu>(duongDan(`/nhiem-vu/${id}/thu-hoi-phan-cong`), yeuCau);
  }

  /* ============================================================
     §5.4 - Thuc hien (M09)
     ============================================================ */

  /** D1 - T2: `(3, null)` -> `(2, null)` + ghi `ngaytiepnhan`. */
  tiepNhan(id: Guid, yeuCau?: TiepNhanRequest): Observable<NhiemVu> {
    return this.http.post<NhiemVu>(duongDan(`/nhiem-vu/${id}/tiep-nhan`), yeuCau ?? {});
  }

  /** D2 - T3: `(3|2, null)` -> `(6, 10)`. `lyDo` BAT BUOC. */
  tuChoi(id: Guid, yeuCau: TuChoiRequest): Observable<NhiemVu> {
    return this.http.post<NhiemVu>(duongDan(`/nhiem-vu/${id}/tu-choi`), yeuCau);
  }

  /** D3 - T6: chi ghi `mucdoht` + lich su, KHONG doi trang thai. */
  capNhatTienDo(id: Guid, yeuCau: TienDoRequest): Observable<NhiemVu> {
    return this.http.post<NhiemVu>(duongDan(`/nhiem-vu/${id}/tien-do`), yeuCau);
  }

  /** D4 - T7: gui bao cao => `(trangthai da chon, 10)`. BE validate lai theo han. */
  guiBaoCao(id: Guid, yeuCau: BaoCaoRequest): Observable<NhiemVu> {
    return this.http.post<NhiemVu>(duongDan(`/nhiem-vu/${id}/bao-cao`), yeuCau);
  }

  /**
   * D5 - GET /api/v1/nhiem-vu/{id}/trang-thai-hop-le.
   * Danh sach trang thai duoc chon khi bao cao, DA LOC THEO HAN
   * (con han 1/2/3 - qua han 5/7/3; ma 13 luon bi loai).
   */
  trangThaiHopLe(id: Guid): Observable<TuDien[]> {
    return this.http.get<TuDien[]>(duongDan(`/nhiem-vu/${id}/trang-thai-hop-le`));
  }

  /** D6 - T8: `(1|5, 10)` -> `(2 neu con han | 7 neu qua han, null)`. */
  thuHoiBaoCao(id: Guid, yeuCau?: ThuHoiBaoCaoRequest): Observable<NhiemVu> {
    return this.http.post<NhiemVu>(duongDan(`/nhiem-vu/${id}/thu-hoi-bao-cao`), yeuCau ?? {});
  }

  /** D7 - GET /api/v1/nhiem-vu/{id}/lich-su - lich su xu ly + gia han. */
  lichSu(id: Guid): Observable<LichSuNhiemVu> {
    return this.http.get<LichSuNhiemVu>(duongDan(`/nhiem-vu/${id}/lich-su`));
  }

  /* ============================================================
     §5.5 - Kiem tra ket qua (M07)
     ============================================================ */

  /**
   * E1 - T9/T10.
   *  - `DAT`      => `trangthaiDvXuly = 11` (DIEM CUOI khi truc A thuoc {1,5}).
   *  - `CHUA_DAT` => `trangthaiDvXuly = 12`, truc A ve 2 (con han) / 7 (qua han).
   */
  nghiemThu(id: Guid, yeuCau: NghiemThuRequest): Observable<NhiemVu> {
    return this.http.post<NhiemVu>(duongDan(`/nhiem-vu/${id}/nghiem-thu`), yeuCau);
  }

  /**
   * MO RONG §2.4 T4/T5 - POST /api/v1/nhiem-vu/{id}/xu-ly-tu-choi.
   * Nguoi giao xu ly de nghi tu choi tai cap `(6, 10)`:
   *  - `CHAP_NHAN` => `(97, null)` - thu hoi;
   *  - `BAC_BO`    => `(2, 12)` - nguoi thuc hien lam lai.
   */
  xuLyTuChoi(id: Guid, yeuCau: XuLyTuChoiRequest): Observable<NhiemVu> {
    return this.http.post<NhiemVu>(duongDan(`/nhiem-vu/${id}/xu-ly-tu-choi`), yeuCau);
  }

  /** §1.3 - POST /api/v1/nhiem-vu/{id}/nhac-viec. */
  nhacViec(id: Guid, yeuCau: NhacViecRequest): Observable<NhiemVu> {
    return this.http.post<NhiemVu>(duongDan(`/nhiem-vu/${id}/nhac-viec`), yeuCau);
  }

  /* ============================================================
     §5.6 - Gia han (M10)
     ============================================================ */

  /**
   * F1 - T11: de xuat gia han => `trangthai = 13`, `trangthaixulygiahan = 10`.
   * Chan khi `solangiahan >= 2` hoac da co yeu cau dang treo.
   */
  xinGiaHan(id: Guid, yeuCau: GiaHanRequest): Observable<KetQuaGiaHan> {
    return this.http.post<KetQuaGiaHan>(duongDan(`/nhiem-vu/${id}/gia-han`), yeuCau);
  }

  /**
   * F2 - T12/T13: POST /api/v1/gia-han/{idGiaHan}/duyet.
   *  - `DUYET`   => truc C = 11, `solangiahan += 1`, cap nhat `hanxulyth`,
   *                 truc A khoi phuc ve 2 neu han moi con hieu luc.
   *  - `TU_CHOI` => truc C = 12.
   * LUU Y: tham so duong dan la ID CUA BAN GHI GIA HAN, khong phai id nhiem vu.
   */
  duyetGiaHan(idGiaHan: Guid, yeuCau: DuyetGiaHanRequest): Observable<KetQuaGiaHan> {
    return this.http.post<KetQuaGiaHan>(duongDan(`/gia-han/${idGiaHan}/duyet`), yeuCau);
  }

  /** F3 - GET /api/v1/nhiem-vu/{id}/gia-han - lich su gia han (toi da 2 lan). */
  lichSuGiaHan(id: Guid): Observable<GiaHan[]> {
    return this.http.get<GiaHan[]>(duongDan(`/nhiem-vu/${id}/gia-han`));
  }
}
