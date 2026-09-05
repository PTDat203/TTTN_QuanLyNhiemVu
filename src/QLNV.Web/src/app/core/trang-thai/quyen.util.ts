import { NguoiDung, laBenGiao, laBenLam, VaiTro } from '../models/auth.model';
import { VaiTroPhanCong } from '../models/danh-muc.model';
import { NhiemVu, PhanCong, QuyenNhiemVu } from '../models/nhiem-vu.model';
import { TrangThaiNv, TrangThaiPh, TrangThaiGiaHan } from './trang-thai.const';

/* =====================================================================
   §6.2 - BANG VAI TRO x HANH DONG, CAI LAI O FRONTEND.

   §6.4 ghi ro: FE an/hien nut bang DUNG bieu thuc cua §6.2, tinh tu
   `trangthai`, `trangthaiDvXuly`, `trangthaixulygiahan` va danh sach
   phan cong. TUYET DOI KHONG dung co do BE tinh san kieu `isxuly` /
   `istuchoi` - o he goc quy tac tinh nhung co do KHONG ton tai trong FE
   nen khong kiem chung duoc.
   `nv.quyen` do BE tra ve chi dung DOI CHIEU khi go loi.

   Ban nay port nguyen van tu `flow.js` (da kiem chung doi khang voi dac ta).
   Cac cho LECH so voi §6.2 deu co ghi chu ly do ngay tai dong.
   ===================================================================== */

/** Cau hinh nghiep vu anh huong toi quyen (§2.3). */
export interface CauHinhQuyen {
  /** §2.3 - toi da 2 lan gia han. */
  soLanGiaHanToiDa: number;
  /** QUAN_TRI bo qua dieu kien so huu du lieu, nhung VAN phai dung trang thai. */
  quanTriToanQuyen: boolean;
}

export const CAU_HINH_QUYEN_MAC_DINH: CauHinhQuyen = {
  soLanGiaHanToiDa: 2,
  quanTriToanQuyen: true
};

/** Vai tro cua nguoi dang thao tac TREN MOT nhiem vu cu the. */
export interface VaiTroNhiemVu {
  laNguoiGiao: boolean;
  laNguoiTao: boolean;
  laChuTri: boolean;
  laPhoiHop: boolean;
  laQuanTri: boolean;
  /** Co bat ky lien he nao voi nhiem vu khong (§6.2 dong 18). */
  coLienQuan: boolean;
}

export const VAI_TRO_KHONG_CO: VaiTroNhiemVu = {
  laNguoiGiao: false,
  laNguoiTao: false,
  laChuTri: false,
  laPhoiHop: false,
  laQuanTri: false,
  coLienQuan: false
};

/** 15 co deu tat - dung khi khong co nhiem vu hoac chua dang nhap. */
export const QUYEN_KHONG_CO: QuyenNhiemVu = {
  suaNhiemVu: false,
  thuHoiNhiemVu: false,
  thuHoiPhanCong: false,
  tiepNhan: false,
  tuChoi: false,
  capNhatTienDo: false,
  guiBaoCao: false,
  thuHoiBaoCao: false,
  kiemTraKetQua: false,
  xinGiaHan: false,
  duyetGiaHan: false,
  nhacViec: false,
  xemChiTiet: false,
  taiTep: false,
  xuLyTuChoi: false
};

/**
 * Xac dinh vai tro cua nguoi dung tren mot nhiem vu.
 *
 * Nguon phan cong theo thu tu uu tien:
 *  1. `dsPhanCong` (tu C4 - day du, ke ca ban ghi da thu hoi) neu duoc truyen;
 *  2. `nv.chuTri` / `nv.phoiHop` (BE da loc san `trangthai = 1`).
 * Tai khoan bi khoa (`trangthai = 0`) khong co vai tro nao.
 */
export function tinhVaiTroTrenNhiemVu(
  nv: NhiemVu | null | undefined,
  nguoiDung: NguoiDung | null | undefined,
  dsPhanCong?: PhanCong[] | null
): VaiTroNhiemVu {
  if (!nguoiDung || nguoiDung.trangthai === 0) return VAI_TRO_KHONG_CO;

  const laQuanTri = nguoiDung.vaitro === VaiTro.QuanTri;
  if (!nv) {
    return { ...VAI_TRO_KHONG_CO, laQuanTri, coLienQuan: laQuanTri };
  }

  const toi = nguoiDung.id;
  const laNguoiGiao = !!nv.userIdGiaoViec && nv.userIdGiaoViec === toi;
  const laNguoiTao = !!nv.useridcreate && nv.useridcreate === toi;

  let laChuTri = false;
  let laPhoiHop = false;

  if (dsPhanCong && dsPhanCong.length > 0) {
    for (const pc of dsPhanCong) {
      // Chi tinh ban ghi CON HIEU LUC (trangthai = 1); ban ghi da thu hoi = 0.
      if (pc.trangthai !== 1 || pc.userid !== toi) continue;
      if (pc.vaitro === VaiTroPhanCong.ChuTri) laChuTri = true;
      else if (pc.vaitro === VaiTroPhanCong.PhoiHop) laPhoiHop = true;
    }
  } else {
    laChuTri = (nv.chuTri ?? []).some((x) => x.userid === toi);
    laPhoiHop = (nv.phoiHop ?? []).some((x) => x.userid === toi);
  }

  return {
    laNguoiGiao,
    laNguoiTao,
    laChuTri,
    laPhoiHop,
    laQuanTri,
    coLienQuan: laNguoiGiao || laNguoiTao || laChuTri || laPhoiHop || laQuanTri
  };
}

/**
 * §6.2 dong 6..19 (+ mo rong T4/T5). Tra ve 15 co de an/hien nut.
 *
 * @param nv         Nhiem vu dang xet. `null` => tat het.
 * @param nguoiDung  Nguoi dang dang nhap.
 * @param dsPhanCong Danh sach phan cong day du (tuy chon, tu C4).
 * @param cauHinh    Cau hinh nghiep vu (so lan gia han toi da...).
 */
export function tinhQuyen(
  nv: NhiemVu | null | undefined,
  nguoiDung: NguoiDung | null | undefined,
  dsPhanCong?: PhanCong[] | null,
  cauHinh: CauHinhQuyen = CAU_HINH_QUYEN_MAC_DINH
): QuyenNhiemVu {
  const q: QuyenNhiemVu = { ...QUYEN_KHONG_CO };
  if (!nguoiDung || nguoiDung.trangthai === 0) return q;

  const v = tinhVaiTroTrenNhiemVu(nv, nguoiDung, dsPhanCong);

  const tt = nv ? soHoacNull(nv.trangthai) : null; // truc A
  const dv = nv ? soHoacNull(nv.trangthaiDvXuly) : null; // truc B
  const gh = nv ? soHoacNull(nv.trangthaixulygiahan) : null; // truc C
  const slgh = nv ? soHoacNull(nv.solangiahan) ?? 0 : 0;

  // §6.4 LOP 1 - `vaitro` co duoc phep goi endpoint hay khong (cac o KHONG cua §6.2).
  // §6.2 dong 6,7,8,14,16,17: cot QUAN_TRI / NGUOI_GIAO co dau.
  const benGiao = laBenGiao(nguoiDung.vaitro);
  // §6.2 dong 9,10,11,12,13,15: CHI cot NGUOI_THUC_HIEN co dau.
  const benLam = laBenLam(nguoiDung.vaitro);

  // QUAN_TRI bo qua dieu kien SO HUU du lieu nhung VAN phai dung trang thai
  // (hanh dong sai trang thai la vo nghia).
  const qt = cauHinh.quanTriToanQuyen && v.laQuanTri;

  // Nguoi giao "hieu luc": userIdGiaoViec = toi, HOAC toi la nguoi tao khi nhiem vu
  // chua chi dinh can bo giao viec (bam `coTheXacNhan` cua he goc - §6.2 dong 14).
  const nguoiGiaoHoacTao = v.laNguoiGiao || (!nv || !nv.userIdGiaoViec ? v.laNguoiTao : false);

  // §6.2 dong 6 - Sua nhiem vu da giao: userIdGiaoViec = toi VA trangthai = 3.
  q.suaNhiemVu = benGiao && (v.laNguoiGiao || qt) && tt === TrangThaiNv.ChuaTrienKhai;

  // §6.2 dong 7 - Thu hoi nhiem vu (ve 97): trangthai khong thuoc {1,5,97}.
  q.thuHoiNhiemVu = benGiao && (v.laNguoiGiao || qt) && tt !== null && !trong([1, 5, 97], tt);

  // §6.2 dong 8 - Thu hoi phan cong (rut 1 nguoi): trangthai khong thuoc {1,5}.
  q.thuHoiPhanCong = benGiao && (v.laNguoiGiao || qt) && tt !== null && !trong([1, 5], tt);

  // §6.2 dong 9 - Tiep nhan: toi la CHUTRI VA trangthai = 3.
  q.tiepNhan = benLam && v.laChuTri && tt === TrangThaiNv.ChuaTrienKhai;

  // §6.2 dong 10 - Tu choi: CHUTRI VA trangthai thuoc {2,3} VA solangiahan = 0.
  // XUNG DOT DAC TA: §6.2 dong 10 khong neu truc B, nhung §2.4 T3 chi cho tu
  // (3, null) hoac (2, null). Chon theo §2.4 (may trang thai la nguon chuan)
  // => them ve `dv === null`, chan vong lap tu choi vo han:
  // tu choi -> (6,10) -> bac bo -> (2,12) -> tu choi lai...
  q.tuChoi = benLam && v.laChuTri && trong([2, 3], tt) && dv === null && slgh === 0;

  // §6.2 dong 11 - Cap nhat tien do: CHUTRI VA trangthai thuoc {2,3,7}
  //                VA trangthaiDvXuly thuoc {null, 12}.
  q.capNhatTienDo = benLam && v.laChuTri && trong([2, 3, 7], tt) && (dv === null || dv === TrangThaiPh.TuChoi);

  // §6.2 dong 12 - Gui bao cao ket qua: dieu kien nhu dong 11.
  q.guiBaoCao = q.capNhatTienDo;

  // §6.2 dong 13 - Thu hoi bao cao: CHUTRI VA trangthai thuoc {1,5} VA truc B = 10.
  q.thuHoiBaoCao = benLam && v.laChuTri && trong([1, 5], tt) && dv === TrangThaiPh.ChoXacNhan;

  // §6.2 dong 14 - Kiem tra ket qua: truc B = 10 VA (userIdGiaoViec = toi HOAC nguoi tao).
  // BO SUNG so voi §6.2: loai truc A = 6 (cap (6,10) thuoc §2.4 T4/T5 "Xu ly tu choi",
  // khong phai T9/T10; de chung se sinh cap chet (6,11)/(6,12) ngoai §2.4)
  // va loai truc A = 97 (diem cuoi §2.6).
  q.kiemTraKetQua =
    benGiao &&
    dv === TrangThaiPh.ChoXacNhan &&
    tt !== TrangThaiNv.TuChoi &&
    tt !== TrangThaiNv.DaThuHoi &&
    (nguoiGiaoHoacTao || qt);

  // §6.2 dong 15 - Xin gia han: CHUTRI VA trangthai thuoc {2,3,7}
  //                VA trangthaixulygiahan khac 10 VA solangiahan < 2.
  q.xinGiaHan =
    benLam &&
    v.laChuTri &&
    trong([2, 3, 7], tt) &&
    gh !== TrangThaiGiaHan.ChoDuyet &&
    slgh < cauHinh.soLanGiaHanToiDa;

  // §6.2 dong 16 - Duyet / tu choi gia han: truc C = 10 VA userIdGiaoViec = toi.
  // BO SUNG: chan khi trangthai = 97 - §2.6 coi 97 la DIEM CUOI, khong duoc
  // "hoi sinh" nhiem vu da thu hoi bang cach duyet gia han.
  q.duyetGiaHan =
    benGiao && gh === TrangThaiGiaHan.ChoDuyet && tt !== TrangThaiNv.DaThuHoi && (v.laNguoiGiao || qt);

  // §6.2 dong 17 - Nhac viec: trangthai khong thuoc {1,5,97}.
  q.nhacViec = benGiao && (v.laNguoiGiao || qt) && tt !== null && !trong([1, 5, 97], tt);

  // §6.2 dong 18 - Xem chi tiet + lich su: nguoi giao, hoac co ten trong
  // NHIEMVU_PHANCONG (ca CHUTRI lan PHOIHOP). Bo sung nguoi tao ban ghi.
  q.xemChiTiet = !!nv && v.coLienQuan;

  // §6.2 dong 19 - Tai tep dinh kem: nhu dong 18.
  q.taiTep = q.xemChiTiet;

  // MO RONG §2.4 T4/T5 - §6.2 KHONG co dong cho "Xu ly de nghi tu choi".
  // Nguoi giao (hoac quan tri) xu ly khi nhiem vu o cap (6, 10);
  // thieu no thi cap (6,10) khong co loi ra.
  q.xuLyTuChoi =
    benGiao && (nguoiGiaoHoacTao || qt) && tt === TrangThaiNv.TuChoi && dv === TrangThaiPh.ChoXacNhan;

  return q;
}

/* ------------------------------------------------------------------ */
/* Quyen KHONG gan voi mot nhiem vu cu the (§6.2 dong 1..5, 20..24)     */
/* ------------------------------------------------------------------ */

/** §6.2 dong 2 - tao / sua van ban chi dao. */
export function coTheTaoVanBan(nguoiDung: NguoiDung | null | undefined): boolean {
  return !!nguoiDung && nguoiDung.trangthai !== 0 && laBenGiao(nguoiDung.vaitro);
}

/** §6.2 dong 4 - tao & giao nhiem vu. */
export function coTheGiaoNhiemVu(nguoiDung: NguoiDung | null | undefined): boolean {
  return coTheTaoVanBan(nguoiDung);
}

/** §6.2 dong 5 - dung AI goi y nguoi thuc hien (chi trong man phan cong). */
export function coTheDungAiGoiY(nguoiDung: NguoiDung | null | undefined): boolean {
  return coTheTaoVanBan(nguoiDung);
}

/** §6.2 dong 21 / 23 / 24 - quan ly nguoi dung, danh muc, nhat ky AI. */
export function laQuanTri(nguoiDung: NguoiDung | null | undefined): boolean {
  return !!nguoiDung && nguoiDung.trangthai !== 0 && nguoiDung.vaitro === VaiTro.QuanTri;
}

/* ------------------------------------------------------------------ */
/* Tien ich noi bo                                                     */
/* ------------------------------------------------------------------ */

function trong(ds: readonly number[], gt: number | null): boolean {
  return gt !== null && ds.indexOf(gt) >= 0;
}

function soHoacNull(gt: number | null | undefined): number | null {
  if (gt === null || gt === undefined) return null;
  const n = Number(gt);
  return Number.isFinite(n) ? n : null;
}
