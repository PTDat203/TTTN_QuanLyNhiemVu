import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  Guid,
  HieuSuatNguoiDung,
  KetQuaJob,
  KetQuaPhanTrang,
  LuuNguoiDungRequest,
  NangLucNguoiDung,
  NguoiDung,
  NguoiDungLocRequest
} from '../models';
import { duongDan, thamSo } from './api.util';

/**
 * §5.9 I1-I5 - quan tri nguoi dung, ho so nang luc va hai job nen.
 * §6.2 dong 21: CRUD nguoi dung CHI danh cho QUAN_TRI (route guard `vaiTroGuard`).
 */
@Injectable({ providedIn: 'root' })
export class NguoiDungService {
  constructor(private readonly http: HttpClient) {}

  /** I1 - GET /api/v1/nguoi-dung - danh sach phan trang (M11). */
  danhSach(loc: NguoiDungLocRequest): Observable<KetQuaPhanTrang<NguoiDung>> {
    return this.http.get<KetQuaPhanTrang<NguoiDung>>(duongDan('/nguoi-dung'), {
      params: thamSo(loc as unknown as Record<string, unknown>)
    });
  }

  /** I1 - GET /api/v1/nguoi-dung/{id}. */
  chiTiet(id: Guid): Observable<NguoiDung> {
    return this.http.get<NguoiDung>(duongDan(`/nguoi-dung/${id}`));
  }

  /** I1 - POST /api/v1/nguoi-dung - tao moi (tra 201 kem ban ghi). */
  tao(yeuCau: LuuNguoiDungRequest): Observable<NguoiDung> {
    return this.http.post<NguoiDung>(duongDan('/nguoi-dung'), yeuCau);
  }

  /** I1 - PUT /api/v1/nguoi-dung/{id}. Bo trong `password` neu khong doi mat khau. */
  sua(id: Guid, yeuCau: LuuNguoiDungRequest): Observable<NguoiDung> {
    return this.http.put<NguoiDung>(duongDan(`/nguoi-dung/${id}`), yeuCau);
  }

  /**
   * I2 - GET /api/v1/nguoi-dung/{id}/nang-luc.
   * CHI DE XEM - §10.1: he goc khong luu chuyen mon, so lieu suy tu lich su
   * nhiem vu da nghiem thu, KHONG nhap tay.
   */
  nangLuc(id: Guid): Observable<NangLucNguoiDung> {
    return this.http.get<NangLucNguoiDung>(duongDan(`/nguoi-dung/${id}/nang-luc`));
  }

  /** I3 - GET /api/v1/nguoi-dung/{id}/hieu-suat?linhvuc= - chi so hieu qua. */
  hieuSuat(id: Guid, linhvuc?: string | null): Observable<HieuSuatNguoiDung> {
    return this.http.get<HieuSuatNguoiDung>(duongDan(`/nguoi-dung/${id}/hieu-suat`), {
      params: thamSo({ linhvuc })
    });
  }

  /** I4 - POST /api/v1/jobs/cap-nhat-hieu-suat - tong hop USER_HIEUSUAT. Chi QUAN_TRI. */
  chayJobCapNhatHieuSuat(): Observable<KetQuaJob> {
    return this.http.post<KetQuaJob>(duongDan('/jobs/cap-nhat-hieu-suat'), {});
  }

  /**
   * I5 - POST /api/v1/jobs/cap-nhat-qua-han - §2.5: `trangthai 2 -> 7`, `3 -> 7`
   * khi `hanxulyth < hom nay`. Chi QUAN_TRI. `homNay` de kiem thu.
   */
  chayJobCapNhatQuaHan(homNay?: string | null): Observable<KetQuaJob> {
    return this.http.post<KetQuaJob>(duongDan('/jobs/cap-nhat-qua-han'), null, {
      params: thamSo({ homNay })
    });
  }
}
