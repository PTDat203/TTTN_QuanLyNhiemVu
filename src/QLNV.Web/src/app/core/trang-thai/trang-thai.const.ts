/* =====================================================================
   §2.1 / §2.2 / §2.3 - BA TRUC TRANG THAI SONG SONG.
   §10.5: he goc KHONG co bang ma -> nhan; nhan nap runtime tu DM_TUDIEN.
   Tep nay la ban SEED CUNG cua FE: dung khi danh muc chua nap xong, va
   dung lam nguon so lieu cho `quyen.util.ts`. Khi da nap duoc DM_TUDIEN
   (A4) thi UU TIEN nhan cua may chu - xem `TrangThaiStore` trong
   `danh-muc.service.ts`.
   ===================================================================== */

/** Truc A - `trangthai`. §7.4 muc 2: KHONG co 4, 8, 100. */
export const TrangThaiNv = {
  HoanThanh: 1,
  DangTrienKhai: 2,
  ChuaTrienKhai: 3,
  HoanThanhSauHan: 5,
  TuChoi: 6,
  DangTrienKhaiQuaHan: 7,
  GiaHan: 13,
  DaThuHoi: 97
} as const;

export type TrangThaiNvType = (typeof TrangThaiNv)[keyof typeof TrangThaiNv];

/** §2.6 - da bao cao xong (con trong han hoac tre han). */
export const TT_DA_HOAN_THANH: readonly number[] = [TrangThaiNv.HoanThanh, TrangThaiNv.HoanThanhSauHan];

/** Cac trang thai con "mo" - con thao tac duoc. */
export const TT_DANG_MO: readonly number[] = [
  TrangThaiNv.DangTrienKhai,
  TrangThaiNv.ChuaTrienKhai,
  TrangThaiNv.DangTrienKhaiQuaHan
];

/** Cac trang thai khong con nhan hanh dong thuc hien. */
export const TT_KET_THUC: readonly number[] = [
  TrangThaiNv.HoanThanh,
  TrangThaiNv.HoanThanhSauHan,
  TrangThaiNv.DaThuHoi
];

/** Toan bo ma truc A theo dung thu tu hien thi. */
export const TT_TOAN_BO: readonly number[] = [1, 2, 3, 5, 6, 7, 13, 97];

/** Truc B - `trangthaiDvXuly`. §7.4 muc 2: KHONG co 14. */
export const TrangThaiPh = {
  ChoXacNhan: 10,
  DaXacNhan: 11,
  TuChoi: 12
} as const;

export type TrangThaiPhType = (typeof TrangThaiPh)[keyof typeof TrangThaiPh];

/** Truc C - `trangthaixulygiahan`. */
export const TrangThaiGiaHan = {
  ChoDuyet: 10,
  DaDuyet: 11,
  TuChoi: 12
} as const;

/** Mo ta mot ma trang thai de hien thi. */
export interface MoTaTrangThai {
  ma: number;
  nhan: string;
  /** Ten mau tieng Viet - khop cot `mau` cua DM_TUDIEN do BE seed. */
  mau: string;
}

/** §2.1 - ma -> nhan -> mau cua truc A (khop `TrangThaiNv.Nhan` + seed DM_TUDIEN). */
export const MO_TA_TRANG_THAI_NV: Readonly<Record<number, MoTaTrangThai>> = {
  1: { ma: 1, nhan: 'Hoàn thành', mau: 'xanh' },
  2: { ma: 2, nhan: 'Đang triển khai', mau: 'duong' },
  3: { ma: 3, nhan: 'Chưa triển khai', mau: 'xam' },
  5: { ma: 5, nhan: 'Hoàn thành - Sau hạn', mau: 'cam' },
  6: { ma: 6, nhan: 'Từ chối nhiệm vụ', mau: 'do' },
  7: { ma: 7, nhan: 'Đang triển khai - Đã hết hạn', mau: 'do' },
  13: { ma: 13, nhan: 'Gia hạn', mau: 'tim' },
  97: { ma: 97, nhan: 'Đã thu hồi', mau: 'xam' }
};

/** §2.2 - ma -> nhan -> mau cua truc B. */
export const MO_TA_TRANG_THAI_PH: Readonly<Record<number, MoTaTrangThai>> = {
  10: { ma: 10, nhan: 'Chờ xác nhận', mau: 'vang' },
  11: { ma: 11, nhan: 'Đã xác nhận', mau: 'xanh' },
  12: { ma: 12, nhan: 'Từ chối', mau: 'do' }
};

/** §2.3 - ma -> nhan cua truc C. */
export const MO_TA_TRANG_THAI_GIA_HAN: Readonly<Record<number, MoTaTrangThai>> = {
  10: { ma: 10, nhan: 'Chờ duyệt gia hạn', mau: 'vang' },
  11: { ma: 11, nhan: 'Đã duyệt gia hạn', mau: 'xanh' },
  12: { ma: 12, nhan: 'Từ chối gia hạn', mau: 'do' }
};

/** Nhan truc A. `null` khong hop le o truc A nhung van chan cho an toan. */
export function nhanTrangThaiNv(ma: number | null | undefined): string {
  if (ma === null || ma === undefined) return 'Không xác định';
  return MO_TA_TRANG_THAI_NV[ma]?.nhan ?? 'Không xác định';
}

/** Nhan truc B. `null` = chua gui bao cao (§2.2 dong dau). */
export function nhanTrangThaiPh(ma: number | null | undefined): string {
  if (ma === null || ma === undefined) return 'Chưa gửi báo cáo';
  return MO_TA_TRANG_THAI_PH[ma]?.nhan ?? 'Không xác định';
}

/** Nhan truc C. `null` = chua xin gia han. */
export function nhanTrangThaiGiaHan(ma: number | null | undefined): string {
  if (ma === null || ma === undefined) return 'Chưa xin gia hạn';
  return MO_TA_TRANG_THAI_GIA_HAN[ma]?.nhan ?? 'Không xác định';
}

export function mauTrangThaiNv(ma: number | null | undefined): string {
  if (ma === null || ma === undefined) return 'xam';
  return MO_TA_TRANG_THAI_NV[ma]?.mau ?? 'xam';
}

export function mauTrangThaiPh(ma: number | null | undefined): string {
  if (ma === null || ma === undefined) return 'xam';
  return MO_TA_TRANG_THAI_PH[ma]?.mau ?? 'xam';
}

/**
 * Chuyen ten mau tieng Viet cua DM_TUDIEN sang `nbStatus` cua Nebular.
 * Dung cho `nbBadge` / `nbButton [status]` / `nb-tag`.
 */
export const MAU_SANG_NB_STATUS: Readonly<Record<string, string>> = {
  xanh: 'success',
  duong: 'info',
  xam: 'basic',
  cam: 'warning',
  do: 'danger',
  tim: 'primary',
  vang: 'warning'
};

export function nbStatusTuMau(mau: string | null | undefined): string {
  if (!mau) return 'basic';
  return MAU_SANG_NB_STATUS[mau] ?? 'basic';
}

/** Bien CSS tuong ung (dinh nghia o `styles.scss`). */
export function bienCssTuMau(mau: string | null | undefined): string {
  return `var(--qlnv-${mau && MAU_SANG_NB_STATUS[mau] ? mau : 'xam'})`;
}

/**
 * §2.6 - hai diem cuoi.
 * Tra ve ma diem cuoi hoac null. FE tinh lai de khong phu thuoc `diemCuoi` cua BE.
 */
export function laDiemCuoi(trangThai: number | null, trangThaiDvXuly: number | null): string | null {
  if (trangThai === TrangThaiNv.DaThuHoi) return 'DA_THU_HOI';
  if (
    trangThai !== null &&
    TT_DA_HOAN_THANH.includes(trangThai) &&
    trangThaiDvXuly === TrangThaiPh.DaXacNhan
  ) {
    return 'HOAN_THANH_NGHIEM_THU';
  }
  return null;
}

/* =====================================================================
   §5.4 D5 / §2.5 - loc trang thai duoc chon khi bao cao ket qua.
   BE la nguon chuan (goi GET /nhiem-vu/{id}/trang-thai-hop-le); ham duoi
   day dung de hien thi tam thoi va de kiem tra lai truoc khi gui.
   ===================================================================== */

/**
 * Con han (hoac khong co han) -> {1, 2, 3}; qua han -> {3, 5, 7}.
 * Ma 13 (Gia han) LUON bi loai - khong duoc chon khi bao cao.
 * (Dac ta §5.4 D5 viet thu tu {5,7,3}; day la CUNG MOT TAP, chi khac thu tu hien thi.)
 */
export function trangThaiHopLeKhiBaoCao(quaHan: boolean): number[] {
  return quaHan
    ? [TrangThaiNv.ChuaTrienKhai, TrangThaiNv.HoanThanhSauHan, TrangThaiNv.DangTrienKhaiQuaHan]
    : [TrangThaiNv.HoanThanh, TrangThaiNv.DangTrienKhai, TrangThaiNv.ChuaTrienKhai];
}

/** §2.5 - trang thai "dang lam" dung theo han: con han 2, qua han 7. */
export function trangThaiTheoHan(quaHan: boolean): number {
  return quaHan ? TrangThaiNv.DangTrienKhaiQuaHan : TrangThaiNv.DangTrienKhai;
}

/* =====================================================================
   M08 - bo loc nhanh dang chip.
   ===================================================================== */

export interface ChipLoc {
  ma: string;
  nhan: string;
  trangThai?: number[];
  trangThaiDvXuly?: number[];
  quaHan?: boolean;
}

/** 7 chip cua M08, dung dung thu tu dac ta §3.3. */
export const CHIP_LOC_NHIEM_VU: readonly ChipLoc[] = [
  { ma: 'TAT_CA', nhan: 'Tất cả' },
  { ma: 'CHUA_TIEP_NHAN', nhan: 'Chưa tiếp nhận', trangThai: [TrangThaiNv.ChuaTrienKhai] },
  {
    ma: 'DANG_LAM',
    nhan: 'Đang làm',
    trangThai: [TrangThaiNv.DangTrienKhai, TrangThaiNv.DangTrienKhaiQuaHan]
  },
  { ma: 'CHO_XAC_NHAN', nhan: 'Chờ xác nhận', trangThaiDvXuly: [TrangThaiPh.ChoXacNhan] },
  { ma: 'BI_TRA_LAI', nhan: 'Bị trả lại', trangThaiDvXuly: [TrangThaiPh.TuChoi] },
  { ma: 'QUA_HAN', nhan: 'Quá hạn', quaHan: true },
  {
    ma: 'HOAN_THANH',
    nhan: 'Hoàn thành',
    trangThai: [TrangThaiNv.HoanThanh, TrangThaiNv.HoanThanhSauHan]
  }
];
