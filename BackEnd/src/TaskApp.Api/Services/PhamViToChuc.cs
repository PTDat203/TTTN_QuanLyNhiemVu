using Microsoft.EntityFrameworkCore;
using TaskApp.Api.Common;
using TaskApp.Api.Data;
using TaskApp.Api.Entities;

namespace TaskApp.Api.Services;

/// <summary>
/// Vị trí của một người trong tổ chức: cấp nào, phòng nào, nhóm nào.
///
/// <para>
/// Nạp từ database ở mỗi yêu cầu chứ không đọc từ token: người đổi phòng hay đổi nhóm thì
/// quyền mới có hiệu lực ngay, không phải chờ token cũ hết hạn.
/// </para>
/// </summary>
public sealed record ViTriToChuc(long UserId, string VaiTro, long? DepartmentId, long? TeamId);

/// <summary>
/// Phạm vi quyền theo cơ cấu tổ chức — MỘT CHỖ DUY NHẤT định nghĩa "ai giao được việc cho ai"
/// và "ai thấy được nhiệm vụ nào".
///
/// <para>
/// Mọi nơi cần các quy tắc này đều gọi qua đây: kiểm tra khi giao việc, danh sách người nhận
/// trên giao diện, tập ứng viên của AI gợi ý, danh sách nhiệm vụ, xem chi tiết. Nếu mỗi nơi tự
/// viết điều kiện riêng thì sớm muộn sẽ lệch nhau — AI gợi ý một người mà nút "Giao" lại từ chối.
/// </para>
///
/// <para><b>Giao việc.</b> Người nhận phải đang hoạt động, ở cấp THẤP HƠN người giao, và:</para>
/// <list type="bullet">
///   <item>Giám đốc giao cho bất kỳ ai cấp dưới trong công ty.</item>
///   <item>Trưởng phòng chỉ giao cho người trong phòng mình.</item>
///   <item>Trưởng nhóm chỉ giao cho người trong nhóm mình.</item>
///   <item>Nhân viên không giao cho ai.</item>
/// </list>
///
/// <para><b>Xem nhiệm vụ.</b> Ai cũng thấy việc mình tạo và việc giao cho mình. Thêm vào đó:</para>
/// <list type="bullet">
///   <item>Giám đốc thấy mọi nhiệm vụ.</item>
///   <item>Trưởng phòng thấy nhiệm vụ của phòng mình.</item>
///   <item>Trưởng nhóm thấy nhiệm vụ của nhóm mình.</item>
/// </list>
/// <para>
/// "Của phòng" tính theo phòng thực thi của nhiệm vụ, HOẶC phòng của người đang làm, HOẶC phòng
/// của người tạo. Vế cuối để trưởng phòng thấy cả nhiệm vụ trưởng nhóm vừa tạo mà chưa giao ai —
/// lúc đó nhiệm vụ chưa có phòng thực thi lẫn người làm. "Của nhóm" tính theo nhóm phụ trách
/// hoặc nhóm của người đang làm.
/// </para>
///
/// <para>
/// Viết thành biểu thức <see cref="IQueryable{T}"/> để lọc ngay trong SQL: dữ liệu ngoài phạm vi
/// không bao giờ rời khỏi database.
/// </para>
/// </summary>
public static class PhamViToChuc
{
    /// <summary>Nạp vị trí của một người. Rỗng nếu người đó không tồn tại.</summary>
    public static Task<ViTriToChuc?> NapAsync(TaskDbContext db, long userId, CancellationToken ct)
        => db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new ViTriToChuc(u.Id, u.Role, u.DepartmentId, u.TeamId))
            .FirstOrDefaultAsync(ct);

    /// <summary>Những người mà <paramref name="nguoiGiao"/> được giao việc cho.</summary>
    public static IQueryable<User> NguoiNhanDuoc(this IQueryable<User> q, ViTriToChuc nguoiGiao)
    {
        var capDuoi = VaiTro.CacCapDuoi(nguoiGiao.VaiTro);
        if (capDuoi.Length == 0) return q.Where(_ => false);

        q = q.Where(u => u.Status == TrangThaiNguoiDung.HoatDong && capDuoi.Contains(u.Role));

        return nguoiGiao.VaiTro switch
        {
            VaiTro.GiamDoc => q,
            VaiTro.TruongPhong when nguoiGiao.DepartmentId is { } phong
                => q.Where(u => u.DepartmentId == phong),
            VaiTro.TruongNhom when nguoiGiao.TeamId is { } nhom
                => q.Where(u => u.TeamId == nhom),

            // Trưởng phòng không thuộc phòng nào, trưởng nhóm không thuộc nhóm nào: dữ liệu tổ
            // chức đang thiếu. Không đoán phạm vi — thà không giao được còn hơn giao nhầm phòng.
            _ => q.Where(_ => false)
        };
    }

    /// <summary>Những nhiệm vụ mà <paramref name="nguoi"/> được xem.</summary>
    public static IQueryable<TaskItem> ThayDuoc(this IQueryable<TaskItem> q, ViTriToChuc nguoi)
    {
        var id = nguoi.UserId;

        return nguoi.VaiTro switch
        {
            VaiTro.GiamDoc => q,

            VaiTro.TruongPhong when nguoi.DepartmentId is { } phong
                => q.Where(t => t.CreatorId == id || t.AssigneeId == id
                                || t.DepartmentId == phong
                                || (t.Assignee != null && t.Assignee.DepartmentId == phong)
                                || t.Creator!.DepartmentId == phong),

            VaiTro.TruongNhom when nguoi.TeamId is { } nhom
                => q.Where(t => t.CreatorId == id || t.AssigneeId == id
                                || t.TeamId == nhom
                                || (t.Assignee != null && t.Assignee.TeamId == nhom)),

            _ => q.Where(t => t.CreatorId == id || t.AssigneeId == id)
        };
    }

    /// <summary>Nhiệm vụ có tồn tại không, và người này có được xem không.</summary>
    public static async Task<(bool TonTai, bool DuocXem)> QuyenXemAsync(
        TaskDbContext db, long taskId, long userId, CancellationToken ct)
    {
        if (!await db.Tasks.AnyAsync(t => t.Id == taskId, ct)) return (false, false);

        var nguoi = await NapAsync(db, userId, ct);
        var duocXem = nguoi is not null && await db.Tasks.ThayDuoc(nguoi).AnyAsync(t => t.Id == taskId, ct);
        return (true, duocXem);
    }

    /// <summary>
    /// Kiểm người giao có giao được việc cho người nhận không.
    /// Được thì trả về người nhận; không thì trả về lý do để hiện cho người dùng.
    /// </summary>
    public static async Task<(User? NguoiNhan, string? Loi)> KiemTraGiaoAsync(
        TaskDbContext db, long nguoiGiaoId, long nguoiNhanId, CancellationToken ct)
    {
        var nguoiGiao = await NapAsync(db, nguoiGiaoId, ct);
        if (nguoiGiao is null) return (null, "Không xác định được người giao việc.");

        var nguoiNhan = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == nguoiNhanId, ct);
        if (nguoiNhan is null) return (null, $"Không tìm thấy người dùng #{nguoiNhanId}.");

        // Quyết định bằng CHÍNH truy vấn dùng cho danh sách người nhận và tập ứng viên AI,
        // nên ba nơi không thể lệch nhau. LyDoKhongGiaoDuoc chỉ lo phần câu chữ.
        var duoc = await db.Users.NguoiNhanDuoc(nguoiGiao).AnyAsync(u => u.Id == nguoiNhanId, ct);
        return duoc ? (nguoiNhan, null) : (null, LyDoKhongGiaoDuoc(nguoiGiao, nguoiNhan));
    }

    private static string LyDoKhongGiaoDuoc(ViTriToChuc nguoiGiao, User nguoiNhan)
    {
        if (nguoiNhan.Status != TrangThaiNguoiDung.HoatDong)
            return $"Tài khoản \"{nguoiNhan.FullName}\" đã ngừng hoạt động.";

        if (VaiTro.Bac(nguoiNhan.Role) <= VaiTro.Bac(nguoiGiao.VaiTro))
            return $"Chỉ giao được việc cho cấp dưới. \"{nguoiNhan.FullName}\" là " +
                   $"{VaiTro.TenHienThi(nguoiNhan.Role).ToLowerInvariant()}, không thấp hơn cấp của bạn.";

        return nguoiGiao.VaiTro switch
        {
            VaiTro.TruongPhong =>
                $"Trưởng phòng chỉ giao được việc cho người trong phòng mình. " +
                $"\"{nguoiNhan.FullName}\" thuộc phòng khác.",
            VaiTro.TruongNhom =>
                $"Trưởng nhóm chỉ giao được việc cho người trong nhóm mình. " +
                $"\"{nguoiNhan.FullName}\" không thuộc nhóm của bạn.",
            _ => $"Bạn không giao được việc cho \"{nguoiNhan.FullName}\"."
        };
    }
}
