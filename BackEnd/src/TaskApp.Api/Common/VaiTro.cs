namespace TaskApp.Api.Common;

/// <summary>
/// Bốn cấp trong cơ cấu tổ chức, tương ứng cột <c>USERS.USER_ROLE</c>.
///
/// <para>
/// Vai trò trả lời câu hỏi "người này ở cấp nào", còn <c>DEPARTMENT_ID</c> và
/// <c>TEAM_ID</c> trả lời "thuộc đâu". Hai thứ độc lập nhau: hai trưởng nhóm ở hai nhóm
/// khác nhau cùng cấp nhưng phạm vi quản lý không giao nhau.
/// </para>
/// <para>
/// Đây là QUYỀN TRONG HỆ THỐNG, khác với chức danh (<c>JOB_TITLE</c>) chỉ để hiển thị.
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
    /// Các vai trò THẤP HƠN vai trò đã cho — tức những cấp mà người này được giao việc cho.
    ///
    /// <para>
    /// Quy tắc là "cấp trên giao cho cấp dưới", không phải một danh sách cố định ai được
    /// nhận việc. Trưởng phòng vừa nhận việc từ Giám đốc vừa giao cho trưởng nhóm; trưởng
    /// nhóm vừa nhận từ trưởng phòng vừa giao cho nhân viên. Chỉ Giám đốc không nhận việc
    /// của ai, và chỉ nhân viên không giao việc cho ai.
    /// </para>
    /// <para>
    /// Đây mới là quy tắc về CẤP. Quy tắc về PHẠM VI (cùng phòng, cùng nhóm) nằm ở
    /// <c>Services.PhamViToChuc</c>.
    /// </para>
    /// </summary>
    public static string[] CacCapDuoi(string? vaiTro)
        => TatCa.Where(v => Bac(v) > Bac(vaiTro)).ToArray();

    // Đã gỡ ThayToanCongTy và LaCapQuanLy ngày 06/10/2026: không nơi nào gọi, mà chú thích của
    // chúng còn mô tả quy tắc "phạm vi nhìn thấy dữ liệu" đã bị bỏ từ 29/09 — danh sách nhiệm vụ
    // nay chỉ hiện việc mình tạo hoặc giao cho mình, kể cả Giám đốc. Mã chết mà mô tả sai quy tắc
    // còn hại hơn không có mã. Xem PhamViToChuc.ViecCuaToi và PhamViToChuc.QuyenXemAsync.

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
