namespace TaskApp.Api.Entities;

/// <summary>
/// Bằng cấp, chứng chỉ của một người. Bảng USER_QUALIFICATIONS.
///
/// <para>
/// Tách khỏi <see cref="UserSkill"/> vì khác bản chất: bằng cấp là chứng nhận đào tạo
/// chính quy, kiểm chứng được, ít thay đổi; kỹ năng là mức thành thạo tự khai và
/// biến động theo công việc.
/// </para>
/// <para>
/// <b>Bằng cấp KHÔNG dùng làm điều kiện lọc.</b> Nó chỉ là một tín hiệu xếp hạng có
/// trọng số nhỏ. Người học Kinh tế nhưng đang làm Backend nhiều năm với kỹ năng tốt
/// vẫn phải được xếp cao — lấy bằng cấp làm điều kiện cứng là loại oan người đó.
/// </para>
/// </summary>
public class UserQualification : IAuditable
{
    public long Id { get; set; }
    public long UserId { get; set; }

    /// <summary>Tên bằng, ví dụ "Kỹ sư Công nghệ thông tin".</summary>
    public string DegreeName { get; set; } = string.Empty;

    /// <summary>Chuyên ngành, ví dụ "Kỹ thuật phần mềm". Đây là phần AI dùng để so khớp.</summary>
    public string Major { get; set; } = string.Empty;

    /// <summary>TRUNG_CAP / CAO_DANG / DAI_HOC / THAC_SI / TIEN_SI / CHUNG_CHI.</summary>
    public string? DegreeLevel { get; set; }

    public string? School { get; set; }
    public int? GradYear { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
}

/// <summary>Bậc bằng cấp và trọng số quy đổi khi chấm điểm.</summary>
public static class BacBangCap
{
    public const string TrungCap = "TRUNG_CAP";
    public const string CaoDang = "CAO_DANG";
    public const string DaiHoc = "DAI_HOC";
    public const string ThacSi = "THAC_SI";
    public const string TienSi = "TIEN_SI";
    public const string ChungChi = "CHUNG_CHI";

    /// <summary>
    /// Quy đổi bậc sang hệ số 0..1. Chênh lệch cố ý giữ hẹp: bằng cao hơn là một lợi thế
    /// nhẹ, không phải yếu tố quyết định — người có kinh nghiệm thực tế vẫn hơn.
    /// </summary>
    public static double HeSo(string? bac) => bac switch
    {
        TienSi => 1.00,
        ThacSi => 0.90,
        DaiHoc => 0.80,
        CaoDang => 0.65,
        TrungCap => 0.50,
        ChungChi => 0.60,
        _ => 0.60
    };

    public static string TenHienThi(string? bac) => bac switch
    {
        TienSi => "Tiến sĩ",
        ThacSi => "Thạc sĩ",
        DaiHoc => "Đại học",
        CaoDang => "Cao đẳng",
        TrungCap => "Trung cấp",
        ChungChi => "Chứng chỉ",
        _ => bac ?? string.Empty
    };
}
