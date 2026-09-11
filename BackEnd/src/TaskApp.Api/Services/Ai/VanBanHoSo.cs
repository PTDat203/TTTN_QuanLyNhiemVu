using System.Text;

namespace TaskApp.Api.Services.Ai;

/// <summary>
/// Dựng các đoạn văn đưa vào mô hình.
///
/// <para>
/// Gom về một chỗ để thấy rõ mô hình "nhìn" thấy gì. Khi kết quả gợi ý trông lạ, việc đầu tiên
/// nên làm là in đoạn văn ở đây ra xem — phần lớn lỗi nằm ở đầu vào chứ không ở mô hình.
/// </para>
/// <para>
/// Viết thành câu tiếng Việt tự nhiên chứ không nối từ khoá: mô hình học trên câu văn, đưa câu
/// văn thì véc-tơ ổn định hơn. Hồ sơ người KHÔNG kèm lịch sử việc đã làm — lịch sử đã có thành
/// phần điểm riêng, đưa vào đây nữa là tính hai lần.
/// </para>
/// </summary>
public static class VanBanHoSo
{
    public static string NhiemVu(string? tieuDe, string? moTa)
    {
        var td = tieuDe?.Trim() ?? string.Empty;
        var mt = moTa?.Trim() ?? string.Empty;
        if (mt.Length == 0) return td;
        if (td.Length == 0) return mt;
        return td.EndsWith('.') ? $"{td} {mt}" : $"{td}. {mt}";
    }

    public static string NguoiThucHien(
        string? chucDanh, string? tenPhong, string? tenNhom,
        IEnumerable<(string Ten, int Muc, decimal? SoNam, string? MoTa)> kyNang,
        IEnumerable<(string Bang, string ChuyenNganh)> hocVan)
    {
        var sb = new StringBuilder();

        var viTri = new[] { chucDanh, tenNhom, tenPhong }.Where(s => !string.IsNullOrWhiteSpace(s));
        sb.Append(string.Join(", ", viTri)).Append(". ");

        var dsKyNang = kyNang.OrderByDescending(k => k.Muc).Select(k =>
        {
            var phan = $"{k.Ten} mức {k.Muc}/5";
            if (k.SoNam is > 0) phan += $", {k.SoNam:0.#} năm";
            if (!string.IsNullOrWhiteSpace(k.MoTa)) phan += $" ({k.MoTa.Trim()})";
            return phan;
        }).ToList();
        if (dsKyNang.Count > 0) sb.Append("Kỹ năng: ").Append(string.Join("; ", dsKyNang)).Append(". ");

        var dsHocVan = hocVan.Select(h => $"{h.Bang}, chuyên ngành {h.ChuyenNganh}").ToList();
        if (dsHocVan.Count > 0) sb.Append("Học vấn: ").Append(string.Join("; ", dsHocVan)).Append('.');

        return sb.ToString().Trim();
    }

    public static string PhongBan(
        string ten, string? moTa,
        IEnumerable<(string Ten, string? MoTa)> nhom,
        IEnumerable<string> kyNangChinh, IEnumerable<string> chuyenNganh)
    {
        var sb = new StringBuilder(ten).Append('.');
        if (!string.IsNullOrWhiteSpace(moTa)) sb.Append(' ').Append(moTa.Trim().TrimEnd('.')).Append('.');

        foreach (var n in nhom)
        {
            sb.Append(' ').Append(n.Ten);
            if (!string.IsNullOrWhiteSpace(n.MoTa)) sb.Append(": ").Append(n.MoTa.Trim().TrimEnd('.'));
            sb.Append('.');
        }

        var kn = kyNangChinh.ToList();
        if (kn.Count > 0) sb.Append(" Kỹ năng chính: ").Append(string.Join(", ", kn)).Append('.');

        var cn = chuyenNganh.ToList();
        if (cn.Count > 0) sb.Append(" Chuyên ngành: ").Append(string.Join(", ", cn)).Append('.');

        return sb.ToString();
    }

    public static string Nhom(string ten, string? tenPhong, string? moTa, IEnumerable<string> kyNangChinh)
    {
        var sb = new StringBuilder(ten);
        if (!string.IsNullOrWhiteSpace(tenPhong)) sb.Append(", ").Append(tenPhong);
        sb.Append('.');
        if (!string.IsNullOrWhiteSpace(moTa)) sb.Append(' ').Append(moTa.Trim().TrimEnd('.')).Append('.');

        var kn = kyNangChinh.ToList();
        if (kn.Count > 0) sb.Append(" Kỹ năng chính: ").Append(string.Join(", ", kn)).Append('.');

        return sb.ToString();
    }

    public static string KyNang(string ten, string? moTa)
        => string.IsNullOrWhiteSpace(moTa) ? ten : $"{ten}: {moTa.Trim()}";
}
