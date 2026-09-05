import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';

import { environment } from '../../../../environments/environment';
import {
  BoLocVaiTro,
  DashboardDonVi,
  DashboardTongQuan,
  NHAN_DO_KHAN,
  NhiemVu,
  NhiemVuLocRequest,
  NhiemVuSapDenHan
} from '../../../core/models';
import {
  AuthService,
  DashboardService,
  NhiemVuService,
  ThongBaoService
} from '../../../core/services';
import { dinhDang } from '../../../core/ngay.util';
import {
  bienCssTuMau,
  mauTrangThaiNv,
  nhanTrangThaiNv,
  TrangThaiNv
} from '../../../core/trang-thai/trang-thai.const';
import { CotBang } from '../../../shared/data-table/data-table.model';

/** Mot cung cua bieu do tron (tu ve bang SVG - KHONG dung thu vien bieu do). */
interface CungTron {
  ma: number;
  nhan: string;
  mau: string;
  soLuong: number;
  /** 0..1 - ty le THUC, dung de hien so; do dai cung co the da duoc keo gian. */
  tyLe: number;
  /** `stroke-dasharray` cua duong tron. */
  dasharray: string;
  /** `stroke-dashoffset` (am - dich cung ve phia truoc). */
  dashoffset: number;
}

/** Mot cot cua bieu do khoi luong theo don vi. */
interface CotDonVi {
  unitcode: string;
  tendonvi: string;
  tongSo: number;
  hoanThanh: number;
  quaHan: number;
  dangXuLy: number;
  tyLeHoanThanh: number;
  /** % chieu rong cua thanh ngoai so voi don vi lon nhat (da ep toi thieu). */
  rongCot: number;
  pcHoanThanh: number;
  pcQuaHan: number;
  pcDangXuLy: number;
}

/** Mot dong cua bang "sap den han" - gop tu hai nguon (C3 hoac J1). */
interface DongSapDenHan {
  id: string;
  noidung: string;
  hanxulyth: string | null;
  soNgayConLai: number | null;
  dokhan: string;
  trangthai: number;
  trangthaiDvXuly: number | null;
  quaHan: boolean;
  nguoiLienQuan: string;
}

/**
 * M02 - BANG DIEU KHIEN (§3.1).
 *
 * 4 the dem: Chua trien khai (3) / Dang trien khai (2) / Cho xac nhan (truc B = 10) /
 * Qua han. Bieu do phan bo theo trang thai truc A va khoi luong theo don vi
 * deu TU VE bang SVG + CSS (§7.2.1 phuong an 3: khong nap thu vien bieu do,
 * khong dung Kendo Chart).
 *
 * Bang duoi cung DOI THEO VAI:
 *  - Nguoi thuc hien  -> "Viec cua toi sap den han"  (C3 vaiTro = TOI_LAM)
 *  - Nguoi giao / QT  -> "Viec toi giao sap den han" (C3 vaiTro = TOI_GIAO)
 * §6.1: nguoi giao KHONG duoc giao viec cho chinh minh nen bang "cua toi"
 * cua ho luon rong - khong duoc de bang rong tren man dau tien.
 */
@Component({
  selector: 'qlnv-tong-quan',
  templateUrl: './tong-quan.component.html',
  styleUrls: ['./tong-quan.component.scss']
})
export class TongQuanComponent implements OnInit {
  /** Nguong "sap het han" (§2.5) - lay tu cau hinh moi truong, mac dinh 3 ngay. */
  readonly nguongSapHetHan = environment.nguongSapHetHan;

  dangTai = false;
  dangTaiBang = false;

  tongQuan: DashboardTongQuan | null = null;

  /** Cung cua bieu do tron theo truc A. */
  dsCung: CungTron[] = [];

  /** Tong so nhiem vu dung de ve bieu do tron. */
  tongTrangThai = 0;

  /** Cot cua bieu do khoi luong theo don vi. */
  dsCotDonVi: CotDonVi[] = [];

  /** Dong cua bang "sap den han". */
  dsSapDenHan: DongSapDenHan[] = [];

  /** TOI_LAM hoac TOI_GIAO - quyet dinh tieu de + nguon du lieu cua bang duoi. */
  vaiTroBang: string = BoLocVaiTro.ToiLam;

  /* Hang so ve bieu do tron. r = 70 => chu vi = 2 * PI * 70. */
  readonly banKinh = 70;
  readonly tamX = 100;
  readonly tamY = 100;
  readonly chuVi = 2 * Math.PI * 70;

  /** Ma truc A dung cho tooltip cua the dem. */
  readonly maChuaTrienKhai: number = TrangThaiNv.ChuaTrienKhai;
  readonly maDangTrienKhai: number = TrangThaiNv.DangTrienKhai;

  /** Do dai cung toi thieu (don vi chu vi) de nhom it nhiem vu khong bien mat. */
  private readonly cungToiThieu = 10;

  /** % chieu rong toi thieu cua mot cot don vi. */
  private readonly cotToiThieu = 8;

  /** Cot cua bang "sap den han" - moi cot deu ve bang template rieng. */
  readonly cot: CotBang[] = [
    { khoa: 'noidung', nhan: 'Nội dung nhiệm vụ', kieu: 'template', rong: '40%' },
    { khoa: 'nguoiLienQuan', nhan: 'Người liên quan', kieu: 'template', rong: '18%' },
    { khoa: 'hanxulyth', nhan: 'Hạn xử lý', kieu: 'template', rong: '20%' },
    { khoa: 'trangthai', nhan: 'Trạng thái', kieu: 'template', rong: '22%' }
  ];

  constructor(
    private readonly dashboardSv: DashboardService,
    private readonly nhiemVuSv: NhiemVuService,
    private readonly auth: AuthService,
    private readonly tb: ThongBaoService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    // §6.1 - nguoi giao khong tu giao viec cho minh => bang duoi phai doi vai.
    this.vaiTroBang = this.auth.laBenLam ? BoLocVaiTro.ToiLam : BoLocVaiTro.ToiGiao;
    this.nap();
  }

  // ---------------------------------------------------------------
  // Nap du lieu
  // ---------------------------------------------------------------

  /** J1 - GET /api/v1/dashboard/tong-quan. */
  nap(): void {
    this.dangTai = true;
    this.dashboardSv
      .tongQuan()
      .pipe(finalize(() => (this.dangTai = false)))
      .subscribe({
        next: (kq) => {
          this.tongQuan = kq;
          this.dungBieuDoTron(kq);
          this.dungBieuDoDonVi(kq.theoDonVi ?? []);
          this.napSapDenHan(kq);
        },
        error: (e) => this.tb.loi(e)
      });
  }

  /**
   * Bang "sap den han": uu tien C3 (loc dung vai cua nguoi dang dang nhap);
   * neu C3 khong tra ve gi ma J1 co san danh sach thi dung tam J1.
   */
  private napSapDenHan(tq: DashboardTongQuan): void {
    const loc: NhiemVuLocRequest = {
      vaiTro: this.vaiTroBang,
      sapHetHan: true,
      page: 1,
      size: 10
    };

    this.dangTaiBang = true;
    this.nhiemVuSv
      .danhSach(loc)
      .pipe(finalize(() => (this.dangTaiBang = false)))
      .subscribe({
        next: (kq) => {
          const ds = (kq.items ?? []).map((nv) => this.tuNhiemVu(nv));
          this.dsSapDenHan = ds.length > 0 ? ds : this.tuTongQuan(tq);
        },
        // Loi ky thuat: quay ve danh sach do J1 tra kem, khong de trong man dau tien.
        error: () => {
          this.dsSapDenHan = this.tuTongQuan(tq);
        }
      });
  }

  private tuNhiemVu(nv: NhiemVu): DongSapDenHan {
    const tenChuTri = (nv.chuTri ?? []).map((x) => x.fullname).join(', ');
    return {
      id: nv.id,
      noidung: nv.noidung,
      hanxulyth: nv.hanxulyth,
      soNgayConLai: nv.soNgayConLai,
      dokhan: nv.dokhan,
      trangthai: nv.trangthai,
      trangthaiDvXuly: nv.trangthaiDvXuly,
      quaHan: nv.quaHan,
      nguoiLienQuan:
        this.vaiTroBang === BoLocVaiTro.ToiLam
          ? nv.nguoiGiaoTen ?? 'Không rõ người giao'
          : tenChuTri || 'Chưa phân công'
    };
  }

  private tuTongQuan(tq: DashboardTongQuan): DongSapDenHan[] {
    return (tq.sapDenHan ?? []).map((x: NhiemVuSapDenHan) => ({
      id: x.id,
      noidung: x.noidung,
      hanxulyth: x.hanxulyth,
      soNgayConLai: x.soNgayConLai,
      dokhan: x.dokhan,
      trangthai: x.trangthai,
      trangthaiDvXuly: null,
      quaHan: x.quaHan,
      nguoiLienQuan: '—'
    }));
  }

  // ---------------------------------------------------------------
  // Bieu do tron theo trang thai (tu ve bang SVG)
  // ---------------------------------------------------------------

  /**
   * Dung cac cung cua bieu do tron.
   *
   * Nhom co it nhiem vu duoc EP LEN do dai toi thieu roi rut bot phan du o
   * cac nhom lon, de mot nhom 1-2 nhiem vu khong bien mat canh nhom hang tram.
   * So lieu hien thi ben canh van la SO THUC (`soLuong`, `tyLe`).
   */
  private dungBieuDoTron(tq: DashboardTongQuan): void {
    const nguon = (tq.theoTrangThai ?? []).filter((x) => (x.soLuong ?? 0) > 0);
    this.tongTrangThai = nguon.reduce((s, x) => s + x.soLuong, 0);

    if (this.tongTrangThai <= 0 || nguon.length === 0) {
      this.dsCung = [];
      return;
    }

    const chuVi = this.chuVi;
    let doDai = nguon.map((x) => (x.soLuong / this.tongTrangThai) * chuVi);

    // Buoc 1: tinh phan thieu (de dat toi thieu) va phan du (co the rut bot).
    const thieu = doDai.reduce((s, d) => s + Math.max(0, this.cungToiThieu - d), 0);
    const duThua = doDai.reduce((s, d) => s + Math.max(0, d - this.cungToiThieu), 0);

    // Buoc 2: chi keo gian khi phan du con du de bu - neu khong, giu nguyen ty le.
    if (thieu > 0 && duThua > thieu) {
      const tyLeRut = (duThua - thieu) / duThua;
      doDai = doDai.map((d) =>
        d <= this.cungToiThieu
          ? this.cungToiThieu
          : this.cungToiThieu + (d - this.cungToiThieu) * tyLeRut
      );
    }

    const kq: CungTron[] = [];
    let congDon = 0;
    for (let i = 0; i < nguon.length; i++) {
      const x = nguon[i];
      const len = Math.max(0, Math.min(chuVi, doDai[i]));
      kq.push({
        ma: x.ma,
        nhan: x.nhan || nhanTrangThaiNv(x.ma),
        mau: bienCssTuMau(x.mau || mauTrangThaiNv(x.ma)),
        soLuong: x.soLuong,
        tyLe: x.soLuong / this.tongTrangThai,
        dasharray: this.lamTron(len) + ' ' + this.lamTron(chuVi - len),
        dashoffset: -this.lamTron(congDon)
      });
      congDon += len;
    }
    this.dsCung = kq;
  }

  // ---------------------------------------------------------------
  // Bieu do khoi luong theo don vi (tu ve bang CSS)
  // ---------------------------------------------------------------

  /**
   * Chieu rong thanh ngoai ti le voi tong so nhiem vu cua don vi, nhung
   * KHONG duoi `cotToiThieu` % - de don vi it viec van nhin thay duoc
   * canh don vi hang tram viec.
   */
  private dungBieuDoDonVi(ds: DashboardDonVi[]): void {
    const lonNhat = ds.reduce((m, x) => Math.max(m, x.tongSo ?? 0), 0);

    this.dsCotDonVi = ds.map((x) => {
      const tong = x.tongSo ?? 0;
      const hoanThanh = x.hoanThanh ?? 0;
      const quaHan = x.quaHan ?? 0;
      const dangXuLy = Math.max(0, tong - hoanThanh - quaHan);
      const rong = lonNhat > 0 ? (tong / lonNhat) * 100 : 0;

      return {
        unitcode: x.unitcode,
        tendonvi: x.tendonvi,
        tongSo: tong,
        hoanThanh,
        quaHan,
        dangXuLy,
        tyLeHoanThanh: x.tyLeHoanThanh ?? 0,
        rongCot: tong > 0 ? Math.max(this.cotToiThieu, this.lamTron(rong)) : 0,
        pcHoanThanh: tong > 0 ? this.lamTron((hoanThanh / tong) * 100) : 0,
        pcQuaHan: tong > 0 ? this.lamTron((quaHan / tong) * 100) : 0,
        pcDangXuLy: tong > 0 ? this.lamTron((dangXuLy / tong) * 100) : 0
      };
    });
  }

  // ---------------------------------------------------------------
  // Dieu huong tu the dem sang M08
  // ---------------------------------------------------------------

  /** Bam mot the dem => mo M08 voi dung chip loc tuong ung. */
  moLuoi(chip: string): void {
    this.router.navigate(['/nhiem-vu'], {
      queryParams: { chip: chip, vaiTro: this.vaiTroBang }
    });
  }

  /** Bam mot dong cua bang "sap den han" => mo chi tiet nhiem vu. */
  moChiTiet(dong: DongSapDenHan): void {
    this.router.navigate(['/nhiem-vu', dong.id]);
  }

  // ---------------------------------------------------------------
  // Tien ich cho template
  // ---------------------------------------------------------------

  get tieuDeBang(): string {
    return this.vaiTroBang === BoLocVaiTro.ToiLam
      ? 'Việc của tôi sắp đến hạn (≤ ' + this.nguongSapHetHan + ' ngày)'
      : 'Việc tôi giao sắp đến hạn (≤ ' + this.nguongSapHetHan + ' ngày)';
  }

  get moTaBang(): string {
    return this.vaiTroBang === BoLocVaiTro.ToiLam
      ? 'Nhiệm vụ tôi chủ trì hoặc phối hợp, sắp đến hạn xử lý.'
      : 'Nhiệm vụ tôi đã giao cho người khác, sắp đến hạn xử lý. Người giao không tự giao việc cho mình nên bảng này thay cho "Việc của tôi".';
  }

  get tenNguoiDung(): string {
    return this.auth.nguoiDungHienTai?.fullname ?? '';
  }

  get mocHomNay(): string {
    return dinhDang(this.tongQuan?.homNay ?? null);
  }

  /** Ty le hoan thanh dang % nguyen. */
  get phanTramHoanThanh(): number {
    return Math.round((this.tongQuan?.tyLeHoanThanh ?? 0) * 100);
  }

  nhanDoKhan(ma: string): string {
    return NHAN_DO_KHAN[ma] ?? ma;
  }

  phanTram(tyLe: number): number {
    return Math.round((tyLe ?? 0) * 100);
  }

  private lamTron(gt: number): number {
    return Math.round(gt * 100) / 100;
  }
}
