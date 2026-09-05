using System.Text.Json.Serialization;

namespace QLNV.Core.Dtos;

/// <summary>
/// §5.3 C1 — body cua <c>POST /api/v1/van-ban/{idvb}/nhiem-vu</c>
/// (tao + giao nhieu nhiem vu trong 1 lan luu, man M05).
/// Server dat <c>trangthai = 3</c>, <c>trangthaiDvXuly = null</c> (§1.2 buoc 4, §10.4).
/// </summary>
public sealed class TaoNhiemVuRequest
{
    /// <summary>
    /// §1.2 buoc 4 — do trung noi dung fail-open: nguoi dung da xac nhan bo qua canh bao trung.
    /// </summary>
    [JsonPropertyName("boQuaTrungNoidung")]
    public bool BoQuaTrungNoiDung { get; set; }

    [JsonPropertyName("items")]
    public List<TaoNhiemVuItem> Items { get; set; } = new();
}

/// <summary>§5.3 C1 — mot dong nhiem vu trong luoi phan cong (M05).</summary>
public sealed class TaoNhiemVuItem
{
    /// <summary>BAT BUOC, &lt;= 2000 ky tu (§1.2 buoc 2).</summary>
    [JsonPropertyName("noidung")]
    public string NoiDung { get; set; } = string.Empty;

    /// <summary>DAU VAO AI (§9.4 S1).</summary>
    [JsonPropertyName("linhvuc")]
    public string? LinhVuc { get; set; }

    /// <summary>TRONGTAM / THUONGXUYEN / DOTXUAT — DAU VAO AI (§9.4 S4).</summary>
    [JsonPropertyName("dokhan")]
    public string DoKhan { get; set; } = string.Empty;

    /// <summary>Thoi han hoan thanh cua chu tri.</summary>
    [JsonPropertyName("hanxulyth")]
    public DateOnly? HanXuLyTh { get; set; }

    /// <summary>So ngay thuc hien — dung khi nguoi giao nhap so ngay thay vi chon ngay.</summary>
    [JsonPropertyName("songayhxlth")]
    public int? SoNgayHxlTh { get; set; }

    /// <summary>Thoi han phoi hop (§4.2).</summary>
    [JsonPropertyName("hanxulyph")]
    public DateOnly? HanXuLyPh { get; set; }

    /// <summary>BAT BUOC it nhat 1 nguoi (§1.2 buoc 3, §4.3).</summary>
    [JsonPropertyName("chuTri")]
    public List<Guid> ChuTri { get; set; } = new();

    /// <summary>Tuy chon, co the rong (§1.1).</summary>
    [JsonPropertyName("phoiHop")]
    public List<Guid> PhoiHop { get; set; } = new();

    /// <summary>
    /// §4.2 <c>ai_goiy_id</c> — id lan goi y AI da dung de chon nguoi nay.
    /// Null khi nguoi giao chon thu cong.
    /// </summary>
    [JsonPropertyName("aiGoiyId")]
    public Guid? AiGoiYId { get; set; }

    /// <summary>Id cac tep dinh kem rieng cua dong nhiem vu nay.</summary>
    [JsonPropertyName("fileIds")]
    public List<Guid>? FileIds { get; set; }
}

/// <summary>§5.3 C1 — ket qua tao nhieu nhiem vu.</summary>
public sealed class TaoNhiemVuResponse
{
    /// <summary>Id cac nhiem vu vua tao, cung thu tu voi <c>items</c> gui len.</summary>
    [JsonPropertyName("ids")]
    public List<Guid> Ids { get; set; } = new();

    [JsonPropertyName("soLuong")]
    public int SoLuong { get; set; }

    /// <summary>
    /// §1.2 buoc 4 — cac dong bi phat hien trung noi dung. Khi danh sach nay khong rong
    /// va <c>boQuaTrungNoidung = false</c> thi server KHONG luu, tra ve de FE hoi lai.
    /// </summary>
    [JsonPropertyName("trungNoiDung")]
    public List<TrungNoiDungDto> TrungNoiDung { get; set; } = new();
}

/// <summary>§5.3 C2 — body cua <c>POST /api/v1/nhiem-vu/kiem-tra-trung</c>. FAIL-OPEN.</summary>
public sealed class KiemTraTrungRequest
{
    [JsonPropertyName("items")]
    public List<KiemTraTrungItem> Items { get; set; } = new();
}

/// <summary>§5.3 C2 — mot dong can do trung.</summary>
public sealed class KiemTraTrungItem
{
    /// <summary>So thu tu dong trong luoi (de FE to do dung dong).</summary>
    [JsonPropertyName("tt")]
    public int Tt { get; set; }

    [JsonPropertyName("noiDung")]
    public string NoiDung { get; set; } = string.Empty;

    [JsonPropertyName("dsUserChuTri")]
    public List<Guid> DsUserChuTri { get; set; } = new();
}

/// <summary>§5.3 C2 — mot canh bao trung noi dung.</summary>
public sealed class TrungNoiDungDto
{
    [JsonPropertyName("tt")]
    public int Tt { get; set; }

    [JsonPropertyName("noiDung")]
    public string NoiDung { get; set; } = string.Empty;

    /// <summary>Id nhiem vu da ton tai bi coi la trung.</summary>
    [JsonPropertyName("idNhiemVuTrung")]
    public Guid? IdNhiemVuTrung { get; set; }

    [JsonPropertyName("noiDungTrung")]
    public string? NoiDungTrung { get; set; }

    [JsonPropertyName("thongBao")]
    public string ThongBao { get; set; } = string.Empty;
}

/// <summary>§5.3 C3 — tham so loc danh sach nhiem vu (M08).</summary>
public sealed class NhiemVuLocRequest
{
    /// <summary>TOI_GIAO | TOI_LAM (§5.3 C3). Null = ca hai.</summary>
    [JsonPropertyName("vaiTro")]
    public string? VaiTro { get; set; }

    /// <summary>Loc theo mot hoac nhieu ma truc A (§2.1).</summary>
    [JsonPropertyName("trangThai")]
    public List<int>? TrangThai { get; set; }

    /// <summary>Loc theo truc B (§2.2). Vi du 10 = chip "Cho xac nhan".</summary>
    [JsonPropertyName("trangThaiDvXuly")]
    public List<int>? TrangThaiDvXuly { get; set; }

    /// <summary>true = chi lay nhiem vu qua han; false = chi lay con han; null = khong loc.</summary>
    [JsonPropertyName("quaHan")]
    public bool? QuaHan { get; set; }

    /// <summary>true = chi lay nhiem vu sap het han (0..3 ngay).</summary>
    [JsonPropertyName("sapHetHan")]
    public bool? SapHetHan { get; set; }

    [JsonPropertyName("idvb")]
    public Guid? IdVb { get; set; }

    [JsonPropertyName("linhvuc")]
    public string? LinhVuc { get; set; }

    [JsonPropertyName("dokhan")]
    public string? DoKhan { get; set; }

    [JsonPropertyName("search")]
    public string? Search { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("size")]
    public int Size { get; set; } = Constants.GioiHan.KichThuocTrangMacDinh;
}

/// <summary>
/// §5.3 C3 — mot dong trong luoi "Nhiem vu cua toi" (M08).
/// Ten JSON bam §7.4: trangthai, trangthaiDvXuly, mucdoht, hanxulyth, noidung, dokhan, linhvuc.
/// </summary>
public sealed class NhiemVuDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("idvb")]
    public Guid IdVb { get; set; }

    [JsonPropertyName("soKyHieuVanBan")]
    public string? SoKyHieuVanBan { get; set; }

    [JsonPropertyName("trichYeuVanBan")]
    public string? TrichYeuVanBan { get; set; }

    [JsonPropertyName("noidung")]
    public string NoiDung { get; set; } = string.Empty;

    [JsonPropertyName("linhvuc")]
    public string? LinhVuc { get; set; }

    [JsonPropertyName("tenLinhVuc")]
    public string? TenLinhVuc { get; set; }

    [JsonPropertyName("dokhan")]
    public string DoKhan { get; set; } = string.Empty;

    [JsonPropertyName("hanxulyth")]
    public DateOnly? HanXuLyTh { get; set; }

    [JsonPropertyName("songayhxlth")]
    public int? SoNgayHxlTh { get; set; }

    [JsonPropertyName("hanxulyph")]
    public DateOnly? HanXuLyPh { get; set; }

    [JsonPropertyName("ngaygiao")]
    public DateTime NgayGiao { get; set; }

    [JsonPropertyName("ngaytiepnhan")]
    public DateTime? NgayTiepNhan { get; set; }

    [JsonPropertyName("ngayhoanthanhthucte")]
    public DateTime? NgayHoanThanhThucTe { get; set; }

    /// <summary>Truc A (§2.1).</summary>
    [JsonPropertyName("trangthai")]
    public int TrangThai { get; set; }

    [JsonPropertyName("tenTrangThai")]
    public string? TenTrangThai { get; set; }

    /// <summary>Truc B (§2.2), null = chua gui bao cao.</summary>
    [JsonPropertyName("trangthaiDvXuly")]
    public int? TrangThaiDvXuly { get; set; }

    [JsonPropertyName("tenTrangThaiDvXuly")]
    public string? TenTrangThaiDvXuly { get; set; }

    /// <summary>Truc C (§2.3).</summary>
    [JsonPropertyName("trangthaixulygiahan")]
    public int? TrangThaiXuLyGiaHan { get; set; }

    [JsonPropertyName("solangiahan")]
    public int SoLanGiaHan { get; set; }

    [JsonPropertyName("mucdoht")]
    public int? MucDoHt { get; set; }

    [JsonPropertyName("phanhoi")]
    public string? PhanHoi { get; set; }

    [JsonPropertyName("hsChatluong")]
    public int? HsChatLuong { get; set; }

    [JsonPropertyName("userIdGiaoViec")]
    public Guid UserIdGiaoViec { get; set; }

    [JsonPropertyName("nguoiGiaoTen")]
    public string? NguoiGiaoTen { get; set; }

    [JsonPropertyName("useridcreate")]
    public Guid UserIdCreate { get; set; }

    [JsonPropertyName("unitcode")]
    public string UnitCode { get; set; } = string.Empty;

    [JsonPropertyName("createdate")]
    public DateTime CreateDate { get; set; }

    [JsonPropertyName("updatedate")]
    public DateTime UpdateDate { get; set; }

    [JsonPropertyName("aiGoiyId")]
    public Guid? AiGoiYId { get; set; }

    /// <summary>§3.3 M08 — so ngay con lai toi han. Null khi khong co han. Am = qua han.</summary>
    [JsonPropertyName("soNgayConLai")]
    public int? SoNgayConLai { get; set; }

    /// <summary>§3.3 M08 — badge do "Het han".</summary>
    [JsonPropertyName("quaHan")]
    public bool QuaHan { get; set; }

    /// <summary>§3.3 M08 — badge vang "Sap het han" (0..3 ngay).</summary>
    [JsonPropertyName("sapHetHan")]
    public bool SapHetHan { get; set; }

    /// <summary>§2.6 — HOAN_THANH_NGHIEM_THU | DA_THU_HOI | null.</summary>
    [JsonPropertyName("diemCuoi")]
    public string? DiemCuoi { get; set; }

    /// <summary>Danh sach chu tri con hieu luc.</summary>
    [JsonPropertyName("chuTri")]
    public List<NguoiDungTomTatDto> ChuTri { get; set; } = new();

    /// <summary>Danh sach phoi hop con hieu luc.</summary>
    [JsonPropertyName("phoiHop")]
    public List<NguoiDungTomTatDto> PhoiHop { get; set; } = new();

    /// <summary>§6.2 — 15 co quyen cua nguoi dang dang nhap tren nhiem vu nay.</summary>
    [JsonPropertyName("quyen")]
    public QuyenNhiemVuDto? Quyen { get; set; }
}

/// <summary>
/// §5.3 C4 — chi tiet nhiem vu: <see cref="NhiemVuDto"/> + phan cong + lich su + tep.
/// </summary>
public sealed class NhiemVuChiTietDto
{
    [JsonPropertyName("nhiemVu")]
    public NhiemVuDto NhiemVu { get; set; } = new();

    [JsonPropertyName("phanCong")]
    public List<PhanCongDto> PhanCong { get; set; } = new();

    [JsonPropertyName("lichSuXuLy")]
    public List<XuLyDto> LichSuXuLy { get; set; } = new();

    [JsonPropertyName("lichSuGiaHan")]
    public List<GiaHanDto> LichSuGiaHan { get; set; } = new();

    [JsonPropertyName("files")]
    public List<FileDto> Files { get; set; } = new();

    /// <summary>§5.4 D5 — danh sach trang thai duoc chon khi bao cao, da loc theo han.</summary>
    [JsonPropertyName("trangThaiHopLe")]
    public List<TuDienDto> TrangThaiHopLe { get; set; } = new();
}

/// <summary>§4.3 — mot ban ghi phan cong tra ve cho FE.</summary>
public sealed class PhanCongDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("userid")]
    public Guid UserId { get; set; }

    [JsonPropertyName("fullname")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("chucvu")]
    public string? ChucVu { get; set; }

    [JsonPropertyName("unitcode")]
    public string UnitCode { get; set; } = string.Empty;

    [JsonPropertyName("unitname")]
    public string? UnitName { get; set; }

    /// <summary>CHUTRI | PHOIHOP (§4.3).</summary>
    [JsonPropertyName("vaitro")]
    public string VaiTro { get; set; } = string.Empty;

    /// <summary>1 = con hieu luc, 0 = da thu hoi phan cong.</summary>
    [JsonPropertyName("trangthai")]
    public int TrangThai { get; set; }

    [JsonPropertyName("createdate")]
    public DateTime CreateDate { get; set; }
}

/// <summary>§5.3 C5 — body sua nhiem vu (chi khi <c>trangthai = 3</c>, §6.2 dong 6).</summary>
public sealed class SuaNhiemVuRequest
{
    [JsonPropertyName("noidung")]
    public string NoiDung { get; set; } = string.Empty;

    [JsonPropertyName("linhvuc")]
    public string? LinhVuc { get; set; }

    [JsonPropertyName("dokhan")]
    public string DoKhan { get; set; } = string.Empty;

    [JsonPropertyName("hanxulyth")]
    public DateOnly? HanXuLyTh { get; set; }

    [JsonPropertyName("songayhxlth")]
    public int? SoNgayHxlTh { get; set; }

    [JsonPropertyName("hanxulyph")]
    public DateOnly? HanXuLyPh { get; set; }

    [JsonPropertyName("fileIds")]
    public List<Guid>? FileIds { get; set; }
}

/// <summary>§5.3 C6 — body <c>POST /api/v1/nhiem-vu/{id}/thu-hoi</c> (T14).</summary>
public sealed class ThuHoiNhiemVuRequest
{
    /// <summary>Ly do thu hoi. Khong bat buoc, &lt;= 2000 ky tu.</summary>
    [JsonPropertyName("lyDo")]
    public string? LyDo { get; set; }
}

/// <summary>§5.3 C7 — body <c>POST /api/v1/nhiem-vu/{id}/thu-hoi-phan-cong</c>.</summary>
public sealed class ThuHoiPhanCongRequest
{
    [JsonPropertyName("userIds")]
    public List<Guid> UserIds { get; set; } = new();

    [JsonPropertyName("lyDo")]
    public string? LyDo { get; set; }
}

/// <summary>§1.3 — body nhac viec. Khong doi trang thai, khong dinh kem tep.</summary>
public sealed class NhacViecRequest
{
    /// <summary>BAT BUOC, &lt;= 2000 ky tu.</summary>
    [JsonPropertyName("noidung")]
    public string NoiDung { get; set; } = string.Empty;
}

/// <summary>
/// §6.2 — 15 co quyen tinh tu trang thai + bang phan cong.
/// FE dung DUNG cac co nay de an/hien nut (§6.4), khong tu tinh lai.
/// </summary>
public sealed class QuyenNhiemVuDto
{
    [JsonPropertyName("suaNhiemVu")]
    public bool SuaNhiemVu { get; set; }

    [JsonPropertyName("thuHoiNhiemVu")]
    public bool ThuHoiNhiemVu { get; set; }

    [JsonPropertyName("thuHoiPhanCong")]
    public bool ThuHoiPhanCong { get; set; }

    [JsonPropertyName("tiepNhan")]
    public bool TiepNhan { get; set; }

    [JsonPropertyName("tuChoi")]
    public bool TuChoi { get; set; }

    [JsonPropertyName("capNhatTienDo")]
    public bool CapNhatTienDo { get; set; }

    [JsonPropertyName("guiBaoCao")]
    public bool GuiBaoCao { get; set; }

    [JsonPropertyName("thuHoiBaoCao")]
    public bool ThuHoiBaoCao { get; set; }

    [JsonPropertyName("kiemTraKetQua")]
    public bool KiemTraKetQua { get; set; }

    [JsonPropertyName("xinGiaHan")]
    public bool XinGiaHan { get; set; }

    [JsonPropertyName("duyetGiaHan")]
    public bool DuyetGiaHan { get; set; }

    [JsonPropertyName("nhacViec")]
    public bool NhacViec { get; set; }

    [JsonPropertyName("xemChiTiet")]
    public bool XemChiTiet { get; set; }

    [JsonPropertyName("taiTep")]
    public bool TaiTep { get; set; }

    /// <summary>MO RONG §2.4 T4/T5 — §6.2 khong co dong rieng cho hanh dong nay.</summary>
    [JsonPropertyName("xuLyTuChoi")]
    public bool XuLyTuChoi { get; set; }
}
