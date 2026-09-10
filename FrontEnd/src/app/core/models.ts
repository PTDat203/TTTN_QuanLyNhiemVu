/**
 * Kiểu dữ liệu khớp đúng JSON mà BackEnd trả về.
 *
 * Tên trường phải trùng từng chữ với DTO phía C# (đã cấu hình camelCase).
 * Lệch một chữ thì TypeScript vẫn biên dịch được nhưng giá trị ra undefined lúc chạy —
 * loại lỗi im lặng khó tìm nhất khi nối frontend với backend.
 */

// ---------------------------------------------------------------- chung

export interface KetQuaPhanTrang<T> {
  danhSach: T[];
  trangHienTai: number;
  kichThuocTrang: number;
  tongSoDong: number;
  tongSoTrang: number;
  coTrangTruoc: boolean;
  coTrangSau: boolean;
}

/** Thân lỗi chuẩn ProblemDetails của ASP.NET Core. */
export interface LoiApi {
  title?: string;
  detail?: string;
  status?: number;
}

// ---------------------------------------------------------------- xác thực

export type VaiTro = 'MANAGER' | 'EMPLOYEE';

export interface NguoiDung {
  id: number;
  username: string;
  fullName: string;
  email?: string | null;
  role: VaiTro;
  tenVaiTro: string;
  status: string;
}

export interface DangNhapRequest {
  username: string;
  password: string;
}

export interface DangNhapResponse {
  accessToken: string;
  refreshToken: string;
  loaiToken: string;
  hetHanLuc: string;
  nguoiDung: NguoiDung;
}

// ---------------------------------------------------------------- nhiệm vụ

export type MaTrangThai =
  | 'MOI_TAO'
  | 'DA_GIAO'
  | 'DANG_THUC_HIEN'
  | 'CHO_XAC_NHAN'
  | 'YEU_CAU_BO_SUNG'
  | 'HOAN_THANH';

export type MucUuTien = 'LOW' | 'MEDIUM' | 'HIGH';

export interface NhiemVuTomTat {
  id: number;
  title: string;
  priority: MucUuTien;
  tenUuTien: string;
  statusCode: MaTrangThai;
  tenTrangThai: string;
  creatorId: number;
  tenNguoiTao?: string | null;
  assigneeId?: number | null;
  tenNguoiThucHien?: string | null;
  startDate?: string | null;
  dueDate?: string | null;
  soNgayConLai?: number | null;
  quaHan: boolean;
  tienDoPhanTram?: number | null;
  createdAt?: string | null;
  updatedAt?: string | null;
}

export interface TienDo {
  id: number;
  userId: number;
  tenNguoiCapNhat?: string | null;
  progressPercent: number;
  content?: string | null;
  createdAt?: string | null;
}

export type MaTrangThaiBaoCao = 'CHO_XAC_NHAN' | 'DA_XAC_NHAN' | 'TU_CHOI';

export interface BaoCao {
  id: number;
  reporterId: number;
  tenNguoiBaoCao?: string | null;
  content: string;
  reportStatus: MaTrangThaiBaoCao;
  tenTrangThaiBaoCao: string;
  reviewerId?: number | null;
  tenNguoiDuyet?: string | null;
  reviewNote?: string | null;
  createdAt?: string | null;
  reviewedAt?: string | null;
}

export interface TepDinhKem {
  id: number;
  reportId?: number | null;
  fileName: string;
  fileType?: string | null;
  fileSize?: number | null;
  uploadedBy: number;
  tenNguoiTaiLen?: string | null;
  uploadedAt?: string | null;
}

export interface NhiemVuChiTiet extends NhiemVuTomTat {
  description?: string | null;
  trangThaiKeTiep: MaTrangThai[];
  lichSuTienDo: TienDo[];
  danhSachBaoCao: BaoCao[];
  tepDinhKem: TepDinhKem[];
}

export interface NhiemVuLoc {
  statusCode?: string;
  priority?: string;
  assigneeId?: number;
  creatorId?: number;
  tuKhoa?: string;
  chuaGiao?: boolean;
  quaHan?: boolean;
  trang?: number;
  kichThuocTrang?: number;
  sapXep?: string;
  giamDan?: boolean;
}

export interface TaoNhiemVuRequest {
  title: string;
  description?: string | null;
  priority?: MucUuTien;
  startDate?: string | null;
  dueDate?: string | null;
  assigneeId?: number | null;
}

// ---------------------------------------------------------------- AI gợi ý

export interface ThanhPhanDiem {
  diem: number;
  trongSo: number;
  dongGop: number;
}

export interface ChiTietDiem {
  kyNang: ThanhPhanDiem;
  kinhNghiem: ThanhPhanDiem;
  dungHan: ThanhPhanDiem;
  khoiLuong: ThanhPhanDiem;
}

export interface SoLieuUngVien {
  soNhiemVuHoanThanh: number;
  soNhiemVuDungHan: number;
  soNhiemVuDangLam: number;
  taiHienTai: number;
  kyNangKhop: string[];
}

export interface UngVien {
  userId: number;
  fullName: string;
  diem: number;
  thuHang: number;
  chiTietDiem: ChiTietDiem;
  soLieu: SoLieuUngVien;
  lyDo: string[];
}

export interface GoiYResponse {
  phienBanTrongSo: string;
  noiDungDaDung: string;
  soUngVienDaXet: number;
  canhBao: string[];
  ungVien: UngVien[];
  thoiGianMs: number;
}

// ---------------------------------------------------------------- nhãn hiển thị

/** Màu cho từng trạng thái, dùng chung để giao diện nhất quán. */
export const MAU_TRANG_THAI: Record<MaTrangThai, string> = {
  MOI_TAO: '#6b7280',
  DA_GIAO: '#2563eb',
  DANG_THUC_HIEN: '#0891b2',
  CHO_XAC_NHAN: '#d97706',
  YEU_CAU_BO_SUNG: '#dc2626',
  HOAN_THANH: '#16a34a',
};

export const MAU_UU_TIEN: Record<MucUuTien, string> = {
  LOW: '#6b7280',
  MEDIUM: '#d97706',
  HIGH: '#dc2626',
};

export const DANH_SACH_TRANG_THAI: { ma: MaTrangThai; ten: string }[] = [
  { ma: 'MOI_TAO', ten: 'Mới tạo' },
  { ma: 'DA_GIAO', ten: 'Đã giao' },
  { ma: 'DANG_THUC_HIEN', ten: 'Đang thực hiện' },
  { ma: 'CHO_XAC_NHAN', ten: 'Chờ xác nhận' },
  { ma: 'YEU_CAU_BO_SUNG', ten: 'Yêu cầu bổ sung' },
  { ma: 'HOAN_THANH', ten: 'Hoàn thành' },
];
