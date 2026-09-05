import { Guid, MocIso, NgayIso } from './common.model';
import { NguoiDungTomTat, TuDien } from './danh-muc.model';
import { TepDinhKem } from './file.model';

/* =====================================================================
   C1 - Tao & giao nhieu nhiem vu mot lan (M05)
   POST /api/v1/van-ban/{idvb}/nhiem-vu
   ===================================================================== */

/** LUU Y CHINH TA: khoa JSON la `boQuaTrungNoidung` (chu "d" thuong). */
export interface TaoNhiemVuRequest {
  boQuaTrungNoidung: boolean;
  items: TaoNhiemVuItem[];
}

export interface TaoNhiemVuItem {
  /** Bat buoc, <= 2000 ky tu (§3 M05). */
  noidung: string;
  linhvuc?: string | null;
  /** TRONGTAM | THUONGXUYEN | DOTXUAT. */
  dokhan: string;
  hanxulyth?: NgayIso | null;
  songayhxlth?: number | null;
  hanxulyph?: NgayIso | null;
  /** Bat buoc >= 1 nguoi chu tri. */
  chuTri: Guid[];
  phoiHop: Guid[];
  /** LUU Y CHINH TA: khoa JSON la `aiGoiyId` (chu "y" thuong). */
  aiGoiyId?: Guid | null;
  fileIds?: Guid[] | null;
}

export interface TaoNhiemVuResponse {
  ids: Guid[];
  soLuong: number;
  /** Khac rong => BE tu choi luu, FE phai hoi lai roi gui `boQuaTrungNoidung = true`. */
  trungNoiDung: TrungNoiDung[];
}

/** C2 - do trung noi dung truoc khi luu. Fail-open: loi ky thuat => mang rong. */
export interface KiemTraTrungRequest {
  items: KiemTraTrungItem[];
}

export interface KiemTraTrungItem {
  /** So thu tu dong trong luoi M05 (de FE to do dung dong). */
  tt: number;
  noiDung: string;
  dsUserChuTri: Guid[];
}

export interface TrungNoiDung {
  tt: number;
  noiDung: string;
  idNhiemVuTrung: Guid | null;
  noiDungTrung: string | null;
  thongBao: string;
}

/* =====================================================================
   C3 - Danh sach nhiem vu theo vai (M08)
   ===================================================================== */

/** Bo loc theo vai (`BoLocVaiTro`). */
export const BoLocVaiTro = {
  ToiGiao: 'TOI_GIAO',
  ToiLam: 'TOI_LAM'
} as const;

export type BoLocVaiTroType = (typeof BoLocVaiTro)[keyof typeof BoLocVaiTro];

/**
 * Tham so loc gui bang query string.
 *
 * CANH BAO RANG BUOC: ASP.NET Core rang buoc query theo TEN THUOC TINH C#,
 * KHONG theo [JsonPropertyName]. So sanh khong phan biet hoa thuong, nen
 * dung dung cac khoa duoi day: vaiTro, trangThai, trangThaiDvXuly, quaHan,
 * sapHetHan, idVb, linhVuc, doKhan, search, page, size.
 * Mang gui bang cach lap khoa: `?trangThai=2&trangThai=7`.
 */
export interface NhiemVuLocRequest {
  vaiTro?: string | null;
  trangThai?: number[] | null;
  trangThaiDvXuly?: number[] | null;
  quaHan?: boolean | null;
  sapHetHan?: boolean | null;
  idVb?: Guid | null;
  linhVuc?: string | null;
  doKhan?: string | null;
  search?: string | null;
  page?: number;
  size?: number;
}

/* =====================================================================
   §6.2 - 15 co quyen do BE tinh (`QuyenNhiemVuDto`)
   ===================================================================== */

/**
 * §6.4 - FE PHAI tu tinh lai bang bieu thuc §6.2 (xem `core/trang-thai/quyen.util.ts`)
 * de an/hien nut. Cac co duoi day chi dung DOI CHIEU / go loi, KHONG duoc coi la
 * nguon su that duy nhat cho giao dien.
 */
export interface QuyenNhiemVu {
  suaNhiemVu: boolean;
  thuHoiNhiemVu: boolean;
  thuHoiPhanCong: boolean;
  tiepNhan: boolean;
  tuChoi: boolean;
  capNhatTienDo: boolean;
  guiBaoCao: boolean;
  thuHoiBaoCao: boolean;
  kiemTraKetQua: boolean;
  xinGiaHan: boolean;
  duyetGiaHan: boolean;
  nhacViec: boolean;
  xemChiTiet: boolean;
  taiTep: boolean;
  /** MO RONG §2.4 T4/T5 - §6.2 khong co dong rieng cho hanh dong nay. */
  xuLyTuChoi: boolean;
}

/* =====================================================================
   Nhiem vu (`NhiemVuDto`) - §7.4 muc 1: giu nguyen ten truong cua he goc
   ===================================================================== */

export interface NhiemVu {
  id: Guid;
  idvb: Guid;
  soKyHieuVanBan: string | null;
  trichYeuVanBan: string | null;
  noidung: string;
  linhvuc: string | null;
  tenLinhVuc: string | null;
  dokhan: string;
  hanxulyth: NgayIso | null;
  songayhxlth: number | null;
  hanxulyph: NgayIso | null;
  ngaygiao: MocIso;
  ngaytiepnhan: MocIso | null;
  ngayhoanthanhthucte: MocIso | null;

  /** Truc A (§2.1): 1 | 2 | 3 | 5 | 6 | 7 | 13 | 97. */
  trangthai: number;
  tenTrangThai: string | null;
  /** Truc B (§2.2): null | 10 | 11 | 12. */
  trangthaiDvXuly: number | null;
  tenTrangThaiDvXuly: string | null;
  /** Truc C (§2.3): null | 10 | 11 | 12. */
  trangthaixulygiahan: number | null;
  /** Toi da 2 (§2.3). */
  solangiahan: number;
  /** Tien do 0..100. */
  mucdoht: number | null;
  phanhoi: string | null;
  /** He so chat luong 1..6 khi nghiem thu DAT. */
  hsChatluong: number | null;

  userIdGiaoViec: Guid;
  nguoiGiaoTen: string | null;
  useridcreate: Guid;
  unitcode: string;
  createdate: MocIso;
  updatedate: MocIso;
  /** LUU Y CHINH TA: `aiGoiyId`. */
  aiGoiyId: Guid | null;

  /** BE tinh san theo `hanxulyth` va moc "hom nay" cua request. */
  soNgayConLai: number | null;
  quaHan: boolean;
  sapHetHan: boolean;
  /** §2.6 - "HOAN_THANH_NGHIEM_THU" | "DA_THU_HOI" | null. */
  diemCuoi: string | null;

  chuTri: NguoiDungTomTat[];
  phoiHop: NguoiDungTomTat[];
  quyen: QuyenNhiemVu | null;
}

/** §2.6 - hai diem cuoi (`DiemCuoi`). */
export const DiemCuoi = {
  HoanThanhNghiemThu: 'HOAN_THANH_NGHIEM_THU',
  DaThuHoi: 'DA_THU_HOI'
} as const;

/** C4 - chi tiet nhiem vu (`NhiemVuChiTietDto`). */
export interface NhiemVuChiTiet {
  nhiemVu: NhiemVu;
  phanCong: PhanCong[];
  lichSuXuLy: XuLy[];
  lichSuGiaHan: GiaHan[];
  files: TepDinhKem[];
  /** D5 - danh sach trang thai duoc chon khi bao cao, DA LOC THEO HAN. */
  trangThaiHopLe: TuDien[];
}

/** Mot dong cua bang NHIEMVU_PHANCONG (`PhanCongDto`). */
export interface PhanCong {
  id: Guid;
  userid: Guid;
  fullname: string;
  chucvu: string | null;
  unitcode: string;
  unitname: string | null;
  /** CHUTRI | PHOIHOP. */
  vaitro: string;
  /** 1 = con hieu luc, 0 = da thu hoi. */
  trangthai: number;
  createdate: MocIso;
}

/** C5 - sua nhiem vu da giao (chi khi `trangthai = 3`). */
export interface SuaNhiemVuRequest {
  noidung: string;
  linhvuc?: string | null;
  dokhan: string;
  hanxulyth?: NgayIso | null;
  songayhxlth?: number | null;
  hanxulyph?: NgayIso | null;
  fileIds?: Guid[] | null;
}

/** C6 - thu hoi ca nhiem vu (ve 97). */
export interface ThuHoiNhiemVuRequest {
  lyDo?: string | null;
}

/** C7 - rut phan cong cua mot so nguoi. */
export interface ThuHoiPhanCongRequest {
  userIds: Guid[];
  lyDo?: string | null;
}

/** §1.3 - nhac viec. */
export interface NhacViecRequest {
  noidung: string;
}

/* =====================================================================
   §5.4 - cac hanh dong thuc hien (M09)
   ===================================================================== */

/** D1 - tiep nhan nhiem vu (3 sang 2). */
export interface TiepNhanRequest {
  noidung?: string | null;
}

/** D2 - tu choi nhiem vu. `lyDo` BAT BUOC (§10.7). */
export interface TuChoiRequest {
  lyDo: string;
  fileIds?: Guid[] | null;
}

/** D3 - cap nhat tien do. KHONG doi trang thai. */
export interface TienDoRequest {
  /** 0..100. */
  mucdoht: number;
  noidung?: string | null;
  fileIds?: Guid[] | null;
}

/** D4 - gui bao cao ket qua. `trangthai` phai nam trong danh sach D5. */
export interface BaoCaoRequest {
  trangthai: number;
  noidung?: string | null;
  mucdoht?: number | null;
  fileIds?: Guid[] | null;
}

/** D6 - thu hoi bao cao. */
export interface ThuHoiBaoCaoRequest {
  lyDo?: string | null;
}

/** E1 - nghiem thu ket qua (M07). */
export interface NghiemThuRequest {
  /** DAT | CHUA_DAT. */
  ketQua: string;
  /** Bat buoc (§10.7 - app moi bat buoc nhap phan hoi). */
  phanHoi: string;
  /** 1..6, chi gui khi ketQua = DAT. */
  hsChatluong?: number | null;
}

/** §2.4 T4/T5 - nguoi giao xu ly de nghi tu choi tai cap (6, 10). */
export interface XuLyTuChoiRequest {
  /** CHAP_NHAN (ve 97) | BAC_BO (ve (2,12)). */
  ketQua: string;
  phanHoi?: string | null;
}

/** Ket qua nghiem thu (`KetQuaNghiemThu`). */
export const KetQuaNghiemThu = { Dat: 'DAT', ChuaDat: 'CHUA_DAT' } as const;

/** Ket qua duyet gia han (`KetQuaDuyetGiaHan`). */
export const KetQuaDuyetGiaHan = { Duyet: 'DUYET', TuChoi: 'TU_CHOI' } as const;

/** Ket qua xu ly de nghi tu choi (`KetQuaXuLyTuChoi`). */
export const KetQuaXuLyTuChoi = { ChapNhan: 'CHAP_NHAN', BacBo: 'BAC_BO' } as const;

/* =====================================================================
   Lich su xu ly / gia han
   ===================================================================== */

/** Mot dong lich su XULY_NHIEMVU (`XuLyDto`). */
export interface XuLy {
  id: Guid;
  idCtnv: Guid;
  /** Xem hang `LoaiXuLy` ben duoi. */
  loai: string;
  tenLoai: string | null;
  noidung: string | null;
  mucdoht: number | null;
  trangthai: number | null;
  tenTrangThai: string | null;
  trangthaiXuly: number | null;
  /** Bo sung ngoai §4.4 - can cho §9.4 S3b (dem so lan bi tra lai). */
  trangthaiDvXuly: number | null;
  useridXuly: Guid;
  nguoiXuLyTen: string | null;
  ngayxuly: MocIso;
  files: TepDinhKem[];
}

/** D7 - lich su xu ly + gia han (`LichSuNhiemVuDto`). */
export interface LichSuNhiemVu {
  xuLy: XuLy[];
  giaHan: GiaHan[];
}

/** Loai ban ghi lich su (`LoaiXuLy`). */
export const LoaiXuLy = {
  TiepNhan: 'TIEPNHAN',
  TienDo: 'TIENDO',
  BaoCao: 'BAOCAO',
  TuChoi: 'TUCHOI',
  ThuHoiBaoCao: 'THUHOI_BC',
  NghiemThu: 'NGHIEMTHU',
  XuLyTuChoi: 'XL_TUCHOI',
  GiaHan: 'GIAHAN',
  DuyetGiaHan: 'DUYET_GIAHAN',
  ThuHoiNhiemVu: 'THUHOI_NV',
  NhacViec: 'NHACVIEC'
} as const;

/** Nhan tieng Viet cua loai xu ly (khop `LoaiXuLy.Nhan`). */
export const NHAN_LOAI_XU_LY: Record<string, string> = {
  [LoaiXuLy.TiepNhan]: 'Tiếp nhận nhiệm vụ',
  [LoaiXuLy.TienDo]: 'Cập nhật tiến độ',
  [LoaiXuLy.BaoCao]: 'Gửi báo cáo kết quả',
  [LoaiXuLy.TuChoi]: 'Từ chối nhiệm vụ',
  [LoaiXuLy.ThuHoiBaoCao]: 'Thu hồi báo cáo',
  [LoaiXuLy.NghiemThu]: 'Kiểm tra kết quả',
  [LoaiXuLy.XuLyTuChoi]: 'Xử lý đề nghị từ chối',
  [LoaiXuLy.GiaHan]: 'Đề xuất gia hạn',
  [LoaiXuLy.DuyetGiaHan]: 'Duyệt gia hạn',
  [LoaiXuLy.ThuHoiNhiemVu]: 'Thu hồi nhiệm vụ',
  [LoaiXuLy.NhacViec]: 'Nhắc việc'
};

/* =====================================================================
   §5.6 - Gia han (M10)
   ===================================================================== */

/** F1 - de xuat gia han. LUU Y: khoa `noiDung` (chu D hoa), khac cac DTO khac. */
export interface GiaHanRequest {
  hanxulydexuat: NgayIso;
  noiDung?: string | null;
  fileIds?: Guid[] | null;
}

/** F2 - duyet / tu choi gia han. */
export interface DuyetGiaHanRequest {
  /** DUYET | TU_CHOI. */
  ketQua: string;
  phanHoi?: string | null;
  /** Tuy chon - de doi chieu voi ban ghi gia han dang cho duyet. */
  idGiaHan?: Guid | null;
}

/** F3 - mot ban ghi GIAHAN_NHIEMVU (`GiaHanDto`). */
export interface GiaHan {
  id: Guid;
  idCtnv: Guid;
  noiDung: string | null;
  hanxulydexuat: NgayIso;
  /** LUU Y: khoa JSON co gach duoi - `hanxulyth_cu`. */
  hanxulyth_cu: NgayIso | null;
  /** Truc C: 10 cho duyet | 11 da duyet | 12 tu choi. LUU Y: `trangThai` chu T hoa. */
  trangThai: number;
  tenTrangThai: string | null;
  phanhoi: string | null;
  useridDexuat: Guid;
  nguoiDeXuatTen: string | null;
  useridDuyet: Guid | null;
  nguoiDuyetTen: string | null;
  createDate: MocIso;
  ngayduyet: MocIso | null;
  files: TepDinhKem[];
}

/** Phan hoi cua F1 va F2 (kieu an danh do controller tra ve). */
export interface KetQuaGiaHan {
  nhiemVu: NhiemVu;
  lichSuGiaHan: GiaHan[];
}
