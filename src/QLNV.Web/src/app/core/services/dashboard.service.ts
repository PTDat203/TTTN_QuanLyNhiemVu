import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { DashboardDonVi, DashboardTongQuan, NhiemVuSapDenHan } from '../models';
import { duongDan, thamSo } from './api.util';

/**
 * §5.10 J1-J2 - thong ke cho man M02.
 * §6.2 dong 20: QUAN_TRI thay toan he thong, NGUOI_GIAO chi thay don vi minh
 * va don vi con - BE tu gioi han pham vi, FE khong can loc them.
 */
@Injectable({ providedIn: 'root' })
export class DashboardService {
  constructor(private readonly http: HttpClient) {}

  /** J1 - GET /api/v1/dashboard/tong-quan - 4 the dem + bieu do + sap den han. */
  tongQuan(): Observable<DashboardTongQuan> {
    return this.http.get<DashboardTongQuan>(duongDan('/dashboard/tong-quan'));
  }

  /** J2 - GET /api/v1/dashboard/theo-don-vi - so nhiem vu + ty le hoan thanh theo don vi. */
  theoDonVi(): Observable<DashboardDonVi[]> {
    return this.http.get<DashboardDonVi[]>(duongDan('/dashboard/theo-don-vi'));
  }

  /** GET /api/v1/dashboard/sap-den-han?soNgay=3 - "Viec cua toi sap den han". */
  sapDenHan(soNgay = 3): Observable<NhiemVuSapDenHan[]> {
    return this.http.get<NhiemVuSapDenHan[]>(duongDan('/dashboard/sap-den-han'), {
      params: thamSo({ soNgay })
    });
  }
}
