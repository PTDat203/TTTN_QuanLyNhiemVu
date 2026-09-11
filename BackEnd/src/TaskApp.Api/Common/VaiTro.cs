namespace TaskApp.Api.Common;

/// <summary>
/// Bốn cấp trong cơ cấu tổ chức, tương ứng cột <c>USERS.USER_ROLE</c>.
///
/// <para>
/// Vai trò trả lời câu hỏi "người này ở cấp nào", còn <c>DEPARTMENT_ID</c> trả lời
/// "thuộc phòng nào" và <c>MANAGER_ID</c> trả lời "ai quản". Ba thứ này độc lập nhau:
/// một TEAM_LEAD phòng Phát triển và một TEAM_LEAD phòng Nhân sự cùng cấp nhưng
/// phạm vi quản lý hoàn toàn khác.
/// </para>
/// </summary>
public static class VaiTro
{
    /// <summary>Giám đốc — điều hành toàn công ty, giao việc cho mọi phòng.</summary>
    public const string GiamDoc = "DIRECTOR";

    /// <summary>Trưởng phòng — quản một phòng, chỉ giao việc trong phòng mình.</summary>
    public const string TruongPhong = "DEPT_HEAD";

    /// <summary>Trưởng nhóm — quản một nhóm, vừa giao vừa trực tiếp nhận việc.</summary>
    public const string TruongNhom = "TEAM_LEAD";

    /// <summary>Nhân viên — nhận và thực hiện nhiệm vụ.</summary>
    public const string NhanVien = "EMPLOYEE";

    public static readonly string[] TatCa = { GiamDoc, TruongPhong, TruongNhom, NhanVien };

    /// <summary>
    /// Danh sách vai trò được phép giao việc, dạng chuỗi cho <c>[Authorize(Roles = ...)]</c>.
    /// </summary>
    public const string NhomGiaoViec = GiamDoc + "," + TruongPhong + "," + TruongNhom;

    /// <summary>Vai trò được phép quản trị danh mục, phòng ban, người dùng.</summary>
    public const string NhomQuanTri = GiamDoc + "," + TruongPhong;

    public static bool HopLe(string? vaiTro)
        => !string.IsNullOrWhiteSpace(vaiTro) && TatCa.Contains(vaiTro, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Có được giao việc cho người khác không.
    /// Nhân viên thì không — họ chỉ nhận việc.
    /// </summary>
    public static bool CoTheGiaoViec(string? vaiTro)
        => vaiTro is GiamDoc or TruongPhong or TruongNhom;

    /// <summary>
    /// Có được nhận nhiệm vụ không.
    ///
    /// <para>
    /// Trưởng nhóm CÓ nhận việc: trong một nhóm phát triển, trưởng nhóm vẫn trực tiếp
    /// làm chứ không chỉ điều phối. Giám đốc và trưởng phòng thì không — họ giao việc.
    /// Đây cũng là tập ứng viên mà AI xét khi gợi ý.
    /// </para>
    /// </summary>
    public static bool CoTheNhanViec(string? vaiTro)
        => vaiTro is TruongNhom or NhanVien;

    /// <summary>Giám đốc thấy và giao được việc ở mọi phòng, không giới hạn phạm vi.</summary>
    public static bool ThayToanCongTy(string? vaiTro) => vaiTro == GiamDoc;

    /// <summary>Cấp quản lý — dùng để quyết định phạm vi nhìn thấy dữ liệu.</summary>
    public static bool LaCapQuanLy(string? vaiTro)
        => vaiTro is GiamDoc or TruongPhong or TruongNhom;

    /// <summary>Thứ bậc: số càng nhỏ càng cao. Dùng để so ai cấp trên của ai.</summary>
    public static int Bac(string? vaiTro) => vaiTro switch
    {
        GiamDoc => 1,
        TruongPhong => 2,
        TruongNhom => 3,
        NhanVien => 4,
        _ => 99
    };

    public static string TenHienThi(string? vaiTro) => vaiTro switch
    {
        GiamDoc => "Giám đốc",
        TruongPhong => "Trưởng phòng",
        TruongNhom => "Trưởng nhóm",
        NhanVien => "Nhân viên",
        _ => vaiTro ?? string.Empty
    };
}
