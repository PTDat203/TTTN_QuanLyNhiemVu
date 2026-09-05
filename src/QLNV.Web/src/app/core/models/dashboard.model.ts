import { Guid, NgayIso } from './common.model';

/* =====================================================================
   J1 - GET /api/v1/dashboard/tong-quan   (M02)
   ===================================================================== */

export interface DashboardTongQuan {
  /** Moc "hom nay" ma BE dung de tinh qua han / sap het han. */
  homNay: NgayIso;

  /** 4 the dem cua M02. */
  chuaTrienKhai: number;
  dangTrienKhai: number;
  choXacNhan: number;
  quaHan: number;

  sapHetHan: number;
  hoanThanh: number;
  tongSo: number;
  /** 0..1. */
  tyLeHoanThanh: number;

  /** Bieu do tron theo trang thai truc A. */
  theoTrangThai: DemTheoTrangThai[];
  /** Bieu do cot theo don vi (J2). */
  theoDonVi: DashboardDonVi[];
  /** Danh sach "Viec cua toi sap den han (<= 3 ngay)". */
  sapDenHan: NhiemVuSapDenHan[];
}

export interface DemTheoTrangThai {
  /** Ma truc A (§2.1). */
  ma: number;
  nhan: string;
  /** Ten mau tieng Viet do DM_TUDIEN seed: xanh|duong|xam|cam|do|tim|vang. */
  mau: string | null;
  soLuong: number;
  /** 0..1. */
  tyLe: number;
}

/** J2 - GET /api/v1/dashboard/theo-don-vi. */
export interface DashboardDonVi {
  unitcode: string;
  tendonvi: string;
  tongSo: number;
  hoanThanh: number;
  quaHan: number;
  tyLeHoanThanh: number;
}

/** GET /api/v1/dashboard/sap-den-han?soNgay=3. */
export interface NhiemVuSapDenHan {
  id: Guid;
  noidung: string;
  hanxulyth: NgayIso | null;
  soNgayConLai: number | null;
  dokhan: string;
  trangthai: number;
  quaHan: boolean;
}
