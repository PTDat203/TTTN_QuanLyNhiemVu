import { Guid } from './common.model';

/** A4 - mot ban ghi DM_TUDIEN (`TuDienDto`). */
export interface TuDien {
  type: string;
  ma: string;
  nhan: string;
  mau: string | null;
  mota: string | null;
  thutu: number;
}

/** A5 - node cua cay linh vuc (`LinhVucDto`). */
export interface LinhVuc {
  ma: string;
  ten: string;
  nhomCha: string | null;
  trangthai: number;
  thutu: number;
  con: LinhVuc[];
}

/** A6 - node cua cay don vi (`DonViDto`). */
export interface DonVi {
  unitcode: string;
  tendonvi: string;
  macha: string | null;
  capdonvi: number;
  trangthai: number;
  nguoiDung: NguoiDungTomTat[];
  con: DonVi[];
}

/** Nguoi dung rut gon dung trong cay don vi va danh sach phan cong. */
export interface NguoiDungTomTat {
  userid: Guid;
  fullname: string;
  chucvu: string | null;
  unitcode: string;
  unitname: string | null;
  vaitro: string | null;
}

/** §7.4 muc 3 - 4 ma type cua DM_TUDIEN (`MaTypeTuDien`). */
export const MaTypeTuDien = {
  TrangThaiNv: 'TRANGTHAINV',
  TrangThaiPh: 'TRANGTHAIPH',
  LoaiVb: 'LOAIVB',
  DoKhan: 'DOKHAN'
} as const;

export type MaTypeTuDienType = (typeof MaTypeTuDien)[keyof typeof MaTypeTuDien];

/** §7.4 muc 4 - danh muc do khan (dung hang `MUCDOUUTIEN` cua he goc). */
export const DoKhan = {
  TrongTam: 'TRONGTAM',
  ThuongXuyen: 'THUONGXUYEN',
  DotXuat: 'DOTXUAT'
} as const;

export type DoKhanType = (typeof DoKhan)[keyof typeof DoKhan];

/** Nhan tieng Viet cua do khan (khop `DoKhan.Nhan`). */
export const NHAN_DO_KHAN: Record<string, string> = {
  [DoKhan.DotXuat]: 'Đột xuất',
  [DoKhan.TrongTam]: 'Trọng tâm',
  [DoKhan.ThuongXuyen]: 'Thường xuyên'
};

/** Mau cua do khan (khop cot `mau` do BE seed). */
export const MAU_DO_KHAN: Record<string, string> = {
  [DoKhan.DotXuat]: 'do',
  [DoKhan.TrongTam]: 'cam',
  [DoKhan.ThuongXuyen]: 'xam'
};

/** Vai tro trong bang NHIEMVU_PHANCONG (`VaiTroPhanCong`). */
export const VaiTroPhanCong = {
  ChuTri: 'CHUTRI',
  PhoiHop: 'PHOIHOP'
} as const;

export type VaiTroPhanCongType = (typeof VaiTroPhanCong)[keyof typeof VaiTroPhanCong];

export const NHAN_VAI_TRO_PHAN_CONG: Record<string, string> = {
  [VaiTroPhanCong.ChuTri]: 'Chủ trì',
  [VaiTroPhanCong.PhoiHop]: 'Phối hợp'
};

/** Trang thai ban ghi phan cong (`TrangThaiPhanCong`). */
export const TrangThaiPhanCong = {
  ConHieuLuc: 1,
  DaThuHoi: 0
} as const;
