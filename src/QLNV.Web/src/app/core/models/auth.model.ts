import { Guid, MocIso } from './common.model';

/** A1 - POST /api/v1/auth/login. */
export interface DangNhapRequest {
  username: string;
  password: string;
}

/** A2 - POST /api/v1/auth/refresh. */
export interface LamMoiTokenRequest {
  accessToken?: string | null;
  refreshToken: string;
}

/** A3 - POST /api/v1/auth/logout. */
export interface DangXuatRequest {
  refreshToken?: string | null;
}

/** POST /api/v1/auth/doi-mat-khau. */
export interface DoiMatKhauRequest {
  matKhauCu: string;
  matKhauMoi: string;
}

/** Cap token do BE phat hanh (dung noi bo, khong tra thang o A1/A2). */
export interface CapToken {
  accessToken: string;
  refreshToken: string;
  loaiToken: string;
  hetHanLuc: MocIso;
  refreshHetHanLuc: MocIso;
}

/** Ho so nguoi dung dang dang nhap - khop `NguoiDungDto`. */
export interface NguoiDung {
  id: Guid;
  username: string;
  fullname: string;
  email: string | null;
  chucvu: string | null;
  unitcode: string;
  unitname: string | null;
  vaitro: string;
  trangthai: number;
  maxConcurrentTasks: number;
}

/** Phan hoi cua A1 va A2 - khop `DangNhapResponse`. */
export interface DangNhapResponse {
  accessToken: string;
  refreshToken: string;
  loaiToken: string;
  hetHanLuc: MocIso;
  nguoiDung: NguoiDung;
}

/** §6.1 - MOT truc vai tro duy nhat. Khop `QLNV.Core.Constants.VaiTro`. */
export const VaiTro = {
  QuanTri: 'QUAN_TRI',
  NguoiGiao: 'NGUOI_GIAO',
  NguoiThucHien: 'NGUOI_THUC_HIEN'
} as const;

export type VaiTroType = (typeof VaiTro)[keyof typeof VaiTro];

/** Nhan tieng Viet cua vai tro (khop `VaiTro.Nhan`). */
export const NHAN_VAI_TRO: Record<string, string> = {
  [VaiTro.QuanTri]: 'Quản trị hệ thống',
  [VaiTro.NguoiGiao]: 'Người giao nhiệm vụ',
  [VaiTro.NguoiThucHien]: 'Người thực hiện'
};

/** §6.2 - cot QUAN_TRI / NGUOI_GIAO co dau. */
export function laBenGiao(vaiTro: string | null | undefined): boolean {
  return vaiTro === VaiTro.QuanTri || vaiTro === VaiTro.NguoiGiao;
}

/** §6.2 - chi cot NGUOI_THUC_HIEN co dau. */
export function laBenLam(vaiTro: string | null | undefined): boolean {
  return vaiTro === VaiTro.NguoiThucHien;
}
