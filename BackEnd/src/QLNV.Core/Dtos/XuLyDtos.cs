using System.Text.Json.Serialization;

namespace QLNV.Core.Dtos;

/// <summary>
/// §5.4 D2 — body <c>POST /api/v1/nhiem-vu/{id}/tu-choi</c> (§2.4 T3).
/// §10.7: he goc KHONG bat buoc ly do; app moi BAT BUOC — day la yeu cau MOI.
/// </summary>
public sealed class TuChoiRequest
{
    /// <summary>BAT BUOC, &lt;= 2000 ky tu.</summary>
    [JsonPropertyName("lyDo")]
    public string LyDo { get; set; } = string.Empty;

    [JsonPropertyName("fileIds")]
    public List<Guid>? FileIds { get; set; }
}

/// <summary>
/// §5.4 D3 — body <c>POST /api/v1/nhiem-vu/{id}/tien-do</c> (§2.4 T6).
/// KHONG doi trang thai (ca truc A lan truc B).
/// </summary>
public sealed class TienDoRequest
{
    /// <summary>BAT BUOC, so nguyen 0-100 (§10.2: goc khong validate — day la yeu cau MOI).</summary>
    [JsonPropertyName("mucdoht")]
    public int MucDoHt { get; set; }

    /// <summary>Noi dung cong viec da lam. Khong bat buoc, &lt;= 2000 ky tu.</summary>
    [JsonPropertyName("noidung")]
    public string? NoiDung { get; set; }

    [JsonPropertyName("fileIds")]
    public List<Guid>? FileIds { get; set; }
}

/// <summary>
/// §5.4 D4 — body <c>POST /api/v1/nhiem-vu/{id}/bao-cao</c> (§2.4 T7).
/// Server VALIDATE LAI <see cref="TrangThai"/> theo bo loc han cua §5.4 D5 (§6.4).
/// Ket qua: <c>trangthai := TrangThai</c>, <c>trangthaiDvXuly := 10</c>.
/// </summary>
public sealed class BaoCaoRequest
{
    /// <summary>Ma truc A do nguoi dung chon, phai nam trong danh sach da loc theo han.</summary>
    [JsonPropertyName("trangthai")]
    public int TrangThai { get; set; }

    /// <summary>BAT BUOC khi <see cref="TrangThai"/> thuoc {1, 5} (§1.2 buoc 5). &lt;= 2000 ky tu.</summary>
    [JsonPropertyName("noidung")]
    public string? NoiDung { get; set; }

    /// <summary>Tuy chon; neu co thi phai la so nguyen 0-100.</summary>
    [JsonPropertyName("mucdoht")]
    public int? MucDoHt { get; set; }

    [JsonPropertyName("fileIds")]
    public List<Guid>? FileIds { get; set; }
}

/// <summary>§5.4 D6 — body <c>POST /api/v1/nhiem-vu/{id}/thu-hoi-bao-cao</c> (§2.4 T8).</summary>
public sealed class ThuHoiBaoCaoRequest
{
    /// <summary>Ly do thu hoi bao cao. Khong bat buoc.</summary>
    [JsonPropertyName("lyDo")]
    public string? LyDo { get; set; }
}

/// <summary>§5.4 D1 — body <c>POST /api/v1/nhiem-vu/{id}/tiep-nhan</c> (§2.4 T2, thiet ke moi §10.3).</summary>
public sealed class TiepNhanRequest
{
    /// <summary>Ghi chu khi tiep nhan. Khong bat buoc.</summary>
    [JsonPropertyName("noidung")]
    public string? NoiDung { get; set; }
}

/// <summary>
/// §5.5 E1 — body <c>POST /api/v1/nhiem-vu/{id}/nghiem-thu</c> (§2.4 T9/T10).
/// DAT      =&gt; trangthaiDvXuly := 11, truc A giu nguyen (DIEM CUOI §2.6).
/// CHUA_DAT =&gt; trangthaiDvXuly := 12; neu truc A thuoc {1,5} thi ve 2 (con han) / 7 (qua han).
/// </summary>
public sealed class NghiemThuRequest
{
    /// <summary>"DAT" | "CHUA_DAT" (xem <c>KetQuaNghiemThu</c>).</summary>
    [JsonPropertyName("ketQua")]
    public string KetQua { get; set; } = string.Empty;

    /// <summary>BAT BUOC o app moi (§1.2 buoc 6, §10.7 — goc khong bat buoc). &lt;= 2000 ky tu.</summary>
    [JsonPropertyName("phanHoi")]
    public string PhanHoi { get; set; } = string.Empty;

    /// <summary>Diem chat luong 1-6, CHI ghi khi ket qua = DAT (§5.5 E1).</summary>
    [JsonPropertyName("hsChatluong")]
    public int? HsChatLuong { get; set; }
}

/// <summary>
/// §2.4 T4/T5 — body xu ly de nghi tu choi cua nguoi giao. MO RONG:
/// §5 khong co endpoint rieng nhung §2.4 co hai dong T4/T5.
/// CHAP_NHAN =&gt; (97, null) · BAC_BO =&gt; (2, 12).
/// </summary>
public sealed class XuLyTuChoiRequest
{
    /// <summary>"CHAP_NHAN" | "BAC_BO" (xem <c>KetQuaXuLyTuChoi</c>).</summary>
    [JsonPropertyName("ketQua")]
    public string KetQua { get; set; } = string.Empty;

    /// <summary>Y kien nguoi giao. Khong bat buoc, &lt;= 2000 ky tu.</summary>
    [JsonPropertyName("phanHoi")]
    public string? PhanHoi { get; set; }
}

/// <summary>§5.4 D7 — mot dong trong lich su xu ly (§4.4).</summary>
public sealed class XuLyDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("idCtnv")]
    public Guid IdCtnv { get; set; }

    /// <summary>TIEPNHAN | TIENDO | BAOCAO | TUCHOI | THUHOI_BC | NGHIEMTHU | ... (§4.4).</summary>
    [JsonPropertyName("loai")]
    public string Loai { get; set; } = string.Empty;

    [JsonPropertyName("tenLoai")]
    public string? TenLoai { get; set; }

    [JsonPropertyName("noidung")]
    public string? NoiDung { get; set; }

    /// <summary>Anh chup % tai thoi diem bao cao — CAI TIEN so voi goc (§4.4).</summary>
    [JsonPropertyName("mucdoht")]
    public int? MucDoHt { get; set; }

    [JsonPropertyName("trangthai")]
    public int? TrangThai { get; set; }

    [JsonPropertyName("tenTrangThai")]
    public string? TenTrangThai { get; set; }

    [JsonPropertyName("trangthaiXuly")]
    public int? TrangThaiXuLy { get; set; }

    [JsonPropertyName("trangthaiDvXuly")]
    public int? TrangThaiDvXuly { get; set; }

    [JsonPropertyName("useridXuly")]
    public Guid UserIdXuLy { get; set; }

    [JsonPropertyName("nguoiXuLyTen")]
    public string? NguoiXuLyTen { get; set; }

    [JsonPropertyName("ngayxuly")]
    public DateTime NgayXuLy { get; set; }

    [JsonPropertyName("files")]
    public List<FileDto> Files { get; set; } = new();
}

/// <summary>§5.4 D7 — goi tra ve cua <c>GET /api/v1/nhiem-vu/{id}/lich-su</c>.</summary>
public sealed class LichSuNhiemVuDto
{
    [JsonPropertyName("xuLy")]
    public List<XuLyDto> XuLy { get; set; } = new();

    [JsonPropertyName("giaHan")]
    public List<GiaHanDto> GiaHan { get; set; } = new();
}
