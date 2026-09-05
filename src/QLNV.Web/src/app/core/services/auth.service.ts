import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';

import {
  DangNhapRequest,
  DangNhapResponse,
  DangXuatRequest,
  DoiMatKhauRequest,
  LamMoiTokenRequest,
  NguoiDung,
  VaiTro,
  laBenGiao,
  laBenLam
} from '../models';
import { KHOA_LUU_TRU, duongDan } from './api.util';

/**
 * §5.1 A1-A3 - xac thuc JWT tu phat hanh.
 *
 * §10.11: KHONG luu bat ky secret nao o FE. Chi luu access/refresh token do
 * BE phat hanh, trong `localStorage`, va xoa sach khi dang xuat.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _nguoiDung$ = new BehaviorSubject<NguoiDung | null>(this.docNguoiDungDaLuu());

  /** Nguoi dung dang dang nhap; phat lai moi khi phien thay doi. */
  readonly nguoiDung$: Observable<NguoiDung | null> = this._nguoiDung$.asObservable();

  constructor(private readonly http: HttpClient) {}

  // ---------------------------------------------------------------
  // Trang thai phien
  // ---------------------------------------------------------------

  get nguoiDungHienTai(): NguoiDung | null {
    return this._nguoiDung$.value;
  }

  get accessToken(): string | null {
    return this.doc(KHOA_LUU_TRU.accessToken);
  }

  get refreshToken(): string | null {
    return this.doc(KHOA_LUU_TRU.refreshToken);
  }

  get daDangNhap(): boolean {
    return !!this.accessToken && !!this.nguoiDungHienTai;
  }

  get laQuanTri(): boolean {
    return this.nguoiDungHienTai?.vaitro === VaiTro.QuanTri;
  }

  /** §6.2 - cot QUAN_TRI / NGUOI_GIAO. */
  get laBenGiao(): boolean {
    return laBenGiao(this.nguoiDungHienTai?.vaitro);
  }

  /** §6.2 - cot NGUOI_THUC_HIEN. */
  get laBenLam(): boolean {
    return laBenLam(this.nguoiDungHienTai?.vaitro);
  }

  // ---------------------------------------------------------------
  // A1 - POST /api/v1/auth/login
  // ---------------------------------------------------------------

  dangNhap(yeuCau: DangNhapRequest): Observable<DangNhapResponse> {
    return this.http
      .post<DangNhapResponse>(duongDan('/auth/login'), yeuCau)
      .pipe(tap((kq) => this.luuPhien(kq)));
  }

  // ---------------------------------------------------------------
  // A2 - POST /api/v1/auth/refresh
  // ---------------------------------------------------------------

  /** Lam moi token bang refresh token dang luu. Interceptor goi ham nay khi gap 401. */
  lamMoiToken(): Observable<DangNhapResponse> {
    const body: LamMoiTokenRequest = {
      accessToken: this.accessToken,
      refreshToken: this.refreshToken ?? ''
    };
    return this.http
      .post<DangNhapResponse>(duongDan('/auth/refresh'), body)
      .pipe(tap((kq) => this.luuPhien(kq)));
  }

  // ---------------------------------------------------------------
  // A3 - POST /api/v1/auth/logout
  // ---------------------------------------------------------------

  dangXuat(): Observable<void> {
    const body: DangXuatRequest = { refreshToken: this.refreshToken };
    return this.http.post<void>(duongDan('/auth/logout'), body).pipe(tap(() => this.xoaPhien()));
  }

  // ---------------------------------------------------------------
  // GET /api/v1/auth/toi - nap lai ho so nguoi dang dang nhap
  // ---------------------------------------------------------------

  toi(): Observable<NguoiDung> {
    return this.http.get<NguoiDung>(duongDan('/auth/toi')).pipe(tap((u) => this.datNguoiDung(u)));
  }

  // ---------------------------------------------------------------
  // POST /api/v1/auth/doi-mat-khau
  // ---------------------------------------------------------------

  doiMatKhau(yeuCau: DoiMatKhauRequest): Observable<void> {
    return this.http.post<void>(duongDan('/auth/doi-mat-khau'), yeuCau);
  }

  // ---------------------------------------------------------------
  // Quan ly phien
  // ---------------------------------------------------------------

  /** Ghi token + ho so nguoi dung sau khi dang nhap / lam moi thanh cong. */
  luuPhien(kq: DangNhapResponse): void {
    this.ghi(KHOA_LUU_TRU.accessToken, kq.accessToken);
    this.ghi(KHOA_LUU_TRU.refreshToken, kq.refreshToken);
    this.datNguoiDung(kq.nguoiDung);
  }

  /** Xoa sach phien (dang xuat, hoac refresh that bai). */
  xoaPhien(): void {
    this.xoa(KHOA_LUU_TRU.accessToken);
    this.xoa(KHOA_LUU_TRU.refreshToken);
    this.xoa(KHOA_LUU_TRU.nguoiDung);
    this._nguoiDung$.next(null);
  }

  private datNguoiDung(u: NguoiDung): void {
    this.ghi(KHOA_LUU_TRU.nguoiDung, JSON.stringify(u));
    this._nguoiDung$.next(u);
  }

  private docNguoiDungDaLuu(): NguoiDung | null {
    const chuoi = this.doc(KHOA_LUU_TRU.nguoiDung);
    if (!chuoi) return null;
    try {
      return JSON.parse(chuoi) as NguoiDung;
    } catch {
      // Du lieu hong -> coi nhu chua dang nhap, khong nem loi ra ngoai.
      this.xoa(KHOA_LUU_TRU.nguoiDung);
      return null;
    }
  }

  private doc(khoa: string): string | null {
    try {
      return localStorage.getItem(khoa);
    } catch {
      return null;
    }
  }

  private ghi(khoa: string, giaTri: string): void {
    try {
      localStorage.setItem(khoa, giaTri);
    } catch {
      // Trinh duyet chan luu tru - bo qua, phien chi ton tai trong bo nho.
    }
  }

  private xoa(khoa: string): void {
    try {
      localStorage.removeItem(khoa);
    } catch {
      // Bo qua.
    }
  }
}
