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

/** Quyền trong hệ thống, khớp cột USERS.USER_ROLE. Khác chức danh (jobTitle) chỉ để hiển thị. */
export type VaiTro = 'DIRECTOR' | 'DEPT_HEAD' | 'TEAM_LEAD' | 'EMPLOYEE';

/** Các vai trò được giao việc cho người khác — khớp VaiTro.NhomGiaoViec phía backend. */
export const VAI_TRO_GIAO_VIEC: readonly VaiTro[] = ['DIRECTOR', 'DEPT_HEAD', 'TEAM_LEAD'];

export interface NguoiDung {
  id: number;
  username: string;
  fullName: string;
  email?: string | null;
  role: VaiTro;
  tenVaiTro: string;
  status: string;
  jobTitle?: string | null;
  departmentId?: number | null;
  tenPhongBan?: string | null;
  teamId?: number | null;
  tenNhom?: string | null;
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
  /** Phòng thực thi — AI đoán khi tạo, hoặc lấy theo người nhận khi giao. */
  departmentId?: number | null;
  tenPhongBan?: string | null;
  teamId?: number | null;
  tenNhom?: string | null;
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
  /** Chất lượng kết quả, thang 1..5. Rỗng khi chưa duyệt. */
  qualityScore?: number | null;
  /** Mức đáp ứng đủ yêu cầu, thang 1..5. Rỗng khi chưa duyệt. */
  completionScore?: number | null;
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
  /** Điểm của thành phần, 0..1. */
  diem: number;
  trongSo: number;
  /** Phần đóng góp vào điểm tổng = diem × trongSo. */
  dongGop: number;
}

/** Sáu thành phần cấu thành điểm phù hợp. */
export interface ChiTietDiem {
  nguNghia: ThanhPhanDiem;
  mucKyNang: ThanhPhanDiem;
  hieuSuat: ThanhPhanDiem;
  viecTuongTu: ThanhPhanDiem;
  dungHan: ThanhPhanDiem;
  khoiLuong: ThanhPhanDiem;
}

export interface ViecTuongTu {
  taskId: number;
  tieuDe: string;
  doGan: number;
  chatLuong?: number | null;
}

export interface SoLieuUngVien {
  soNhiemVuHoanThanh: number;
  soNhiemVuDungHan: number;
  soNhiemVuDangLam: number;
  taiHienTai: number;
  chatLuongTrungBinh?: number | null;
  /** Chưa hoàn thành việc nào — hiệu suất và đúng hạn đang là giá trị mặc định. */
  chuaCoLichSu: boolean;
  kyNangKhop: string[];
  kyNangThieu: string[];
  viecTuongTu: ViecTuongTu[];
}

export interface UngVien {
  userId: number;
  fullName: string;
  chucDanh?: string | null;
  tenPhongBan?: string | null;
  tenNhom?: string | null;
  diem: number;
  thuHang: number;
  chiTietDiem: ChiTietDiem;
  soLieu: SoLieuUngVien;
  lyDo: string[];
}

export interface DiemDonVi {
  id: number;
  ten: string;
  diem: number;
  diemHoSo: number;
  diemLichSu?: number | null;
}

export type KetLuanPhongBan = 'CHAC_CHAN' | 'LUONG_LU' | 'KHONG_RO';

/** AI đoán nhiệm vụ thuộc phòng nào — tầng lọc thứ nhất. */
export interface SuyLuanPhongBan {
  ketLuan: KetLuanPhongBan;
  moTa: string;
  cacPhong: DiemDonVi[];
  phongDaChon: number[];
  nhom?: DiemDonVi | null;
}

export interface KyNangYeuCau {
  skillId: number;
  code: string;
  ten: string;
  mucYeuCau?: number | null;
  /** MANUAL = người giao nhập; AI = trích tự động từ nội dung. */
  nguon: 'AI' | 'MANUAL';
  doKhop?: number | null;
}

export interface GoiYResponse {
  phienBanTrongSo: string;
  /** Nhúng ngữ nghĩa, hoặc TF-IDF khi dịch vụ AI không phản hồi. */
  phuongPhap: string;
  noiDungDaDung: string;
  soUngVienTrongPhamVi: number;
  soUngVienDaXet: number;
  suyLuanPhongBan: SuyLuanPhongBan;
  kyNangYeuCau: KyNangYeuCau[];
  canhBao: string[];
  ungVien: UngVien[];
  thoiGianMs: number;
}

/**
 * Sáu thành phần điểm theo thứ tự trọng số giảm dần, kèm tên và màu. Dùng chung cho thanh
 * phân rã điểm và chú giải, để hai chỗ không bao giờ lệch màu nhau.
 */
export const THANH_PHAN_DIEM: readonly { khoa: keyof ChiTietDiem; ten: string; mau: string }[] = [
  { khoa: 'nguNghia', ten: 'Ngữ nghĩa', mau: '#2563eb' },
  { khoa: 'mucKyNang', ten: 'Mức kỹ năng', mau: '#7c3aed' },
  { khoa: 'hieuSuat', ten: 'Hiệu suất', mau: '#0891b2' },
  { khoa: 'viecTuongTu', ten: 'Việc tương tự', mau: '#db2777' },
  { khoa: 'dungHan', ten: 'Đúng hạn', mau: '#16a34a' },
  { khoa: 'khoiLuong', ten: 'Khối lượng', mau: '#d97706' },
];

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
