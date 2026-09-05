/** Kieu du lieu cua mot cot - quyet dinh cach dinh dang o. */
export type KieuCot = 'text' | 'so' | 'ngay' | 'ngayGio' | 'phanTram' | 'boolean' | 'template';

/** Huong sap xep. `null` = khong sap xep. */
export type HuongSapXep = 'asc' | 'desc' | null;

/** Dinh nghia mot cot cua `qlnv-data-table`. */
export interface CotBang {
  /**
   * Khoa lay du lieu tren ban ghi. Ho tro duong dan long nhau: `'nguoiGiao.fullname'`.
   * Cung la khoa dung cho `qlnvCot` khi muon o tuy bien.
   */
  khoa: string;

  /** Tieu de cot (tieng Viet co dau). */
  nhan: string;

  /** Mac dinh `'text'`. Dat `'template'` khi o duoc ve bang `ng-template`. */
  kieu?: KieuCot;

  /** Can phai (dung cho so lieu). */
  canPhai?: boolean;

  /** Can giua. */
  canGiua?: boolean;

  /** Do rong CSS, vi du `'120px'` hoac `'12%'`. */
  rong?: string;

  /** Cho phep bam tieu de de sap xep. Mac dinh: bat cho moi kieu tru `'template'`. */
  sapXep?: boolean;

  /** Lop CSS them cho o. */
  lopCss?: string;

  /** Van ban thay the khi gia tri rong. Mac dinh `'—'`. */
  khiRong?: string;

  /** Cat bot chuoi dai xuong 2 dong (them lop `.qlnv-cat-dong`). */
  catDong?: boolean;
}

/** Su kien doi trang / doi kich thuoc trang. */
export interface SuKienTrang {
  trang: number;
  kichThuoc: number;
}

/** Su kien doi sap xep - dung khi `phanTrangMayChu = true`. */
export interface SuKienSapXep {
  khoa: string;
  huong: HuongSapXep;
}
