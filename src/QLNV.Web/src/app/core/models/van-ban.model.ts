import { Guid, MocIso, NgayIso } from './common.model';
import { TepDinhKem } from './file.model';

/** B1 - tham so loc danh sach van ban (gui bang query string). */
export interface VanBanLocRequest {
  page?: number;
  size?: number;
  search?: string | null;
  /** LUU Y: rang buoc query cua ASP.NET dung TEN THUOC TINH C# (`LinhVuc`),
   *  khong dung `[JsonPropertyName]`; so sanh KHONG phan biet hoa thuong
   *  nen ca `linhVuc` lan `linhvuc` deu rang buoc dung. */
  linhVuc?: string | null;
  tuNgay?: NgayIso | null;
  denNgay?: NgayIso | null;
}

/** B1/B2 - van ban chi dao (`VanBanDto`). */
export interface VanBan {
  id: Guid;
  sokyhieu: string | null;
  trichyeu: string;
  loaivb: string | null;
  ngaybanhanh: NgayIso | null;
  coquanbanhanh: string | null;
  dokhan: string;
  linhvuc: string | null;
  thoigianchidao: MocIso | null;
  nguonnv: string | null;
  nguoitheodoi: string | null;
  unitcode: string;
  useridcreate: Guid;
  nguoiTaoTen: string | null;
  createdate: MocIso;
  /** M03 - cot "So nhiem vu". */
  tongSoNhiemVu: number;
  files: TepDinhKem[];
  /** §6.2 dong 2 - BE da tinh san quyen sua cho ban ghi nay. */
  choPhepSua: boolean;
  /** §6.2 dong 3 - chi nguoi tao VA chua co nhiem vu con. */
  choPhepXoa: boolean;
}

/** B3/B4 - body tao / sua van ban (`LuuVanBanRequest`). M04 co 8 truong. */
export interface LuuVanBanRequest {
  sokyhieu?: string | null;
  /** Bat buoc. */
  trichyeu: string;
  loaivb?: string | null;
  ngaybanhanh?: NgayIso | null;
  coquanbanhanh?: string | null;
  /** Bat buoc - TRONGTAM | THUONGXUYEN | DOTXUAT. */
  dokhan: string;
  linhvuc?: string | null;
  thoigianchidao?: MocIso | null;
  nguonnv?: string | null;
  nguoitheodoi?: string | null;
  fileIds?: Guid[] | null;
}
