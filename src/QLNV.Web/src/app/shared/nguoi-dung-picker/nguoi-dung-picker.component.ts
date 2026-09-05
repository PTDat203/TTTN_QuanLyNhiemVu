import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';

import { DonVi, Guid, NguoiDungTomTat } from '../../core/models';
import { DanhMucService } from '../../core/services/danh-muc.service';

/** Mot nhom hien thi trong danh sach chon (1 don vi + nguoi cua don vi do). */
export interface NhomDonVi {
  unitcode: string;
  tendonvi: string;
  capdonvi: number;
  nguoiDung: NguoiDungTomTat[];
}

/**
 * Chon nguoi tu CAY DON VI (A6). Dung o:
 *  - M05 - o "Người chủ trì" (bat buoc >= 1) va "Người phối hợp";
 *  - M06 - o loc theo don vi cua popup AI goi y;
 *  - C7  - chon nguoi can thu hoi phan cong.
 *
 * Hai chieu du lieu: `[(dsDaChon)]="ids"`.
 */
@Component({
  selector: 'qlnv-nguoi-dung-picker',
  templateUrl: './nguoi-dung-picker.component.html',
  styleUrls: ['./nguoi-dung-picker.component.scss']
})
export class NguoiDungPickerComponent implements OnInit, OnChanges {
  /** Danh sach userid dang duoc chon (hai chieu). */
  @Input() dsDaChon: Guid[] = [];

  /** Cho chon nhieu nguoi. `false` = chon mot, chon nguoi moi se thay nguoi cu. */
  @Input() nhieu = true;

  /** Cac userid bi loai khoi danh sach (vi du: nguoi da chon o o ben canh). */
  @Input() loaiTru: Guid[] = [];

  /** Chi hien nguoi thuoc cac don vi nay. Mang rong = khong gioi han. */
  @Input() phamViUnitCode: string[] = [];

  /** Chi hien nguoi co vai tro nay (vi du chi NGUOI_THUC_HIEN). Rong = khong loc. */
  @Input() vaiTroChoPhep: string[] = [];

  /** Nhan hien phia tren o chon. */
  @Input() nhan: string | null = null;

  /** Danh dau bat buoc (them dau sao do). */
  @Input() batBuoc = false;

  /** Che do chi doc - chi hien danh sach da chon. */
  @Input() chiDoc = false;

  /** Van ban goi y trong o tim kiem. */
  @Input() goiY = 'Tìm theo tên hoặc đơn vị...';

  /** Chieu cao toi da cua vung cuon, don vi rem. */
  @Input() caoToiDa = 16;

  /** Phat khi danh sach chon doi (hai chieu voi `dsDaChon`). */
  @Output() dsDaChonChange = new EventEmitter<Guid[]>();

  /** Phat ban ghi day du cua nhung nguoi dang duoc chon. */
  @Output() thayDoi = new EventEmitter<NguoiDungTomTat[]>();

  dangTai = false;
  loi: string | null = null;
  tuKhoa = '';

  /** Toan bo nguoi dung phang, tra cuu theo userid. */
  private banDoNguoi = new Map<Guid, NguoiDungTomTat>();
  private nhomGoc: NhomDonVi[] = [];

  /** Nhom sau khi loc theo tu khoa / pham vi / loai tru. */
  nhomHienThi: NhomDonVi[] = [];

  constructor(private readonly danhMuc: DanhMucService) {}

  ngOnInit(): void {
    this.nap();
  }

  ngOnChanges(doi: SimpleChanges): void {
    if (doi['loaiTru'] || doi['phamViUnitCode'] || doi['vaiTroChoPhep']) {
      this.locLai();
    }
  }

  /** Ban ghi day du cua nhung nguoi dang duoc chon (giu dung thu tu chon). */
  get nguoiDaChon(): NguoiDungTomTat[] {
    const kq: NguoiDungTomTat[] = [];
    for (const id of this.dsDaChon ?? []) {
      const n = this.banDoNguoi.get(id);
      if (n) kq.push(n);
    }
    return kq;
  }

  daChon(userid: Guid): boolean {
    return (this.dsDaChon ?? []).indexOf(userid) >= 0;
  }

  bamNguoi(n: NguoiDungTomTat): void {
    if (this.chiDoc) return;

    let moi: Guid[];
    if (this.nhieu) {
      moi = this.daChon(n.userid)
        ? (this.dsDaChon ?? []).filter((x) => x !== n.userid)
        : [...(this.dsDaChon ?? []), n.userid];
    } else {
      moi = this.daChon(n.userid) ? [] : [n.userid];
    }

    this.dsDaChon = moi;
    this.dsDaChonChange.emit(moi);
    this.thayDoi.emit(this.nguoiDaChon);
  }

  boChon(userid: Guid): void {
    if (this.chiDoc) return;
    const moi = (this.dsDaChon ?? []).filter((x) => x !== userid);
    this.dsDaChon = moi;
    this.dsDaChonChange.emit(moi);
    this.thayDoi.emit(this.nguoiDaChon);
  }

  xoaHet(): void {
    if (this.chiDoc) return;
    this.dsDaChon = [];
    this.dsDaChonChange.emit([]);
    this.thayDoi.emit([]);
  }

  doiTuKhoa(gt: string): void {
    this.tuKhoa = gt;
    this.locLai();
  }

  theoUnit = (_i: number, nhom: NhomDonVi): string => nhom.unitcode;
  theoUser = (_i: number, n: NguoiDungTomTat): string => n.userid;

  // ---------------------------------------------------------------
  // Noi bo
  // ---------------------------------------------------------------

  private nap(): void {
    this.dangTai = true;
    this.loi = null;

    this.danhMuc.cayDonViCache().subscribe({
      next: (cay) => {
        this.nhomGoc = this.trai(cay);
        this.banDoNguoi = new Map<Guid, NguoiDungTomTat>();
        for (const nhom of this.nhomGoc) {
          for (const n of nhom.nguoiDung) this.banDoNguoi.set(n.userid, n);
        }
        this.dangTai = false;
        this.locLai();
        this.thayDoi.emit(this.nguoiDaChon);
      },
      error: () => {
        this.dangTai = false;
        this.loi = 'Không tải được danh sách đơn vị và người dùng.';
      }
    });
  }

  private trai(cay: DonVi[]): NhomDonVi[] {
    const kq: NhomDonVi[] = [];
    const di = (ds: DonVi[]): void => {
      for (const dv of ds) {
        kq.push({
          unitcode: dv.unitcode,
          tendonvi: dv.tendonvi,
          capdonvi: dv.capdonvi,
          nguoiDung: dv.nguoiDung ?? []
        });
        if (dv.con?.length) di(dv.con);
      }
    };
    di(cay);
    return kq;
  }

  private locLai(): void {
    const tu = this.tuKhoa.trim().toLowerCase();
    const phamVi = this.phamViUnitCode ?? [];
    const loai = this.loaiTru ?? [];
    const vaiTro = this.vaiTroChoPhep ?? [];

    const kq: NhomDonVi[] = [];
    for (const nhom of this.nhomGoc) {
      if (phamVi.length > 0 && phamVi.indexOf(nhom.unitcode) < 0) continue;

      const nguoi = nhom.nguoiDung.filter((n) => {
        if (loai.indexOf(n.userid) >= 0) return false;
        if (vaiTro.length > 0 && (!n.vaitro || vaiTro.indexOf(n.vaitro) < 0)) return false;
        if (!tu) return true;
        const chuoi = `${n.fullname} ${n.chucvu ?? ''} ${nhom.tendonvi}`.toLowerCase();
        return chuoi.indexOf(tu) >= 0;
      });

      if (nguoi.length > 0) kq.push({ ...nhom, nguoiDung: nguoi });
    }

    this.nhomHienThi = kq;
  }
}
