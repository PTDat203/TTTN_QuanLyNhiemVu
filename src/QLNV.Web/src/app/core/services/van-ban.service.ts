import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { Guid, KetQuaPhanTrang, LuuVanBanRequest, VanBan, VanBanLocRequest } from '../models';
import { duongDan, thamSo } from './api.util';

/**
 * §5.2 B1-B5 - van ban chi dao (M03, M04).
 * §6.2 dong 1-3 quyet dinh ai duoc xem / sua / xoa; BE tra san `choPhepSua`,
 * `choPhepXoa` tren tung ban ghi de FE khong phai doan.
 */
@Injectable({ providedIn: 'root' })
export class VanBanService {
  constructor(private readonly http: HttpClient) {}

  /** B1 - GET /api/v1/van-ban - danh sach phan trang (M03). */
  danhSach(loc: VanBanLocRequest): Observable<KetQuaPhanTrang<VanBan>> {
    return this.http.get<KetQuaPhanTrang<VanBan>>(duongDan('/van-ban'), {
      params: thamSo(loc as unknown as Record<string, unknown>)
    });
  }

  /** B2 - GET /api/v1/van-ban/{id} - chi tiet + tep dinh kem. */
  chiTiet(id: Guid): Observable<VanBan> {
    return this.http.get<VanBan>(duongDan(`/van-ban/${id}`));
  }

  /** B3 - POST /api/v1/van-ban - tao moi (tra 201 kem ban ghi). */
  tao(yeuCau: LuuVanBanRequest): Observable<VanBan> {
    return this.http.post<VanBan>(duongDan('/van-ban'), yeuCau);
  }

  /** B4 - PUT /api/v1/van-ban/{id} - cap nhat. */
  sua(id: Guid, yeuCau: LuuVanBanRequest): Observable<VanBan> {
    return this.http.put<VanBan>(duongDan(`/van-ban/${id}`), yeuCau);
  }

  /** B5 - DELETE /api/v1/van-ban/{id} - chi khi CHUA co nhiem vu con. */
  xoa(id: Guid): Observable<void> {
    return this.http.delete<void>(duongDan(`/van-ban/${id}`));
  }
}
