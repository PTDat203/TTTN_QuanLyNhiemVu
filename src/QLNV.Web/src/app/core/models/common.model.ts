/**
 * Kieu dung chung cho moi loi goi API.
 *
 * QUY UOC KIEU DU LIEU (khop chinh xac voi JSON cua backend):
 *  - `Guid`      -> string (vi du "3f9a...-...").
 *  - `DateOnly`  -> string dang "yyyy-MM-dd"  (hanxulyth, hanxulyph, ngaybanhanh...).
 *  - `DateTime`  -> string ISO "yyyy-MM-ddTHH:mm:ss" (createdate, ngayxuly...).
 *  - Truong nullable cua C# -> `| null` (BE dat DefaultIgnoreCondition = Never
 *    cho MVC nen khoa van co mat voi gia tri null).
 */

/** Dinh danh UUID do backend sinh. */
export type Guid = string;

/** Ngay khong kem gio, dinh dang "yyyy-MM-dd". */
export type NgayIso = string;

/** Moc thoi gian ISO 8601 day du. */
export type MocIso = string;

/** Ket qua phan trang chuan - khop `QLNV.Core.Common.PagedResult<T>`. */
export interface KetQuaPhanTrang<T> {
  items: T[];
  tongSo: number;
  trang: number;
  kichThuoc: number;
  tongSoTrang: number;
}

/** Goi loi chuan cua API - khop `QLNV.Api.Common.LoiApiDto`. */
export interface LoiApi {
  maLoi: string | null;
  thongBao: string;
  chiTiet: string[];
}

/**
 * Ma loi may doc duoc - khop `QLNV.Core.Common.MaLoiChung`.
 * Dung de phan nhanh xu ly, KHONG dung de hien thi (hien `thongBao`).
 */
export const MaLoiChung = {
  DuLieuKhongHopLe: 'DU_LIEU_KHONG_HOP_LE',
  KhongTimThay: 'KHONG_TIM_THAY',
  ChuaDangNhap: 'CHUA_DANG_NHAP',
  KhongCoQuyen: 'KHONG_CO_QUYEN',
  SaiTrangThai: 'SAI_TRANG_THAI',
  ViPhamRangBuoc: 'VI_PHAM_RANG_BUOC',
  TrungNoiDung: 'TRUNG_NOI_DUNG',
  LoiTep: 'LOI_TEP'
} as const;

export type MaLoiChungType = (typeof MaLoiChung)[keyof typeof MaLoiChung];
