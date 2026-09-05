/* eslint-disable @typescript-eslint/no-explicit-any */
import {
  AfterContentInit,
  Component,
  ContentChildren,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  QueryList,
  SimpleChanges,
  TemplateRef
} from '@angular/core';

import { dinhDang, dinhDangMoc } from '../../core/ngay.util';
import { CotTemplateDirective } from './cot-template.directive';
import { CotBang, HuongSapXep, SuKienSapXep, SuKienTrang } from './data-table.model';

/**
 * LUOI DU LIEU TU VIET - thu thay the Kendo Grid.
 *
 * §7.2.1 phuong an 3: license Kendo het han 26/12/2024 nen KHONG dung
 * `@progress/kendo-angular-grid`. Moi man danh sach (M03, M08, M11, M13)
 * deu dung component nay.
 *
 * Hai che do:
 *  - `phanTrangMayChu = false` (mac dinh): component tu sap xep + cat trang
 *    tren mang `duLieu` day du.
 *  - `phanTrangMayChu = true`: component chi HIEN THI; man hinh nghe
 *    `trangThayDoi` / `sapXepThayDoi` roi tu goi API. `tongSo` la bat buoc.
 *
 * `duLieu` de kieu `any[]` co chu dich: luoi dung chung cho moi DTO, ma
 * interface cua TypeScript khong tu gan duoc vao `Record<string, unknown>`.
 */
@Component({
  selector: 'qlnv-data-table',
  templateUrl: './data-table.component.html',
  styleUrls: ['./data-table.component.scss']
})
export class DataTableComponent implements OnChanges, AfterContentInit {
  /** Dinh nghia cot. */
  @Input() cot: CotBang[] = [];

  /** Du lieu hien thi. */
  @Input() duLieu: any[] = [];

  /** Dang tai - hien dong "Đang tải dữ liệu...". */
  @Input() dangTai = false;

  /** Bat khi may chu lo phan trang / sap xep. */
  @Input() phanTrangMayChu = false;

  /** Tong so ban ghi tren may chu (bat buoc khi `phanTrangMayChu = true`). */
  @Input() tongSo: number | null = null;

  /** Trang hien tai, bat dau tu 1. */
  @Input() trang = 1;

  /** So dong moi trang. */
  @Input() kichThuoc = 20;

  /** Cac lua chon kich thuoc trang. Mang rong = an o chon. */
  @Input() dsKichThuoc: number[] = [10, 20, 50, 100];

  /** An ca thanh phan trang (dung cho bang ngan). */
  @Input() anPhanTrang = false;

  /** Hien cot so thu tu. */
  @Input() hienStt = true;

  /** Cho phep bam vao dong. */
  @Input() chonDuocDong = false;

  /** Truong lam khoa `trackBy`. */
  @Input() khoaDong = 'id';

  /** Van ban khi khong co du lieu. */
  @Input() thongBaoRong = 'Không có dữ liệu phù hợp.';

  /** Doi trang hoac doi kich thuoc trang. */
  @Output() trangThayDoi = new EventEmitter<SuKienTrang>();

  /** Doi sap xep - chi phat khi `phanTrangMayChu = true`. */
  @Output() sapXepThayDoi = new EventEmitter<SuKienSapXep>();

  /** Bam vao mot dong (khi `chonDuocDong = true`). */
  @Output() chonDong = new EventEmitter<any>();

  @ContentChildren(CotTemplateDirective) dsTemplate?: QueryList<CotTemplateDirective>;

  /** Dong dang hien thi sau khi sap xep / cat trang. */
  duLieuHienThi: any[] = [];

  /** Bo dem de moi luoi tren cung mot trang co id rieng (tranh trung id HTML). */
  private static dem = 0;

  /** Id cua o chon kich thuoc trang - duy nhat cho tung the hien. */
  readonly idKichThuoc = `qlnv-kich-thuoc-${++DataTableComponent.dem}`;

  khoaSapXep: string | null = null;
  huongSapXep: HuongSapXep = null;

  private banDoTemplate = new Map<string, TemplateRef<unknown>>();

  // ---------------------------------------------------------------
  // Vong doi
  // ---------------------------------------------------------------

  ngOnChanges(thayDoi: SimpleChanges): void {
    if (thayDoi['duLieu'] || thayDoi['trang'] || thayDoi['kichThuoc'] || thayDoi['phanTrangMayChu']) {
      this.dungLaiDuLieu();
    }
  }

  ngAfterContentInit(): void {
    this.napTemplate();
    this.dsTemplate?.changes.subscribe(() => this.napTemplate());
  }

  // ---------------------------------------------------------------
  // Tinh toan hien thi
  // ---------------------------------------------------------------

  /** Tong so ban ghi dung cho thanh phan trang. */
  get tongSoThuc(): number {
    if (this.phanTrangMayChu) return this.tongSo ?? 0;
    return this.duLieu?.length ?? 0;
  }

  get tongSoTrang(): number {
    if (this.kichThuoc <= 0) return 1;
    return Math.max(1, Math.ceil(this.tongSoThuc / this.kichThuoc));
  }

  /** Chi so ban ghi dau tien cua trang (1-based) - dung cho cot STT va dong tom tat. */
  get chiSoDau(): number {
    if (this.tongSoThuc === 0) return 0;
    return (this.trang - 1) * this.kichThuoc + 1;
  }

  get chiSoCuoi(): number {
    return Math.min(this.trang * this.kichThuoc, this.tongSoThuc);
  }

  /** Danh sach so trang de ve nut, co dau "..." khi qua nhieu trang. */
  get dsSoTrang(): number[] {
    const tong = this.tongSoTrang;
    const hienTai = this.trang;
    const kq: number[] = [];

    const dau = Math.max(1, hienTai - 2);
    const cuoi = Math.min(tong, dau + 4);
    const datLai = Math.max(1, cuoi - 4);

    for (let i = datLai; i <= cuoi; i++) kq.push(i);
    return kq;
  }

  get soCot(): number {
    return (this.cot?.length ?? 0) + (this.hienStt ? 1 : 0);
  }

  // ---------------------------------------------------------------
  // Sap xep
  // ---------------------------------------------------------------

  choPhepSapXep(c: CotBang): boolean {
    if (c.sapXep !== undefined) return c.sapXep;
    return c.kieu !== 'template';
  }

  bamTieuDe(c: CotBang): void {
    if (!this.choPhepSapXep(c)) return;

    if (this.khoaSapXep !== c.khoa) {
      this.khoaSapXep = c.khoa;
      this.huongSapXep = 'asc';
    } else if (this.huongSapXep === 'asc') {
      this.huongSapXep = 'desc';
    } else if (this.huongSapXep === 'desc') {
      this.khoaSapXep = null;
      this.huongSapXep = null;
    } else {
      this.huongSapXep = 'asc';
    }

    if (this.phanTrangMayChu) {
      this.sapXepThayDoi.emit({ khoa: this.khoaSapXep ?? c.khoa, huong: this.huongSapXep });
      return;
    }
    this.dungLaiDuLieu();
  }

  bieuTuongSapXep(c: CotBang): string {
    if (this.khoaSapXep !== c.khoa || this.huongSapXep === null) return 'fa-sort';
    return this.huongSapXep === 'asc' ? 'fa-sort-up' : 'fa-sort-down';
  }

  // ---------------------------------------------------------------
  // Phan trang
  // ---------------------------------------------------------------

  diToiTrang(t: number): void {
    const dich = Math.min(Math.max(1, t), this.tongSoTrang);
    if (dich === this.trang) return;
    this.trang = dich;
    this.phatTrang();
  }

  doiKichThuoc(gt: string | number): void {
    const k = Number(gt);
    if (!Number.isFinite(k) || k <= 0 || k === this.kichThuoc) return;
    this.kichThuoc = k;
    this.trang = 1;
    this.phatTrang();
  }

  private phatTrang(): void {
    this.trangThayDoi.emit({ trang: this.trang, kichThuoc: this.kichThuoc });
    if (!this.phanTrangMayChu) this.dungLaiDuLieu();
  }

  // ---------------------------------------------------------------
  // Doc + dinh dang o
  // ---------------------------------------------------------------

  /** Doc gia tri theo duong dan co dau cham: `'nguoiGiao.fullname'`. */
  docGiaTri(dong: any, khoa: string): unknown {
    if (dong === null || dong === undefined) return null;
    if (khoa.indexOf('.') < 0) return dong[khoa];

    let hienTai: any = dong;
    for (const doan of khoa.split('.')) {
      if (hienTai === null || hienTai === undefined) return null;
      hienTai = hienTai[doan];
    }
    return hienTai;
  }

  /** Dinh dang o theo `kieu` cua cot. */
  dinhDangO(dong: any, c: CotBang): string {
    const gt = this.docGiaTri(dong, c.khoa);
    if (gt === null || gt === undefined || gt === '') return c.khiRong ?? '—';

    switch (c.kieu) {
      case 'ngay':
        return dinhDang(String(gt));
      case 'ngayGio':
        return dinhDangMoc(String(gt));
      case 'phanTram':
        return `${gt}%`;
      case 'so':
        return typeof gt === 'number' ? gt.toLocaleString('vi-VN') : String(gt);
      case 'boolean':
        return gt ? 'Có' : 'Không';
      default:
        return String(gt);
    }
  }

  /** TemplateRef tuy bien cua cot, hoac `null` neu cot dung dinh dang mac dinh. */
  templateCua(c: CotBang): TemplateRef<unknown> | null {
    return this.banDoTemplate.get(c.khoa) ?? null;
  }

  ngucanhO(dong: any, c: CotBang, i: number): Record<string, unknown> {
    return {
      $implicit: dong,
      giaTri: this.docGiaTri(dong, c.khoa),
      chiSo: i,
      cot: c
    };
  }

  bamDong(dong: any): void {
    if (!this.chonDuocDong) return;
    this.chonDong.emit(dong);
  }

  theoKhoa = (_i: number, dong: any): unknown => {
    if (dong && typeof dong === 'object' && this.khoaDong in dong) return dong[this.khoaDong];
    return _i;
  };

  // ---------------------------------------------------------------
  // Noi bo
  // ---------------------------------------------------------------

  private napTemplate(): void {
    this.banDoTemplate = new Map<string, TemplateRef<unknown>>();
    this.dsTemplate?.forEach((t) => this.banDoTemplate.set(t.khoa, t.template));
  }

  private dungLaiDuLieu(): void {
    const nguon = this.duLieu ?? [];

    if (this.phanTrangMayChu) {
      this.duLieuHienThi = nguon;
      return;
    }

    let ds = [...nguon];

    if (this.khoaSapXep && this.huongSapXep) {
      const khoa = this.khoaSapXep;
      const dau = this.huongSapXep === 'asc' ? 1 : -1;
      ds = ds.sort((a, b) => dau * this.soSanh(this.docGiaTri(a, khoa), this.docGiaTri(b, khoa)));
    }

    if (this.trang > 1 && (this.trang - 1) * this.kichThuoc >= ds.length) {
      this.trang = 1;
    }

    const dau = (this.trang - 1) * this.kichThuoc;
    this.duLieuHienThi = ds.slice(dau, dau + this.kichThuoc);
  }

  private soSanh(a: unknown, b: unknown): number {
    const aRong = a === null || a === undefined || a === '';
    const bRong = b === null || b === undefined || b === '';
    if (aRong && bRong) return 0;
    if (aRong) return 1; // gia tri rong luon xuong cuoi
    if (bRong) return -1;

    if (typeof a === 'number' && typeof b === 'number') return a - b;
    if (typeof a === 'boolean' && typeof b === 'boolean') return (a ? 1 : 0) - (b ? 1 : 0);

    // Chuoi: so sanh theo tieng Viet de "Đ" xep sau "D".
    return String(a).localeCompare(String(b), 'vi');
  }
}
