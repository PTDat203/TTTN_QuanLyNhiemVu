using System.Text.Json.Serialization;

namespace QLNV.Core.Dtos;

/// <summary>
/// §5.10 J1 — <c>GET /api/v1/dashboard/tong-quan</c> (man M02).
///
/// Quy tac dem da kiem chung (ban metrics.js): chi nhiem vu DANG MO
/// (trangthai thuoc {2,3,7,13}) moi duoc dem vao <see cref="QuaHan"/> / <see cref="SapHetHan"/>.
/// Viec da bao cao xong dang cho xac nhan (1|5, 10) hay da tu choi (6, *) KHONG dem vao hai
/// the nay — neu dem thi the "Qua han" chong lan the "Cho xac nhan".
/// </summary>
public sealed class DashboardTongQuanDto
{
    /// <summary>Ngay dung lam moc tinh han.</summary>
    [JsonPropertyName("homNay")]
    public DateOnly HomNay { get; set; }

    /// <summary>The 1 — trangthai = 3.</summary>
    [JsonPropertyName("chuaTrienKhai")]
    public int ChuaTrienKhai { get; set; }

    /// <summary>The 2 — trangthai = 2 hoac 13 (13 = dang cho duyet gia han, van la viec dang lam).</summary>
    [JsonPropertyName("dangTrienKhai")]
    public int DangTrienKhai { get; set; }

    /// <summary>The 3 — trangthaiDvXuly = 10.</summary>
    [JsonPropertyName("choXacNhan")]
    public int ChoXacNhan { get; set; }

    /// <summary>The 4 — dang mo va da qua han.</summary>
    [JsonPropertyName("quaHan")]
    public int QuaHan { get; set; }

    /// <summary>Dang mo va con lai 0..3 ngay.</summary>
    [JsonPropertyName("sapHetHan")]
    public int SapHetHan { get; set; }

    /// <summary>§2.6 diem cuoi — trangthai thuoc {1,5} VA trangthaiDvXuly = 11.</summary>
    [JsonPropertyName("hoanThanh")]
    public int HoanThanh { get; set; }

    [JsonPropertyName("tongSo")]
    public int TongSo { get; set; }

    [JsonPropertyName("tyLeHoanThanh")]
    public double TyLeHoanThanh { get; set; }

    /// <summary>Bieu do tron theo trang thai — giu du 8 ma cua §2.1 de mau on dinh.</summary>
    [JsonPropertyName("theoTrangThai")]
    public List<DemTheoTrangThaiDto> TheoTrangThai { get; set; } = new();

    /// <summary>Bieu do cot theo don vi (§5.10 J2).</summary>
    [JsonPropertyName("theoDonVi")]
    public List<DashboardDonViDto> TheoDonVi { get; set; } = new();

    /// <summary>§3.1 M02 — "Viec cua toi sap den han (&lt;= 3 ngay)".</summary>
    [JsonPropertyName("sapDenHan")]
    public List<NhiemVuSapDenHanDto> SapDenHan { get; set; } = new();
}

/// <summary>§3.1 M02 — mot lat cua bieu do tron theo trang thai.</summary>
public sealed class DemTheoTrangThaiDto
{
    [JsonPropertyName("ma")]
    public int Ma { get; set; }

    [JsonPropertyName("nhan")]
    public string Nhan { get; set; } = string.Empty;

    [JsonPropertyName("mau")]
    public string? Mau { get; set; }

    [JsonPropertyName("soLuong")]
    public int SoLuong { get; set; }

    [JsonPropertyName("tyLe")]
    public double TyLe { get; set; }
}

/// <summary>§5.10 J2 — <c>GET /api/v1/dashboard/theo-don-vi</c>.</summary>
public sealed class DashboardDonViDto
{
    [JsonPropertyName("unitcode")]
    public string UnitCode { get; set; } = string.Empty;

    [JsonPropertyName("tendonvi")]
    public string TenDonVi { get; set; } = string.Empty;

    [JsonPropertyName("tongSo")]
    public int TongSo { get; set; }

    /// <summary>§2.6 diem cuoi — da nghiem thu.</summary>
    [JsonPropertyName("hoanThanh")]
    public int HoanThanh { get; set; }

    [JsonPropertyName("quaHan")]
    public int QuaHan { get; set; }

    [JsonPropertyName("tyLeHoanThanh")]
    public double TyLeHoanThanh { get; set; }
}

/// <summary>§3.1 M02 — mot dong trong danh sach "Viec cua toi sap den han".</summary>
public sealed class NhiemVuSapDenHanDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("noidung")]
    public string NoiDung { get; set; } = string.Empty;

    [JsonPropertyName("hanxulyth")]
    public DateOnly? HanXuLyTh { get; set; }

    /// <summary>Am = qua han.</summary>
    [JsonPropertyName("soNgayConLai")]
    public int? SoNgayConLai { get; set; }

    [JsonPropertyName("dokhan")]
    public string DoKhan { get; set; } = string.Empty;

    [JsonPropertyName("trangthai")]
    public int TrangThai { get; set; }

    [JsonPropertyName("quaHan")]
    public bool QuaHan { get; set; }
}
