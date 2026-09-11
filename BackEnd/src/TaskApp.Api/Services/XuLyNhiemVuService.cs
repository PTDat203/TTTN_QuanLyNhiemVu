using Microsoft.EntityFrameworkCore;
using TaskApp.Api.Common;
using TaskApp.Api.Data;
using TaskApp.Api.Dtos;
using TaskApp.Api.Entities;

namespace TaskApp.Api.Services;

/// <summary>
/// Nửa sau của vòng nghiệp vụ: cập nhật tiến độ, gửi báo cáo, duyệt báo cáo.
///
/// <para>Vòng đời đầy đủ (mục 7 của DATABASE_SOURCE_OF_TRUTH):</para>
/// <code>
/// DANG_THUC_HIEN --gửi báo cáo--> CHO_XAC_NHAN --xác nhận--> HOAN_THANH
///                                      |
///                                  từ chối
///                                      v
///                              YEU_CAU_BO_SUNG --làm lại--> DANG_THUC_HIEN
/// </code>
/// </summary>
public sealed class XuLyNhiemVuService
{
    private readonly TaskDbContext _db;
    private readonly ILogger<XuLyNhiemVuService> _log;

    public XuLyNhiemVuService(TaskDbContext db, ILogger<XuLyNhiemVuService> log)
    {
        _db = db;
        _log = log;
    }

    // =====================================================================
    // TIẾN ĐỘ
    // =====================================================================

    /// <summary>
    /// Ghi một lần cập nhật tiến độ. Chỉ người được giao.
    ///
    /// <para>
    /// <b>Luôn INSERT dòng mới, không bao giờ sửa dòng cũ.</b> Lịch sử tiến độ là dữ liệu
    /// để chấm điểm và để đối chiếu khi nghiệm thu — ghi đè là mất dấu vết.
    /// </para>
    /// <para>
    /// Nếu nhiệm vụ đang ở YEU_CAU_BO_SUNG thì việc cập nhật tiến độ đồng nghĩa người thực
    /// hiện đã bắt tay làm lại, nên tự chuyển về DANG_THUC_HIEN. Bước chuyển này vẫn đi qua
    /// bảng vòng đời chứ không gán thẳng.
    /// </para>
    /// </summary>
    public async Task<KetQua<TienDoDto>> CapNhatTienDoAsync(
        long taskId, CapNhatTienDoRequest yeuCau, long userId, CancellationToken ct = default)
    {
        if (yeuCau.ProgressPercent is < 0 or > 100)
        {
            return KetQua<TienDoDto>.DuLieuKhongHopLe(
                $"Phần trăm tiến độ phải nằm trong khoảng 0 đến 100. Giá trị nhận được: {yeuCau.ProgressPercent}.");
        }

        if (yeuCau.Content is { Length: > 2000 })
        {
            return KetQua<TienDoDto>.DuLieuKhongHopLe("Nội dung cập nhật không được quá 2000 ký tự.");
        }

        var nv = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
        if (nv is null) return KetQua<TienDoDto>.KhongTimThay($"Không tìm thấy nhiệm vụ #{taskId}.");

        if (nv.AssigneeId != userId)
        {
            return KetQua<TienDoDto>.KhongCoQuyen(
                "Chỉ người được giao mới cập nhật được tiến độ của nhiệm vụ này.");
        }

        if (nv.StatusCode != TrangThaiNhiemVu.DangThucHien &&
            nv.StatusCode != TrangThaiNhiemVu.YeuCauBoSung)
        {
            return KetQua<TienDoDto>.ThatBai(
                $"Chỉ cập nhật được tiến độ khi nhiệm vụ đang ở \"Đang thực hiện\" hoặc " +
                $"\"Yêu cầu bổ sung\". Nhiệm vụ này đang ở \"{TrangThaiNhiemVu.TenHienThi(nv.StatusCode)}\".",
                MaLoiChung.ChuyenTrangThaiKhongHopLe);
        }

        var banGhi = new TaskProgress
        {
            TaskId = taskId,
            UserId = userId,
            ProgressPercent = yeuCau.ProgressPercent,
            Content = yeuCau.Content?.Trim()
        };
        _db.TaskProgresses.Add(banGhi);

        // Đang bị trả lại mà cập nhật tiến độ nghĩa là đã bắt tay làm lại.
        if (nv.StatusCode == TrangThaiNhiemVu.YeuCauBoSung &&
            TrangThaiNhiemVu.ChuyenDuoc(nv.StatusCode, TrangThaiNhiemVu.DangThucHien))
        {
            nv.StatusCode = TrangThaiNhiemVu.DangThucHien;
            _log.LogInformation("Nhiệm vụ #{Id} trở lại Đang thực hiện sau khi bị yêu cầu bổ sung", taskId);
        }

        // Một lần SaveChanges duy nhất: EF Core gói mọi thay đổi đang theo dõi vào cùng
        // một giao dịch, nên bản ghi tiến độ và trạng thái nhiệm vụ không thể lệch nhau.
        await _db.SaveChangesAsync(ct);

        var tenNguoi = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId).Select(u => u.FullName).FirstOrDefaultAsync(ct);

        return KetQua<TienDoDto>.Ok(new TienDoDto
        {
            Id = banGhi.Id,
            UserId = userId,
            TenNguoiCapNhat = tenNguoi,
            ProgressPercent = banGhi.ProgressPercent,
            Content = banGhi.Content,
            CreatedAt = banGhi.CreatedAt
        });
    }

    /// <summary>Lịch sử tiến độ của một nhiệm vụ, mới nhất lên đầu.</summary>
    public async Task<KetQua<IReadOnlyList<TienDoDto>>> LichSuTienDoAsync(
        long taskId, long userId, string vaiTro, CancellationToken ct = default)
    {
        var nv = await _db.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId, ct);
        if (nv is null)
            return KetQua<IReadOnlyList<TienDoDto>>.KhongTimThay($"Không tìm thấy nhiệm vụ #{taskId}.");

        if (!DuocXem(nv, userId, vaiTro))
            return KetQua<IReadOnlyList<TienDoDto>>.KhongCoQuyen("Bạn không có quyền xem nhiệm vụ này.");

        var ds = await _db.TaskProgresses.AsNoTracking()
            .Where(p => p.TaskId == taskId)
            .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
            .Select(p => new TienDoDto
            {
                Id = p.Id,
                UserId = p.UserId,
                TenNguoiCapNhat = p.User!.FullName,
                ProgressPercent = p.ProgressPercent,
                Content = p.Content,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(ct);

        return KetQua<IReadOnlyList<TienDoDto>>.Ok(ds);
    }

    // =====================================================================
    // BÁO CÁO
    // =====================================================================

    /// <summary>
    /// Người thực hiện gửi báo cáo kết quả: DANG_THUC_HIEN -> CHO_XAC_NHAN.
    ///
    /// <para>
    /// Một nhiệm vụ có thể có nhiều báo cáo — mỗi lần bị từ chối rồi gửi lại là thêm một
    /// bản ghi mới, giữ nguyên các bản cũ để còn đối chiếu.
    /// </para>
    /// </summary>
    public async Task<KetQua<BaoCaoDto>> GuiBaoCaoAsync(
        long taskId, GuiBaoCaoRequest yeuCau, long userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(yeuCau.Content))
        {
            return KetQua<BaoCaoDto>.DuLieuKhongHopLe("Nội dung báo cáo không được để trống.");
        }

        var nv = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
        if (nv is null) return KetQua<BaoCaoDto>.KhongTimThay($"Không tìm thấy nhiệm vụ #{taskId}.");

        if (nv.AssigneeId != userId)
        {
            return KetQua<BaoCaoDto>.KhongCoQuyen(
                "Chỉ người được giao mới gửi được báo cáo cho nhiệm vụ này.");
        }

        if (!TrangThaiNhiemVu.ChuyenDuoc(nv.StatusCode, TrangThaiNhiemVu.ChoXacNhan))
        {
            var goiY = nv.StatusCode == TrangThaiNhiemVu.YeuCauBoSung
                ? " Hãy cập nhật tiến độ trước để tiếp tục thực hiện, rồi mới gửi báo cáo mới."
                : string.Empty;

            return KetQua<BaoCaoDto>.ThatBai(
                $"Không gửi được báo cáo khi nhiệm vụ đang ở " +
                $"\"{TrangThaiNhiemVu.TenHienThi(nv.StatusCode)}\".{goiY}",
                MaLoiChung.ChuyenTrangThaiKhongHopLe);
        }

        var baoCao = new TaskReport
        {
            TaskId = taskId,
            ReporterId = userId,
            Content = yeuCau.Content.Trim(),
            Status = TrangThaiBaoCao.ChoXacNhan
        };
        _db.TaskReports.Add(baoCao);

        nv.StatusCode = TrangThaiNhiemVu.ChoXacNhan;

        await _db.SaveChangesAsync(ct);

        _log.LogInformation("Nhiệm vụ #{Id} gửi báo cáo #{BaoCaoId}, chờ xác nhận", taskId, baoCao.Id);

        var tenNguoi = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId).Select(u => u.FullName).FirstOrDefaultAsync(ct);

        return KetQua<BaoCaoDto>.Ok(ChuyenDoi(baoCao, tenNguoi, null));
    }

    /// <summary>
    /// Người giao duyệt báo cáo.
    ///
    /// <list type="bullet">
    ///   <item>Xác nhận: báo cáo -> DA_XAC_NHAN, nhiệm vụ -> HOAN_THANH.</item>
    ///   <item>Từ chối: báo cáo -> TU_CHOI, nhiệm vụ -> YEU_CAU_BO_SUNG. Bắt buộc có ý kiến.</item>
    /// </list>
    /// </summary>
    public async Task<KetQua<BaoCaoDto>> DuyetBaoCaoAsync(
        long baoCaoId, DuyetBaoCaoRequest yeuCau, long userId, CancellationToken ct = default)
    {
        var baoCao = await _db.TaskReports
            .Include(r => r.Task)
            .FirstOrDefaultAsync(r => r.Id == baoCaoId, ct);

        if (baoCao?.Task is null)
            return KetQua<BaoCaoDto>.KhongTimThay($"Không tìm thấy báo cáo #{baoCaoId}.");

        var nv = baoCao.Task;

        if (nv.CreatorId != userId)
        {
            return KetQua<BaoCaoDto>.KhongCoQuyen(
                "Chỉ người giao nhiệm vụ mới được duyệt báo cáo này.");
        }

        // Chỉ duyệt báo cáo còn đang chờ. Duyệt lại báo cáo đã xử lý sẽ làm trạng thái
        // nhiệm vụ nhảy lung tung so với lịch sử báo cáo.
        if (baoCao.Status != TrangThaiBaoCao.ChoXacNhan)
        {
            return KetQua<BaoCaoDto>.ThatBai(
                $"Báo cáo này đã được xử lý rồi (trạng thái: " +
                $"\"{TrangThaiBaoCao.TenHienThi(baoCao.Status)}\"), không duyệt lại được.",
                MaLoiChung.ChuyenTrangThaiKhongHopLe);
        }

        if (!yeuCau.XacNhan && string.IsNullOrWhiteSpace(yeuCau.ReviewNote))
        {
            return KetQua<BaoCaoDto>.DuLieuKhongHopLe(
                "Phải nêu lý do khi từ chối báo cáo, để người thực hiện biết cần bổ sung gì.");
        }

        var trangThaiMoi = yeuCau.XacNhan
            ? TrangThaiNhiemVu.HoanThanh
            : TrangThaiNhiemVu.YeuCauBoSung;

        if (!TrangThaiNhiemVu.ChuyenDuoc(nv.StatusCode, trangThaiMoi))
        {
            return KetQua<BaoCaoDto>.ThatBai(
                $"Không chuyển được nhiệm vụ từ \"{TrangThaiNhiemVu.TenHienThi(nv.StatusCode)}\" " +
                $"sang \"{TrangThaiNhiemVu.TenHienThi(trangThaiMoi)}\".",
                MaLoiChung.ChuyenTrangThaiKhongHopLe);
        }

        baoCao.Status = yeuCau.XacNhan ? TrangThaiBaoCao.DaXacNhan : TrangThaiBaoCao.TuChoi;
        baoCao.ReviewerId = userId;
        baoCao.ReviewNote = yeuCau.ReviewNote?.Trim();
        baoCao.ReviewedAt = DateTime.Now;

        nv.StatusCode = trangThaiMoi;

        // Cập nhật báo cáo và cập nhật nhiệm vụ nằm trong CÙNG một SaveChanges, nên EF Core
        // gói chúng vào một giao dịch. Không có cảnh báo cáo đã duyệt mà nhiệm vụ chưa đổi.
        await _db.SaveChangesAsync(ct);

        _log.LogInformation("Báo cáo #{BaoCaoId} được {KetQua}, nhiệm vụ #{TaskId} -> {TrangThai}",
            baoCaoId, yeuCau.XacNhan ? "xác nhận" : "từ chối", nv.Id, trangThaiMoi);

        var ten = await _db.Users.AsNoTracking()
            .Where(u => u.Id == baoCao.ReporterId).Select(u => u.FullName).FirstOrDefaultAsync(ct);
        var tenDuyet = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId).Select(u => u.FullName).FirstOrDefaultAsync(ct);

        return KetQua<BaoCaoDto>.Ok(ChuyenDoi(baoCao, ten, tenDuyet));
    }

    /// <summary>Danh sách báo cáo của một nhiệm vụ, mới nhất lên đầu.</summary>
    public async Task<KetQua<IReadOnlyList<BaoCaoDto>>> DanhSachBaoCaoAsync(
        long taskId, long userId, string vaiTro, CancellationToken ct = default)
    {
        var nv = await _db.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId, ct);
        if (nv is null)
            return KetQua<IReadOnlyList<BaoCaoDto>>.KhongTimThay($"Không tìm thấy nhiệm vụ #{taskId}.");

        if (!DuocXem(nv, userId, vaiTro))
            return KetQua<IReadOnlyList<BaoCaoDto>>.KhongCoQuyen("Bạn không có quyền xem nhiệm vụ này.");

        var ds = await _db.TaskReports.AsNoTracking()
            .Where(r => r.TaskId == taskId)
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .Select(r => new BaoCaoDto
            {
                Id = r.Id,
                ReporterId = r.ReporterId,
                TenNguoiBaoCao = r.Reporter!.FullName,
                Content = r.Content,
                ReportStatus = r.Status,
                TenTrangThaiBaoCao = TrangThaiBaoCao.TenHienThi(r.Status),
                ReviewerId = r.ReviewerId,
                TenNguoiDuyet = r.Reviewer != null ? r.Reviewer.FullName : null,
                ReviewNote = r.ReviewNote,
                CreatedAt = r.CreatedAt,
                ReviewedAt = r.ReviewedAt
            })
            .ToListAsync(ct);

        return KetQua<IReadOnlyList<BaoCaoDto>>.Ok(ds);
    }

    /// <summary>
    /// Các báo cáo đang chờ chính người này duyệt. Là hộp thư việc cần xử lý của MANAGER.
    /// </summary>
    public async Task<KetQua<IReadOnlyList<BaoCaoDto>>> ChoToiDuyetAsync(
        long userId, CancellationToken ct = default)
    {
        var ds = await _db.TaskReports.AsNoTracking()
            .Where(r => r.Status == TrangThaiBaoCao.ChoXacNhan && r.Task!.CreatorId == userId)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new BaoCaoDto
            {
                Id = r.Id,
                ReporterId = r.ReporterId,
                TenNguoiBaoCao = r.Reporter!.FullName,
                Content = r.Content,
                ReportStatus = r.Status,
                TenTrangThaiBaoCao = TrangThaiBaoCao.TenHienThi(r.Status),
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(ct);

        return KetQua<IReadOnlyList<BaoCaoDto>>.Ok(ds);
    }

    // =====================================================================
    private static bool DuocXem(TaskItem nv, long userId, string vaiTro)
        => nv.CreatorId == userId || nv.AssigneeId == userId || VaiTro.LaCapQuanLy(vaiTro);

    private static BaoCaoDto ChuyenDoi(TaskReport r, string? tenNguoiBaoCao, string? tenNguoiDuyet) => new()
    {
        Id = r.Id,
        ReporterId = r.ReporterId,
        TenNguoiBaoCao = tenNguoiBaoCao,
        Content = r.Content,
        ReportStatus = r.Status,
        TenTrangThaiBaoCao = TrangThaiBaoCao.TenHienThi(r.Status),
        ReviewerId = r.ReviewerId,
        TenNguoiDuyet = tenNguoiDuyet,
        ReviewNote = r.ReviewNote,
        CreatedAt = r.CreatedAt,
        ReviewedAt = r.ReviewedAt
    };
}
