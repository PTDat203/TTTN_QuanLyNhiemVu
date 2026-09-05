namespace QLNV.Core.Entities;

/// <summary>
/// §4.2 — bang <c>DM_NHIEMVU_CHITIET</c> (Nhiem vu).
/// Goc: <c>DmNhiemvuChitietV2Model</c>, 91 truong rut con 26.
///
/// §7.4 muc 1 — ten COT giu nguyen chu thuong nhu he goc:
/// idvb, noidung, linhvuc, dokhan, hanxulyth, songayhxlth, hanxulyph, ngaygiao,
/// ngaytiepnhan, ngayhoanthanhthucte, trangthai, trangthaiDvXuly, trangthaixulygiahan,
/// solangiahan, mucdoht, phanhoi, hsChatluong, userIdGiaoViec, useridcreate, unitcode,
/// createdate, updatedate, ai_goiy_id.
///
/// LUOC BO (§4.2): idnvgroup, parentIdCt, idnvchutri, idnvchitietgoc, nhiemvugiao,
/// loainv, gioxuly, thuxuly, ngayketthucdk, isketthucdk, domat, isencrypt, password,
/// captrinh, loaiSpcvId, hsTiendo, trangthaigiahan, tonghopTrangthai va toan bo 8 co
/// do BE tinh (isxuly, ischuyentiep, isthuhoiphancong, istuchoi, isview, isnhomnv,
/// isphoihop, istrinhdexuat) — §6.4: app moi tinh quyen truc tiep tu trang thai +
/// bang phan cong.
/// </summary>
public class DmNhiemVuChiTiet
{
    /// <summary>Khoa chinh. Cot <c>id</c>.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// FK toi <see cref="DmVanBan"/>. BAT BUOC. Cot <c>idvb</c>.
    /// Goc dung ca <c>idnv</c> lan <c>idvb</c>; app moi CHI dung 1 truong (§4.2).
    /// </summary>
    public Guid IdVb { get; set; }

    /// <summary>Noi dung nhiem vu. BAT BUOC, &lt;= 2000 ky tu. Cot <c>noidung</c>.</summary>
    public string NoiDung { get; set; } = string.Empty;

    /// <summary>
    /// Linh vuc — FK logic toi DM_LINHVUC. Cot <c>linhvuc</c>, varchar(50).
    /// DAU VAO AI (§9.4 S1). Co the null khi nhiem vu chua gan linh vuc (§9.5).
    /// </summary>
    public string? LinhVuc { get; set; }

    /// <summary>
    /// Muc do uu tien. BAT BUOC. Cot <c>dokhan</c>, varchar(50).
    /// DAU VAO AI — trong so tai (§9.4 S4).
    /// </summary>
    public string DoKhan { get; set; } = Constants.DoKhan.ThuongXuyen;

    /// <summary>Thoi han hoan thanh cua chu tri. Cot <c>hanxulyth</c>, date.</summary>
    public DateOnly? HanXuLyTh { get; set; }

    /// <summary>So ngay thuc hien. Cot <c>songayhxlth</c>, int.</summary>
    public int? SoNgayHxlTh { get; set; }

    /// <summary>Thoi han phoi hop. Cot <c>hanxulyph</c>, date.</summary>
    public DateOnly? HanXuLyPh { get; set; }

    /// <summary>Thoi diem giao. BAT BUOC. Cot <c>ngaygiao</c>, datetime.</summary>
    public DateTime NgayGiao { get; set; }

    /// <summary>
    /// Thoi diem tiep nhan. Cot <c>ngaytiepnhan</c>, datetime.
    /// MOI — he goc khong co buoc Tiep nhan (§1.1, §10.3). Ghi khi thuc hien §2.4 T2.
    /// </summary>
    public DateTime? NgayTiepNhan { get; set; }

    /// <summary>Thoi diem hoan thanh thuc te (ghi khi nghiem thu DAT). Cot <c>ngayhoanthanhthucte</c>.</summary>
    public DateTime? NgayHoanThanhThucTe { get; set; }

    /// <summary>
    /// TRUC A — trang thai nhiem vu (§2.1). BAT BUOC.
    /// Cot <c>trangthai</c>. MAC DINH = 3 (Chua trien khai), do server dat (§1.2 buoc 4, §10.4).
    /// </summary>
    public int TrangThai { get; set; } = Constants.TrangThaiNv.ChuaTrienKhai;

    /// <summary>
    /// TRUC B — trang thai kiem tra ket qua (§2.2).
    /// Cot <c>trangthaiDvXuly</c>. MAC DINH = null (chua gui bao cao).
    /// </summary>
    public int? TrangThaiDvXuly { get; set; }

    /// <summary>TRUC C — trang thai duyet gia han (§2.3). Cot <c>trangthaixulygiahan</c>.</summary>
    public int? TrangThaiXuLyGiaHan { get; set; }

    /// <summary>So lan da gia han. BAT BUOC, mac dinh 0, toi da 2 (§2.3). Cot <c>solangiahan</c>.</summary>
    public int SoLanGiaHan { get; set; }

    /// <summary>
    /// Phan tram hoan thanh, 0-100 (§1.2 buoc 4 — goc KHONG validate, §10.2).
    /// Cot <c>mucdoht</c>, int, co the null khi chua cap nhat lan nao.
    /// </summary>
    public int? MucDoHt { get; set; }

    /// <summary>Noi dung phan hoi khi kiem tra ket qua. Cot <c>phanhoi</c>, varchar(2000).</summary>
    public string? PhanHoi { get; set; }

    /// <summary>
    /// Diem chat luong 1-6 khi nghiem thu. Cot <c>hsChatluong</c>, int.
    /// §9.4 ghi chu: KHONG dua vao cong thuc cham diem AI (da bo, vi da so ban ghi rong).
    /// </summary>
    public int? HsChatLuong { get; set; }

    /// <summary>
    /// Nguoi giao viec. BAT BUOC. Cot <c>userIdGiaoViec</c>.
    /// La can cu quyen o §6.2 dong 6, 7, 8, 14, 16, 17.
    /// </summary>
    public Guid UserIdGiaoViec { get; set; }

    /// <summary>Nguoi tao ban ghi. BAT BUOC. Cot <c>useridcreate</c>.</summary>
    public Guid UserIdCreate { get; set; }

    /// <summary>Don vi so huu ban ghi. BAT BUOC. Cot <c>unitcode</c>, varchar(50).</summary>
    public string UnitCode { get; set; } = string.Empty;

    /// <summary>Thoi diem tao. BAT BUOC. Cot <c>createdate</c>.</summary>
    public DateTime CreateDate { get; set; }

    /// <summary>Thoi diem cap nhat. BAT BUOC. Cot <c>updatedate</c>.</summary>
    public DateTime UpdateDate { get; set; }

    /// <summary>
    /// FK toi <see cref="AiGoiYLog"/> — nguoi thuc hien nay co phai do AI goi y khong (§4.2).
    /// Cot <c>ai_goiy_id</c>.
    /// </summary>
    public Guid? AiGoiYId { get; set; }

    // --- Dieu huong (§4.9) ---

    /// <summary>Van ban chi dao chua nhiem vu nay.</summary>
    public DmVanBan? VanBan { get; set; }

    /// <summary>Danh sach phan cong (chu tri / phoi hop).</summary>
    public ICollection<NhiemVuPhanCong> PhanCong { get; set; } = new List<NhiemVuPhanCong>();

    /// <summary>Lich su xu ly / bao cao (§4.4).</summary>
    public ICollection<XuLyNhiemVu> LichSuXuLy { get; set; } = new List<XuLyNhiemVu>();

    /// <summary>Lich su gia han (§4.5), toi da 2 lan duoc duyet.</summary>
    public ICollection<GiaHanNhiemVu> LichSuGiaHan { get; set; } = new List<GiaHanNhiemVu>();
}
