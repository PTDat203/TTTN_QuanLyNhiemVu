/* =====================================================================
   Tien ich ngay thang - port tu `NgayUtil` cua QLNV.Core.
   Moi phep so hang deu lam viec tren NGAY (bo phan gio) de khop voi
   kieu `DateOnly` cua backend, tranh lech mui gio.
   ===================================================================== */

/** Chuyen chuoi ISO ("2026-10-15" hoac "2026-10-15T08:00:00") sang Date luc 00:00 gio dia phuong. */
export function docNgay(gt: string | Date | null | undefined): Date | null {
  if (!gt) return null;
  if (gt instanceof Date) return new Date(gt.getFullYear(), gt.getMonth(), gt.getDate());

  const chuoi = gt.trim();
  if (!chuoi) return null;

  // Dang "yyyy-MM-dd" hoac "yyyy-MM-ddTHH:mm:ss" - cat lay 10 ky tu dau.
  const phan = chuoi.substring(0, 10).split('-');
  if (phan.length === 3) {
    const nam = Number(phan[0]);
    const thang = Number(phan[1]);
    const ngay = Number(phan[2]);
    if (Number.isFinite(nam) && Number.isFinite(thang) && Number.isFinite(ngay)) {
      return new Date(nam, thang - 1, ngay);
    }
  }

  const d = new Date(chuoi);
  return Number.isNaN(d.getTime()) ? null : new Date(d.getFullYear(), d.getMonth(), d.getDate());
}

/** Hom nay luc 00:00 gio dia phuong. */
export function homNay(): Date {
  const d = new Date();
  return new Date(d.getFullYear(), d.getMonth(), d.getDate());
}

/** Chuyen Date sang chuoi "yyyy-MM-dd" de gui len BE (kieu DateOnly). */
export function sangChuoiNgay(d: Date | null | undefined): string | null {
  if (!d) return null;
  const nam = d.getFullYear();
  const thang = `${d.getMonth() + 1}`.padStart(2, '0');
  const ngay = `${d.getDate()}`.padStart(2, '0');
  return `${nam}-${thang}-${ngay}`;
}

/**
 * So ngay con lai den han. Am = da qua han.
 * Tra `null` khi khong co han (§2.5 - nhiem vu khong dat han thi khong bao gio qua han).
 */
export function soNgayConLai(han: string | Date | null | undefined, moc?: Date): number | null {
  const h = docNgay(han);
  if (!h) return null;
  const goc = moc ?? homNay();
  const mili = h.getTime() - goc.getTime();
  return Math.round(mili / 86400000);
}

/** Qua han khi so ngay con lai < 0. Khong co han => khong qua han. */
export function quaHan(han: string | Date | null | undefined, moc?: Date): boolean {
  const d = soNgayConLai(han, moc);
  return d !== null && d < 0;
}

/** Sap het han: con 0..nguong ngay (mac dinh 3 - §3.3 M08 / §2.5). */
export function sapHetHan(han: string | Date | null | undefined, nguong = 3, moc?: Date): boolean {
  const d = soNgayConLai(han, moc);
  return d !== null && d >= 0 && d <= nguong;
}

/** Dinh dang ngay "dd/MM/yyyy". Chuoi rong khi khong co gia tri. */
export function dinhDang(gt: string | Date | null | undefined): string {
  const d = docNgay(gt);
  if (!d) return '';
  const ngay = `${d.getDate()}`.padStart(2, '0');
  const thang = `${d.getMonth() + 1}`.padStart(2, '0');
  return `${ngay}/${thang}/${d.getFullYear()}`;
}

/** Dinh dang moc thoi gian "dd/MM/yyyy HH:mm". */
export function dinhDangMoc(gt: string | Date | null | undefined): string {
  if (!gt) return '';
  const d = gt instanceof Date ? gt : new Date(gt);
  if (Number.isNaN(d.getTime())) return '';
  const ngay = `${d.getDate()}`.padStart(2, '0');
  const thang = `${d.getMonth() + 1}`.padStart(2, '0');
  const gio = `${d.getHours()}`.padStart(2, '0');
  const phut = `${d.getMinutes()}`.padStart(2, '0');
  return `${ngay}/${thang}/${d.getFullYear()} ${gio}:${phut}`;
}

/** Cong them so ngay vao mot ngay. */
export function congNgay(goc: Date, soNgay: number): Date {
  const d = new Date(goc.getTime());
  d.setDate(d.getDate() + soNgay);
  return new Date(d.getFullYear(), d.getMonth(), d.getDate());
}

/**
 * Cau chu cho badge han (§3.3 M08).
 * Vi du: "Hết hạn 3 ngày" / "Còn 2 ngày" / "Đến hạn hôm nay".
 */
export function moTaHan(han: string | Date | null | undefined, moc?: Date): string {
  const d = soNgayConLai(han, moc);
  if (d === null) return 'Không đặt hạn';
  if (d < 0) return `Hết hạn ${Math.abs(d)} ngày`;
  if (d === 0) return 'Đến hạn hôm nay';
  return `Còn ${d} ngày`;
}
