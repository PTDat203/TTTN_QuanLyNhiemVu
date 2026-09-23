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
/// <para><b>Xem nhiệm vụ.</b> Hai mức, cố ý khác nhau:</para>
/// <list type="bullet">
///   <item><see cref="ViecCuaToi"/> — việc mình tạo hoặc giao cho mình. Đây là DANH SÁCH.</item>
///   <item><see cref="QuyenXemAsync"/> — thêm mọi mắt xích khác trong cùng chuỗi giao việc.
///         Đây là quyền MỞ CHI TIẾT.</item>
/// </list>
/// <para>
/// Trước đây giám đốc thấy mọi nhiệm vụ, trưởng phòng thấy cả phòng. Bỏ đi vì danh sách đầy
/// những việc người xem không làm gì được: quyền tạm dừng thuộc về người giao, nên mở một việc
/// của phòng khác ra chỉ để thấy không có nút nào bấm được.
/// </para>
/// <para>
/// Mức thứ hai tồn tại vì khối "Chuỗi giao việc" ở màn chi tiết. Giám đốc giao xuống trưởng
/// phòng, trưởng phòng giao tiếp xuống nhân viên: giám đốc cần mở được nhánh dưới để biết lệnh
/// của mình đang nằm ở tay ai, còn nhân viên cần bấm ngược lên nhiệm vụ cha để biết việc này
/// từ đâu ra. Hai chiều đều mở, nhưng chỉ trong phạm vi đúng một chuỗi.
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

    /// <summary>
    /// Việc của chính mình: mình tạo, hoặc giao cho mình. Đây là thứ hiện trong danh sách.
    /// </summary>
    public static IQueryable<TaskItem> ViecCuaToi(this IQueryable<TaskItem> q, long userId)
        => q.Where(t => t.CreatorId == userId || t.AssigneeId == userId);

    /// <summary>
    /// Nhiệm vụ có tồn tại không, và người này có được xem không.
    ///
    /// <para>
    /// Việc của mình thì hiển nhiên xem được. Ngoài ra còn xem được nếu mình giữ BẤT KỲ mắt xích
    /// nào trong cùng chuỗi giao việc — dù ở trên hay ở dưới mắt xích đang mở.
    /// </para>
    /// <para>
    /// Thông cả hai chiều là có chủ đích. Chiều xuống để giám đốc mở được nhánh dưới mà biết lệnh
    /// của mình đang nằm ở tay ai. Chiều lên để nhân viên bấm được vào "Nhiệm vụ cha" ngay trên
    /// màn hình của họ — chặn chiều này thì chính khối "Chuỗi giao việc" lại dẫn tới một trang
    /// báo không có quyền.
    /// </para>
    /// </summary>
    public static async Task<(bool TonTai, bool DuocXem)> QuyenXemAsync(
        TaskDbContext db, long taskId, long userId, CancellationToken ct)
    {
        var nv = await db.Tasks.AsNoTracking()
            .Where(t => t.Id == taskId)
            .Select(t => new { t.CreatorId, t.AssigneeId })
            .FirstOrDefaultAsync(ct);

        if (nv is null) return (false, false);
        if (nv.CreatorId == userId || nv.AssigneeId == userId) return (true, true);

        var chuoi = await ChuoiGiaoViecAsync(db, taskId, ct);
        return (true, chuoi.Any(x => x.CreatorId == userId || x.AssigneeId == userId));
    }

    /// <summary>
    /// Mọi nhiệm vụ cùng một chuỗi giao việc với <paramref name="taskId"/>: ngược lên tới gốc
    /// rồi toả xuống hết các nhánh.
    ///
    /// <para>
    /// Viết thành vòng lặp thay vì một biểu thức LINQ lồng nhiều tầng. Chuỗi dài tối đa bốn mắt
    /// xích — cơ cấu có bốn cấp và mỗi lần giao tiếp chỉ xuống đúng một cấp — nên số lần gọi
    /// database có cận trên rõ ràng, mà đọc lại dễ hơn hẳn. Oracle có CONNECT BY làm được trong
    /// một câu, nhưng phải viết SQL thô và mất khả năng đổi sang provider khác.
    /// </para>
    /// <para>
    /// <paramref name="doSauToiDa"/> chặn vòng lặp vô hạn. Ràng buộc CK_TASKS_PARENT_KHAC_MINH
    /// chỉ cấm nhiệm vụ tự làm cha chính nó, không cấm được một vòng dài hơn.
    /// </para>
    /// </summary>
    private static async Task<List<MatXich>> ChuoiGiaoViecAsync(
        TaskDbContext db, long taskId, CancellationToken ct, int doSauToiDa = 10)
    {
        // --- Ngược lên gốc ---
        var gocId = taskId;
        for (var i = 0; i < doSauToiDa; i++)
        {
            var chaId = await db.Tasks.AsNoTracking()
                .Where(t => t.Id == gocId)
                .Select(t => t.ParentTaskId)
                .FirstOrDefaultAsync(ct);

            if (chaId is not { } cha) break;
            gocId = cha;
        }

        // --- Toả xuống theo chiều rộng ---
        var chuoi = await db.Tasks.AsNoTracking()
            .Where(t => t.Id == gocId)
            .Select(t => new MatXich(t.Id, t.CreatorId, t.AssigneeId))
            .ToListAsync(ct);

        var tangHienTai = chuoi.Select(x => x.Id).ToList();
        for (var i = 0; i < doSauToiDa && tangHienTai.Count > 0; i++)
        {
            var tangSau = await db.Tasks.AsNoTracking()
                .Where(t => t.ParentTaskId != null && tangHienTai.Contains(t.ParentTaskId.Value))
                .Select(t => new MatXich(t.Id, t.CreatorId, t.AssigneeId))
                .ToListAsync(ct);

            if (tangSau.Count == 0) break;
            chuoi.AddRange(tangSau);
            tangHienTai = tangSau.Select(x => x.Id).ToList();
        }

        return chuoi;
    }

    /// <summary>Một mắt xích trong chuỗi giao việc — chỉ cần hai người để xét quyền xem.</summary>
    private sealed record MatXich(long Id, long CreatorId, long? AssigneeId);

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
