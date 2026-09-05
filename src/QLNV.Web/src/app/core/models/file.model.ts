import { Guid, MocIso } from './common.model';

/** G1 - mot tep dinh kem (`FileDto`). */
export interface TepDinhKem {
  id: Guid;
  fileName: string;
  filePath: string;
  size: number;
  contentType: string | null;
  loaiBanGhi: string | null;
  recordId: Guid | null;
  createBy: Guid;
  createDate: MocIso;
}

/** Loai ban ghi ma tep gan vao (`LoaiBanGhiFile`). */
export const LoaiBanGhiFile = {
  VanBan: 'VANBAN',
  NhiemVu: 'NHIEMVU',
  XuLy: 'XULY',
  GiaHan: 'GIAHAN'
} as const;

/** §5.7 G1 - gioi han tep (khop `QLNV.Core.Constants.GioiHan`). */
export const GIOI_HAN_TEP = {
  /** 20 MB moi tep. */
  kichThuocToiDa: 20 * 1024 * 1024,
  duoiChoPhep: ['.pdf', '.doc', '.docx', '.xls', '.xlsx', '.png', '.jpg', '.jpeg']
};
