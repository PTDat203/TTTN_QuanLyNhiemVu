import { Guid, MocIso } from './common.model';

/* =====================================================================
   I1 - CRUD nguoi dung (M11). Chi QUAN_TRI (§6.2 dong 21).
   ===================================================================== */

/**
 * Tham so loc gui bang query string.
 * Rang buoc query dung TEN THUOC TINH C#: search, unitCode, vaiTro, trangThai,
 * page, size (khong phan biet hoa thuong).
 */
export interface NguoiDungLocRequest {
  search?: string | null;
  unitCode?: string | null;
  vaiTro?: string | null;
  trangThai?: number | null;
  page?: number;
  size?: number;
}

/** I1 - body tao / sua nguoi dung (`LuuNguoiDungRequest`). */
export interface LuuNguoiDungRequest {
  username: string;
  /** Bo trong khi cap nhat neu khong doi mat khau. */
  password?: string | null;
  fullname: string;
  email?: string | null;
  unitcode: string;
  chucvu?: string | null;
  /** QUAN_TRI | NGUOI_GIAO | NGUOI_THUC_HIEN. */
  vaitro: string;
  /** 1 = dang hoat dong, 0 = khoa. */
  trangthai: number;
  /** So nhiem vu toi da cung luc - hang so K cua §9.4 S4. */
  maxConcurrentTasks: number;
}

/* =====================================================================
   I2 - GET /api/v1/nguoi-dung/{id}/nang-luc
   §10.1 - SUY TU LICH SU, KHONG nhap tay.
   ===================================================================== */

export interface NangLucNguoiDung {
  userid: Guid;
  fullname: string;
  theoLinhVuc: NangLucLinhVuc[];
  tongSoHoanThanh: number;
}

export interface NangLucLinhVuc {
  linhvuc: string;
  tenLinhVuc: string | null;
  soNvHoanThanh: number;
  /** §9.4 S1 - diem chuyen mon suy ra, 0..1. */
  diemChuyenMon: number;
}

/* =====================================================================
   I3 - GET /api/v1/nguoi-dung/{id}/hieu-suat?linhvuc=
   ===================================================================== */

export interface HieuSuatNguoiDung {
  userid: Guid;
  fullname: string;
  linhvuc: string | null;
  soNvHoanThanh: number;
  soNvDungHan: number;
  soNvBiTraLai: number;
  soLanGiaHan: number;
  soNvDangMo: number;
  taiTrongSo: number;
  soNvQuaHan: number;
  tyLeDungHan: number;
  /** LUU Y: khoa JSON la chu "K" HOA - tran tai cua nguoi dung. */
  K: number;
  updatedate: MocIso | null;
}

/* =====================================================================
   I4 / I5 - kich hoat job thu cong. Chi QUAN_TRI.
   ===================================================================== */

export interface KetQuaJob {
  job: string;
  soBanGhi: number;
  batDau: MocIso;
  ketThuc: MocIso;
  thongBao: string | null;
}
