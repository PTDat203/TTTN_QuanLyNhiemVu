import { Guid, MocIso, NgayIso } from './common.model';

/* =====================================================================
   H1 - POST /api/v1/ai/goi-y-nguoi-thuc-hien   (§9.6)
   Endpoint TRONG TAM cua de tai. Man M06 dung truc tiep hop dong nay.
   ===================================================================== */

export interface GoiYRequest {
  /** Khi goi y cho mot nhiem vu da ton tai. */
  idnvchitiet?: Guid | null;
  noidung?: string | null;
  linhvuc?: string | null;
  dokhan?: string | null;
  hanxulyth?: NgayIso | null;
  /** Gioi han pham vi don vi (o loc theo don vi cua M06). */
  phamViUnitCode?: string[];
  /** Loai tru nguoi da chon o cac dong khac. */
  loaiTru?: Guid[];
  /** Mac dinh 5, toi da 50. Nut "Xem them 5 nguoi" gui soLuong = 10. */
  soLuong?: number;
  /** §9.3 loc cung - nguoi da duoc phan cong cho chinh nhiem vu nay. */
  daPhanCong?: Guid[];
  /** §9.3 loc cung - nguoi da tung tu choi chinh nhiem vu nay. */
  daTuChoi?: Guid[];
}

export interface GoiYResponse {
  /** LUU Y CHINH TA: `goiyId` (chu y thuong). Dung cho H2 va cho `aiGoiyId` cua C1. */
  goiyId: Guid;
  phienBanTrongSo: string;
  /** DAY_DU | KHOI_TAO (§9.5 cold start). */
  cheDo: string;
  canhBao: string[];
  ungVien: UngVien[];
  /** Bo sung ngoai §9.6. */
  idnvchitiet: Guid | null;
  linhvuc: string | null;
  tongSoUngVien: number;
  daLocTrungLap: boolean;
  loaiBo: UngVienBiLoai[];
}

export interface UngVien {
  userid: Guid;
  fullname: string;
  chucvu: string | null;
  unitname: string | null;
  /** Thang 0..100 (§9.7 - thanh diem tong dung MOT mau trung tinh). */
  diemTong: number;
  /** 0..1 - do tin cay cua goi y. */
  doTinCay: number;
  nhan: NhanUngVien[];
  /** Nhan dang chuoi, tien cho truong hop chi can in nhanh. */
  nhanText: string[];
  diemThanhPhan: DiemThanhPhan;
  /** Toi da 4 dong (§9.7). Thu tu uu tien: chuyen mon, kinh nghiem, hieu qua, khoi luong. */
  lyDo: string[];
  soLieu: SoLieuUngVien;
}

/** §9.4 - nam thanh phan diem. Tong `gopPhan` = `diemTong`. */
export interface DiemThanhPhan {
  chuyenMon: ThanhPhanDiem;
  lichSu: ThanhPhanDiem;
  hieuQua: ThanhPhanDiem;
  khoiLuong: ThanhPhanDiem;
  sanSang: ThanhPhanDiem;
}

export interface ThanhPhanDiem {
  /** 0..1. */
  diem: number;
  /** Trong so w1..w5. */
  trongSo: number;
  /** diem * trongSo * 100. */
  gopPhan: number;
}

export interface NhanUngVien {
  ma: string;
  nhan: string;
  mau: string;
}

/** Ma nhan ung vien (`MaNhanUngVien`). */
export const MaNhanUngVien = {
  NguoiMoi: 'NGUOI_MOI',
  DuLieuIt: 'DU_LIEU_IT',
  QuaTai: 'QUA_TAI',
  CoQuaHan: 'CO_QUA_HAN',
  BiTraLai: 'BI_TRA_LAI'
} as const;

/** Che do goi y (`CheDoGoiY`). */
export const CheDoGoiY = {
  DayDu: 'DAY_DU',
  /** §9.5 - he thong chua du du lieu lich su. */
  KhoiTao: 'KHOI_TAO'
} as const;

export interface SoLieuUngVien {
  soNvHoanThanhLinhVuc: number;
  soNvHoanThanh: number;
  soNvDungHan: number;
  soNvBiTraLai: number;
  soLanGiaHan: number;
  soNvDangMo: number;
  taiTrongSo: number;
  soNvQuaHan: number;
  /** LUU Y: khoa JSON la chu "K" HOA. */
  K: number;
  tyLeDungHan: number;
  /** LUON null o v1 (§9.2/§10.1 bo bang khai bao nang luc). Giu khoa de FE khong in "undefined". */
  mucThanhThao: number | null;
  /** LUON null o v1 (§9.4/§10.2 loai hsChatluong khoi cong thuc). */
  diemChatLuongTb: number | null;
  soNvDuocGiao: number;
}

export interface UngVienBiLoai {
  userid: Guid;
  fullname: string;
  lyDo: string;
}

/** H2 - ghi nhan nguoi giao da chon ai. `useridDaChon = null` nghia la BO QUA goi y. */
export interface GhiKetQuaGoiYRequest {
  useridDaChon?: Guid | null;
}

/* =====================================================================
   H3 - GET /api/v1/ai/thong-ke   (§9.8) - dung cho M13
   ===================================================================== */

export interface ThongKeAi {
  soLanGoiY: number;
  soLanChapNhan: number;
  soLanBoQua: number;
  tyLeChapNhan: number;
  precision1: number;
  precision3: number;
  mrr: number;
  /** He so Gini cua tai - do dong deu khi phan bo cong viec. */
  giniTai: number;
  phanBoThuHang: PhanBoThuHang[];
  phanBoDiem: PhanBoDiem[];
  nguong: NguongMucTieu;
  dat: DatNguong;
}

export interface PhanBoThuHang {
  /** null = lan goi y bi bo qua. */
  thuHang: number | null;
  nhan: string;
  soLan: number;
}

export interface PhanBoDiem {
  khoang: string;
  soLan: number;
}

export interface NguongMucTieu {
  precision1: number;
  precision3: number;
  tyLeChapNhan: number;
  mrr: number;
}

export interface DatNguong {
  precision1: boolean;
  precision3: boolean;
  tyLeChapNhan: boolean;
  mrr: boolean;
}

/** GET /api/v1/ai/so-sanh-baseline - doi chieu voi cac chien luoc don gian. */
export interface SoSanhBaseline {
  chienLuoc: string;
  nhan: string;
  precision1: number;
  precision3: number;
  mrr: number;
  giniTai: number;
}

/* =====================================================================
   H4 - GET/PUT /api/v1/ai/cau-hinh - bo trong so w1..w5
   ===================================================================== */

export interface CauHinhAi {
  /** Chuyen mon (mac dinh 0.30). */
  w1: number;
  /** Lich su cung linh vuc (0.20). */
  w2: number;
  /** Hieu qua (0.25). */
  w3: number;
  /** Khoi luong dang gánh (0.20). */
  w4: number;
  /** San sang (0.05). */
  w5: number;
  nguongChuyenMon: number;
  nguongLichSu: number;
  /** Diem hieu qua tien nghiem cho nguoi moi (0.70). */
  p0: number;
  /** Hang so lam muot Laplace (5). */
  alpha: number;
  phatTraLai: number;
  phatGiaHan: number;
  heSoQuaTai: number;
  phienBan: string;
}

/** POST /api/v1/ai/cau-hinh/kiem-tra - kiem tra tong trong so truoc khi luu. */
export interface KiemTraCauHinhAi {
  tongTrongSo: number;
  hopLe: boolean;
  canhBao: string[];
}

/* =====================================================================
   GET /api/v1/ai/nhat-ky   - M13 Nhat ky goi y AI
   ===================================================================== */

export interface AiGoiYLog {
  id: Guid;
  idnvchitiet: Guid | null;
  noiDungNhiemVu: string | null;
  linhvuc: string | null;
  ungVien: UngVienTomTatLog[];
  useridDaChon: Guid | null;
  nguoiDaChonTen: string | null;
  /** null = nguoi giao khong chon ai trong danh sach goi y. */
  thuHangDaChon: number | null;
  /** Chot cung TOP-5, khong theo `soLuong` cua request (de §9.8 so sanh duoc). */
  coTrongGoiY: boolean;
  phienBanTrongSo: string | null;
  cheDo: string | null;
  createdate: MocIso;
}

export interface UngVienTomTatLog {
  thuHang: number;
  userid: Guid;
  fullname: string;
  diemTong: number;
  doTinCay: number;
  nhan: string[];
}
