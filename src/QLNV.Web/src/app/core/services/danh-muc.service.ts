import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map, of, shareReplay, tap } from 'rxjs';

import { DonVi, LinhVuc, MaTypeTuDien, NguoiDungTomTat, TuDien } from '../models';
import {
  MO_TA_TRANG_THAI_NV,
  MO_TA_TRANG_THAI_PH,
  MoTaTrangThai
} from '../trang-thai/trang-thai.const';
import { duongDan, thamSo } from './api.util';

/**
 * §5.1 A4-A6 - danh muc dung chung.
 *
 * §7.4 muc 3: nhan trang thai nam trong DM_TUDIEN voi 2 ma type TRANGTHAINV /
 * TRANGTHAIPH, nap runtime de doi nhan khong phai build lai. `trang-thai.const.ts`
 * chi la ban du phong khi danh muc chua nap xong.
 */
@Injectable({ providedIn: 'root' })
export class DanhMucService {
  private tuDien$?: Observable<TuDien[]>;
  private linhVuc$?: Observable<LinhVuc[]>;
  private cayDonVi$?: Observable<DonVi[]>;

  /** Nhan da nap tu may chu, tra cuu theo `type|ma`. */
  private readonly nhanDaNap = new Map<string, TuDien>();

  constructor(private readonly http: HttpClient) {}

  // ---------------------------------------------------------------
  // A4 - GET /api/v1/danh-muc?type=TRANGTHAINV,TRANGTHAIPH,LOAIVB,DOKHAN
  // ---------------------------------------------------------------

  /** Nap danh muc theo mot hoac nhieu ma type. Bo trong `type` = nap tat ca. */
  tuDien(type?: string | string[]): Observable<TuDien[]> {
    const chuoi = Array.isArray(type) ? type.join(',') : type;
    return this.http
      .get<TuDien[]>(duongDan('/danh-muc'), { params: thamSo({ type: chuoi }) })
      .pipe(tap((ds) => this.ghiNhoNhan(ds)));
  }

  /** Nap TAT CA danh muc mot lan roi dung lai (goi khi khoi dong layout chinh). */
  tatCaTuDien(): Observable<TuDien[]> {
    if (!this.tuDien$) {
      this.tuDien$ = this.tuDien().pipe(shareReplay({ bufferSize: 1, refCount: false }));
    }
    return this.tuDien$;
  }

  /** Loc danh muc da nap theo type (dung sau khi `tatCaTuDien()` da hoan tat). */
  theoType(type: string): Observable<TuDien[]> {
    return this.tatCaTuDien().pipe(map((ds) => ds.filter((x) => x.type === type)));
  }

  /** Danh muc trang thai nhiem vu (truc A). */
  trangThaiNv(): Observable<TuDien[]> {
    return this.theoType(MaTypeTuDien.TrangThaiNv);
  }

  /** Danh muc trang thai phan hoi (truc B). */
  trangThaiPh(): Observable<TuDien[]> {
    return this.theoType(MaTypeTuDien.TrangThaiPh);
  }

  /** Danh muc loai van ban. */
  loaiVanBan(): Observable<TuDien[]> {
    return this.theoType(MaTypeTuDien.LoaiVb);
  }

  /** Danh muc do khan. */
  doKhan(): Observable<TuDien[]> {
    return this.theoType(MaTypeTuDien.DoKhan);
  }

  /**
   * Nhan hien thi cua mot ma, uu tien nhan cua may chu; roi moi den ban seed
   * o `trang-thai.const.ts` (§10.5).
   */
  nhan(type: string, ma: string | number): string {
    const tuMayChu = this.nhanDaNap.get(`${type}|${ma}`);
    if (tuMayChu) return tuMayChu.nhan;

    const so = Number(ma);
    const duPhong: MoTaTrangThai | undefined =
      type === MaTypeTuDien.TrangThaiNv
        ? MO_TA_TRANG_THAI_NV[so]
        : type === MaTypeTuDien.TrangThaiPh
        ? MO_TA_TRANG_THAI_PH[so]
        : undefined;

    return duPhong?.nhan ?? String(ma);
  }

  /** Mau hien thi cua mot ma (ten mau tieng Viet cua DM_TUDIEN). */
  mau(type: string, ma: string | number): string | null {
    const tuMayChu = this.nhanDaNap.get(`${type}|${ma}`);
    if (tuMayChu?.mau) return tuMayChu.mau;

    const so = Number(ma);
    if (type === MaTypeTuDien.TrangThaiNv) return MO_TA_TRANG_THAI_NV[so]?.mau ?? null;
    if (type === MaTypeTuDien.TrangThaiPh) return MO_TA_TRANG_THAI_PH[so]?.mau ?? null;
    return null;
  }

  // ---------------------------------------------------------------
  // A5 - GET /api/v1/linh-vuc
  // ---------------------------------------------------------------

  /** Cay linh vuc / nghiep vu. `baoGomNgungDung = true` lay ca muc da ngung. */
  linhVuc(baoGomNgungDung = false): Observable<LinhVuc[]> {
    return this.http.get<LinhVuc[]>(duongDan('/linh-vuc'), {
      params: thamSo({ baoGomNgungDung })
    });
  }

  /** Ban cache cua cay linh vuc dang hoat dong. */
  cayLinhVuc(): Observable<LinhVuc[]> {
    if (!this.linhVuc$) {
      this.linhVuc$ = this.linhVuc(false).pipe(shareReplay({ bufferSize: 1, refCount: false }));
    }
    return this.linhVuc$;
  }

  /** Trai phang cay linh vuc thanh danh sach (dung cho combobox). */
  linhVucPhang(): Observable<LinhVuc[]> {
    return this.cayLinhVuc().pipe(map((cay) => this.duyetPhang(cay)));
  }

  // ---------------------------------------------------------------
  // A6 - GET /api/v1/don-vi/cay
  // ---------------------------------------------------------------

  /** Cay don vi kem nguoi dung - nguon du lieu cua `nguoi-dung-picker`. */
  cayDonVi(baoGomNgungDung = false): Observable<DonVi[]> {
    return this.http.get<DonVi[]>(duongDan('/don-vi/cay'), {
      params: thamSo({ baoGomNgungDung })
    });
  }

  /** Ban cache cua cay don vi dang hoat dong. */
  cayDonViCache(): Observable<DonVi[]> {
    if (!this.cayDonVi$) {
      this.cayDonVi$ = this.cayDonVi(false).pipe(shareReplay({ bufferSize: 1, refCount: false }));
    }
    return this.cayDonVi$;
  }

  /** Toan bo nguoi dung trong cay don vi, da trai phang. */
  nguoiDungPhang(): Observable<NguoiDungTomTat[]> {
    return this.cayDonViCache().pipe(map((cay) => this.gomNguoiDung(cay)));
  }

  /** Xoa cache khi danh muc vua bi sua o M12. */
  xoaCache(): void {
    this.tuDien$ = undefined;
    this.linhVuc$ = undefined;
    this.cayDonVi$ = undefined;
    this.nhanDaNap.clear();
  }

  // ---------------------------------------------------------------
  // Tien ich noi bo
  // ---------------------------------------------------------------

  private ghiNhoNhan(ds: TuDien[]): void {
    for (const x of ds) this.nhanDaNap.set(`${x.type}|${x.ma}`, x);
  }

  private duyetPhang(cay: LinhVuc[]): LinhVuc[] {
    const kq: LinhVuc[] = [];
    const di = (ds: LinhVuc[]): void => {
      for (const x of ds) {
        kq.push(x);
        if (x.con?.length) di(x.con);
      }
    };
    di(cay);
    return kq;
  }

  private gomNguoiDung(cay: DonVi[]): NguoiDungTomTat[] {
    const kq: NguoiDungTomTat[] = [];
    const di = (ds: DonVi[]): void => {
      for (const dv of ds) {
        if (dv.nguoiDung?.length) kq.push(...dv.nguoiDung);
        if (dv.con?.length) di(dv.con);
      }
    };
    di(cay);
    return kq;
  }

  /** Tien ich cho test - tra ve Observable rong dong bo. */
  static rong<T>(): Observable<T[]> {
    return of([]);
  }
}
