using Microsoft.EntityFrameworkCore;
using TaskApp.Api.Common;
using TaskApp.Api.Data;
using TaskApp.Api.Dtos;
using TaskApp.Api.Entities;

namespace TaskApp.Api.Services;

/// <summary>
/// Nghiệp vụ nhiệm vụ: tạo, sửa, xóa, giao việc, tiếp nhận, tra cứu.
///
/// <para>
/// Đây là nơi duy nhất đặt quy tắc vòng đời trạng thái. Controller chỉ nhận yêu cầu,
/// gọi service, trả kết quả — không tự chuyển trạng thái. Gom một chỗ thì quy tắc
/// không bị lệch giữa các endpoint.
/// </para>
/// <para>
/// Mọi chuyển trạng thái đều đi qua <see cref="TrangThaiNhiemVu.ChuyenDuoc"/>. Bước chuyển
/// không nằm trong vòng đời sẽ bị chặn kèm thông báo chỉ rõ đang ở đâu, đi được tới đâu.
/// </para>
/// </summary>
public sealed class NhiemVuService
{
    private readonly TaskDbContext _db;
    private readonly ILogger<NhiemVuService> _log;

    public NhiemVuService(TaskDbContext db, ILogger<NhiemVuService> log)
    {
        _db = db;
        _log = log;
    }

    // =====================================================================
    // TRA CỨU
    // =====================================================================

    /// <summary>
    /// Danh sách nhiệm vụ có lọc, sắp xếp và phân trang.
    ///
    /// <para>
    /// <b>Giới hạn theo vai trò áp ngay trong truy vấn, không lọc sau khi lấy về:</b>
    /// EMPLOYEE chỉ thấy nhiệm vụ giao cho mình, MANAGER chỉ thấy nhiệm vụ mình tạo.
    /// Lọc ở tầng SQL thì dữ liệu người khác không bao giờ rời khỏi database.
    /// </para>
    /// </summary>
    public async Task<KetQuaPhanTrang<NhiemVuTomTatDto>> DanhSachAsync(
        NhiemVuLocRequest loc, long userId, string vaiTro, CancellationToken ct = default)
    {
        var truyVan = _db.Tasks.AsNoTracking().AsQueryable();

        // --- Giới hạn phạm vi theo vai trò ---
        if (vaiTro == VaiTro.Employee)
        {
            truyVan = truyVan.Where(t => t.AssigneeId == userId);
        }
        else if (vaiTro == VaiTro.Manager)
        {
            truyVan = truyVan.Where(t => t.CreatorId == userId);
        }

        // --- Bộ lọc ---
        if (!string.IsNullOrWhiteSpace(loc.StatusCode))
            truyVan = truyVan.Where(t => t.StatusCode == loc.StatusCode);

        if (!string.IsNullOrWhiteSpace(loc.Priority))
            truyVan = truyVan.Where(t => t.Priority == loc.Priority);

        if (loc.AssigneeId.HasValue)
            truyVan = truyVan.Where(t => t.AssigneeId == loc.AssigneeId.Value);

        if (loc.CreatorId.HasValue)
            truyVan = truyVan.Where(t => t.CreatorId == loc.CreatorId.Value);

        if (loc.ChuaGiao == true)
            truyVan = truyVan.Where(t => t.AssigneeId == null);

        if (!string.IsNullOrWhiteSpace(loc.TuKhoa))
        {
            // ToLower để tìm không phân biệt hoa thường; Oracle mặc định phân biệt.
            var tuKhoa = loc.TuKhoa.Trim().ToLower();
            truyVan = truyVan.Where(t => t.Title.ToLower().Contains(tuKhoa));
        }

        if (loc.HanTuNgay.HasValue)
            truyVan = truyVan.Where(t => t.DueDate >= loc.HanTuNgay.Value);

        if (loc.HanDenNgay.HasValue)
            truyVan = truyVan.Where(t => t.DueDate <= loc.HanDenNgay.Value);

        if (loc.QuaHan == true)
        {
            var homNay = DateTime.Today;
            truyVan = truyVan.Where(t =>
                t.DueDate != null &&
                t.DueDate < homNay &&
                t.StatusCode != TrangThaiNhiemVu.HoanThanh);
        }

        var tongSo = await truyVan.CountAsync(ct);
        if (tongSo == 0)
        {
            return KetQuaPhanTrang<NhiemVuTomTatDto>.Rong(loc.Trang, loc.KichThuocTrang);
        }

        truyVan = SapXep(truyVan, loc.SapXep, loc.GiamDan);

        var kichThuoc = KetQuaPhanTrang<NhiemVuTomTatDto>.ChuanHoaKichThuocTrang(loc.KichThuocTrang);
        var trang = loc.Trang < 1 ? 1 : loc.Trang;

        var dsThucThe = await truyVan
            .Skip((trang - 1) * kichThuoc)
            .Take(kichThuoc)
            .Select(t => new
            {
                NhiemVu = t,
                TenNguoiTao = t.Creator!.FullName,
                TenNguoiThucHien = t.Assignee != null ? t.Assignee.FullName : null,
                // Lấy phần trăm của bản ghi tiến độ mới nhất. Làm trong cùng một truy vấn
                // để tránh N+1: nếu lấy riêng cho từng dòng thì 20 nhiệm vụ = 21 lần gọi DB.
                TienDo = t.ProgressUpdates
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => (int?)p.ProgressPercent)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var danhSach = dsThucThe
            .Select(x => ChuyenDoiTomTat(x.NhiemVu, x.TenNguoiTao, x.TenNguoiThucHien, x.TienDo))
            .ToList();

        // Thu tu tham so: (danhSach, trangHienTai, kichThuocTrang, tongSoDong)
        return KetQuaPhanTrang<NhiemVuTomTatDto>.Tao(danhSach, trang, kichThuoc, tongSo);
    }

    /// <summary>Chi tiết một nhiệm vụ kèm lịch sử tiến độ, báo cáo và tệp đính kèm.</summary>
    public async Task<KetQua<NhiemVuChiTietDto>> ChiTietAsync(
        long id, long userId, string vaiTro, CancellationToken ct = default)
    {
        var nv = await _db.Tasks.AsNoTracking()
            .Include(t => t.Creator)
            .Include(t => t.Assignee)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (nv is null)
        {
            return KetQua<NhiemVuChiTietDto>.KhongTimThay($"Không tìm thấy nhiệm vụ #{id}.");
        }

        if (!DuocXem(nv, userId, vaiTro))
        {
            return KetQua<NhiemVuChiTietDto>.KhongCoQuyen(
                "Bạn không có quyền xem nhiệm vụ này.");
        }

        var tienDo = await _db.TaskProgresses.AsNoTracking()
            .Where(p => p.TaskId == id)
            .OrderByDescending(p => p.CreatedAt)
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

        var baoCao = await _db.TaskReports.AsNoTracking()
            .Where(r => r.TaskId == id)
            .OrderByDescending(r => r.CreatedAt)
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

        var tep = await _db.TaskAttachments.AsNoTracking()
            .Where(a => a.TaskId == id)
            .OrderByDescending(a => a.UploadedAt)
            .Select(a => new TepDinhKemDto
            {
                Id = a.Id,
                ReportId = a.ReportId,
                FileName = a.FileName,
                FileType = a.FileType,
                FileSize = a.FileSize,
                UploadedBy = a.UploadedBy,
                TenNguoiTaiLen = a.Uploader!.FullName,
                UploadedAt = a.UploadedAt
            })
            .ToListAsync(ct);

        var dto = ChuyenDoiChiTiet(nv, tienDo, baoCao, tep);
        return KetQua<NhiemVuChiTietDto>.Ok(dto);
    }

    // =====================================================================
    // TẠO / SỬA / XÓA
    // =====================================================================

    /// <summary>
    /// Tạo nhiệm vụ. Chỉ MANAGER. Trạng thái khởi tạo do server đặt, client không gửi lên được.
    /// Có <c>AssigneeId</c> thì tạo thẳng ở DA_GIAO, không thì ở MOI_TAO.
    /// </summary>
    public async Task<KetQua<NhiemVuChiTietDto>> TaoAsync(
        TaoNhiemVuRequest yeuCau, long creatorId, CancellationToken ct = default)
    {
        var loi = KiemTraNoiDung(yeuCau.Title, yeuCau.Priority, yeuCau.StartDate, yeuCau.DueDate);
        if (loi is not null) return KetQua<NhiemVuChiTietDto>.DuLieuKhongHopLe(loi);

        var nv = new TaskItem
        {
            Title = yeuCau.Title.Trim(),
            Description = yeuCau.Description?.Trim(),
            Priority = string.IsNullOrWhiteSpace(yeuCau.Priority) ? MucUuTien.TrungBinh : yeuCau.Priority,
            CreatorId = creatorId,
            StartDate = yeuCau.StartDate,
            DueDate = yeuCau.DueDate,
            StatusCode = TrangThaiNhiemVu.MoiTao
        };

        // Giao luôn khi tạo: kiểm người nhận rồi nhảy thẳng sang DA_GIAO.
        if (yeuCau.AssigneeId.HasValue)
        {
            var kiemTra = await KiemTraNguoiThucHienAsync(yeuCau.AssigneeId.Value, ct);
            if (kiemTra is not null) return KetQua<NhiemVuChiTietDto>.DuLieuKhongHopLe(kiemTra);

            nv.AssigneeId = yeuCau.AssigneeId.Value;
            nv.StatusCode = TrangThaiNhiemVu.DaGiao;
        }

        _db.Tasks.Add(nv);
        await _db.SaveChangesAsync(ct);

        _log.LogInformation("Tạo nhiệm vụ #{Id} bởi người dùng {UserId}, trạng thái {TrangThai}",
            nv.Id, creatorId, nv.StatusCode);

        return await ChiTietAsync(nv.Id, creatorId, VaiTro.Manager, ct);
    }

    /// <summary>
    /// Sửa nội dung nhiệm vụ. Chỉ người tạo, và chỉ khi chưa ai bắt đầu làm.
    ///
    /// <para>
    /// Chặn từ DANG_THUC_HIEN trở đi là có chủ đích: người thực hiện đã bỏ công theo
    /// yêu cầu cũ, đổi đề bài giữa chừng mà không báo là sai nghiệp vụ. Muốn đổi thì
    /// phải trao đổi rồi tạo nhiệm vụ mới.
    /// </para>
    /// </summary>
    public async Task<KetQua<NhiemVuChiTietDto>> SuaAsync(
        long id, SuaNhiemVuRequest yeuCau, long userId, CancellationToken ct = default)
    {
        var nv = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (nv is null) return KetQua<NhiemVuChiTietDto>.KhongTimThay($"Không tìm thấy nhiệm vụ #{id}.");

        if (nv.CreatorId != userId)
        {
            return KetQua<NhiemVuChiTietDto>.KhongCoQuyen(
                "Chỉ người tạo nhiệm vụ mới được sửa nhiệm vụ này.");
        }

        if (nv.StatusCode != TrangThaiNhiemVu.MoiTao && nv.StatusCode != TrangThaiNhiemVu.DaGiao)
        {
            return KetQua<NhiemVuChiTietDto>.ThatBai(
                $"Không sửa được nhiệm vụ đang ở trạng thái \"{TrangThaiNhiemVu.TenHienThi(nv.StatusCode)}\". " +
                "Chỉ sửa được khi nhiệm vụ còn ở \"Mới tạo\" hoặc \"Đã giao\".",
                MaLoiChung.ChuyenTrangThaiKhongHopLe);
        }

        var loi = KiemTraNoiDung(yeuCau.Title, yeuCau.Priority, yeuCau.StartDate, yeuCau.DueDate);
        if (loi is not null) return KetQua<NhiemVuChiTietDto>.DuLieuKhongHopLe(loi);

        nv.Title = yeuCau.Title.Trim();
        nv.Description = yeuCau.Description?.Trim();
        if (!string.IsNullOrWhiteSpace(yeuCau.Priority)) nv.Priority = yeuCau.Priority;
        nv.StartDate = yeuCau.StartDate;
        nv.DueDate = yeuCau.DueDate;

        await _db.SaveChangesAsync(ct);
        return await ChiTietAsync(id, userId, VaiTro.Manager, ct);
    }

    /// <summary>
    /// Xóa nhiệm vụ. Chỉ người tạo, chỉ khi còn MOI_TAO và chưa phát sinh dữ liệu con.
    ///
    /// <para>
    /// Kiểm dữ liệu con ở tầng ứng dụng để trả thông báo tiếng Việt dễ hiểu, thay vì
    /// để Oracle ném ORA-02292 vi phạm khóa ngoại.
    /// </para>
    /// </summary>
    public async Task<KetQua> XoaAsync(long id, long userId, CancellationToken ct = default)
    {
        var nv = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (nv is null) return KetQua.KhongTimThay($"Không tìm thấy nhiệm vụ #{id}.");

        if (nv.CreatorId != userId)
        {
            return KetQua.KhongCoQuyen("Chỉ người tạo nhiệm vụ mới được xóa nhiệm vụ này.");
        }

        if (nv.StatusCode != TrangThaiNhiemVu.MoiTao)
        {
            return KetQua.ThatBai(
                $"Chỉ xóa được nhiệm vụ ở trạng thái \"Mới tạo\". " +
                $"Nhiệm vụ này đang ở \"{TrangThaiNhiemVu.TenHienThi(nv.StatusCode)}\".",
                MaLoiChung.ChuyenTrangThaiKhongHopLe);
        }

        if (await _db.TaskProgresses.AnyAsync(p => p.TaskId == id, ct) ||
            await _db.TaskReports.AnyAsync(r => r.TaskId == id, ct) ||
            await _db.TaskAttachments.AnyAsync(a => a.TaskId == id, ct))
        {
            return KetQua.ThatBai(
                "Nhiệm vụ đã có tiến độ, báo cáo hoặc tệp đính kèm nên không xóa được.",
                MaLoiChung.LoiNghiepVu);
        }

        _db.Tasks.Remove(nv);
        await _db.SaveChangesAsync(ct);

        _log.LogInformation("Xóa nhiệm vụ #{Id} bởi người dùng {UserId}", id, userId);
        return KetQua.Ok();
    }

    // =====================================================================
    // CHUYỂN TRẠNG THÁI
    // =====================================================================

    /// <summary>
    /// Giao nhiệm vụ cho một người thực hiện: MOI_TAO -> DA_GIAO.
    /// Cũng dùng để đổi người thực hiện khi nhiệm vụ còn ở DA_GIAO.
    /// </summary>
    public async Task<KetQua<NhiemVuChiTietDto>> GiaoAsync(
        long id, GiaoNhiemVuRequest yeuCau, long userId, CancellationToken ct = default)
    {
        var nv = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (nv is null) return KetQua<NhiemVuChiTietDto>.KhongTimThay($"Không tìm thấy nhiệm vụ #{id}.");

        if (nv.CreatorId != userId)
        {
            return KetQua<NhiemVuChiTietDto>.KhongCoQuyen(
                "Chỉ người tạo nhiệm vụ mới được giao nhiệm vụ này.");
        }

        var loiNguoiNhan = await KiemTraNguoiThucHienAsync(yeuCau.AssigneeId, ct);
        if (loiNguoiNhan is not null)
            return KetQua<NhiemVuChiTietDto>.DuLieuKhongHopLe(loiNguoiNhan);

        // Đổi người khi vẫn còn ở DA_GIAO: không phải chuyển trạng thái nên bỏ qua bảng vòng đời.
        if (nv.StatusCode == TrangThaiNhiemVu.DaGiao)
        {
            nv.AssigneeId = yeuCau.AssigneeId;
            await _db.SaveChangesAsync(ct);
            return await ChiTietAsync(id, userId, VaiTro.Manager, ct);
        }

        if (!TrangThaiNhiemVu.ChuyenDuoc(nv.StatusCode, TrangThaiNhiemVu.DaGiao))
        {
            return KetQua<NhiemVuChiTietDto>.ThatBai(LoiChuyenTrangThai(nv.StatusCode, "giao nhiệm vụ"),
                MaLoiChung.ChuyenTrangThaiKhongHopLe);
        }

        nv.AssigneeId = yeuCau.AssigneeId;
        nv.StatusCode = TrangThaiNhiemVu.DaGiao;
        await _db.SaveChangesAsync(ct);

        _log.LogInformation("Giao nhiệm vụ #{Id} cho người dùng {AssigneeId}", id, yeuCau.AssigneeId);
        return await ChiTietAsync(id, userId, VaiTro.Manager, ct);
    }

    /// <summary>
    /// Người thực hiện tiếp nhận nhiệm vụ: DA_GIAO -> DANG_THUC_HIEN.
    /// Chỉ chính người được giao mới tiếp nhận được.
    /// </summary>
    public async Task<KetQua<NhiemVuChiTietDto>> TiepNhanAsync(
        long id, long userId, CancellationToken ct = default)
    {
        var nv = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (nv is null) return KetQua<NhiemVuChiTietDto>.KhongTimThay($"Không tìm thấy nhiệm vụ #{id}.");

        if (nv.AssigneeId != userId)
        {
            return KetQua<NhiemVuChiTietDto>.KhongCoQuyen(
                "Chỉ người được giao mới tiếp nhận được nhiệm vụ này.");
        }

        if (!TrangThaiNhiemVu.ChuyenDuoc(nv.StatusCode, TrangThaiNhiemVu.DangThucHien))
        {
            return KetQua<NhiemVuChiTietDto>.ThatBai(LoiChuyenTrangThai(nv.StatusCode, "tiếp nhận"),
                MaLoiChung.ChuyenTrangThaiKhongHopLe);
        }

        nv.StatusCode = TrangThaiNhiemVu.DangThucHien;
        await _db.SaveChangesAsync(ct);

        _log.LogInformation("Người dùng {UserId} tiếp nhận nhiệm vụ #{Id}", userId, id);
        return await ChiTietAsync(id, userId, VaiTro.Employee, ct);
    }

    // =====================================================================
    // HỖ TRỢ
    // =====================================================================

    /// <summary>Người tạo, người được giao, hoặc MANAGER thì được xem.</summary>
    private static bool DuocXem(TaskItem nv, long userId, string vaiTro)
        => nv.CreatorId == userId || nv.AssigneeId == userId || vaiTro == VaiTro.Manager;

    /// <summary>Người nhận việc phải là EMPLOYEE đang hoạt động.</summary>
    private async Task<string?> KiemTraNguoiThucHienAsync(long assigneeId, CancellationToken ct)
    {
        var nguoi = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == assigneeId, ct);

        if (nguoi is null) return $"Không tìm thấy người dùng #{assigneeId}.";

        if (nguoi.Role != VaiTro.Employee)
            return $"\"{nguoi.FullName}\" không phải người thực hiện nên không nhận được nhiệm vụ.";

        if (nguoi.Status != TrangThaiNguoiDung.HoatDong)
            return $"Tài khoản \"{nguoi.FullName}\" đã ngừng hoạt động.";

        return null;
    }

    private static string? KiemTraNoiDung(
        string? title, string? priority, DateTime? batDau, DateTime? hanChot)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "Tiêu đề nhiệm vụ không được để trống.";

        // Cột TITLE là VARCHAR2(200 CHAR) — đếm theo ký tự, không phải byte.
        if (title.Trim().Length > 200)
            return "Tiêu đề nhiệm vụ không được quá 200 ký tự.";

        if (!string.IsNullOrWhiteSpace(priority) && !MucUuTien.HopLe(priority))
            return $"Mức ưu tiên \"{priority}\" không hợp lệ. Chỉ nhận LOW, MEDIUM hoặc HIGH.";

        // Trùng với ràng buộc CK_TASKS_DATE trong Oracle. Kiểm ở đây để người dùng nhận
        // thông báo tiếng Việt thay vì ORA-02290.
        if (batDau.HasValue && hanChot.HasValue && hanChot.Value < batDau.Value)
            return "Hạn hoàn thành không được sớm hơn ngày bắt đầu.";

        return null;
    }

    private static string LoiChuyenTrangThai(string trangThaiHienTai, string hanhDong)
    {
        var keTiep = TrangThaiNhiemVu.CacTrangThaiKeTiep(trangThaiHienTai)
            .Select(TrangThaiNhiemVu.TenHienThi)
            .ToArray();

        var goiY = keTiep.Length == 0
            ? "Đây là trạng thái kết thúc, không chuyển tiếp được."
            : $"Từ đây chỉ chuyển được sang: {string.Join(", ", keTiep)}.";

        return $"Không thể {hanhDong} khi nhiệm vụ đang ở trạng thái " +
               $"\"{TrangThaiNhiemVu.TenHienThi(trangThaiHienTai)}\". {goiY}";
    }

    private static IQueryable<TaskItem> SapXep(IQueryable<TaskItem> q, string? theo, bool giamDan)
        => (theo?.ToLowerInvariant()) switch
        {
            "duedate" => giamDan ? q.OrderByDescending(t => t.DueDate) : q.OrderBy(t => t.DueDate),
            "title" => giamDan ? q.OrderByDescending(t => t.Title) : q.OrderBy(t => t.Title),
            "priority" => giamDan ? q.OrderByDescending(t => t.Priority) : q.OrderBy(t => t.Priority),
            "created" => giamDan ? q.OrderByDescending(t => t.CreatedAt) : q.OrderBy(t => t.CreatedAt),
            // Mặc định: mới nhất lên đầu. Thêm Id để thứ tự ổn định khi CreatedAt trùng nhau —
            // không có nó thì phân trang có thể trả trùng hoặc sót dòng.
            _ => q.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id)
        };

    private static NhiemVuTomTatDto ChuyenDoiTomTat(
        TaskItem t, string? tenNguoiTao, string? tenNguoiThucHien, int? tienDo)
    {
        var soNgay = t.DueDate.HasValue
            ? (int?)(t.DueDate.Value.Date - DateTime.Today).TotalDays
            : null;

        return new NhiemVuTomTatDto
        {
            Id = t.Id,
            Title = t.Title,
            Priority = t.Priority,
            TenUuTien = MucUuTien.TenHienThi(t.Priority),
            StatusCode = t.StatusCode,
            TenTrangThai = TrangThaiNhiemVu.TenHienThi(t.StatusCode),
            CreatorId = t.CreatorId,
            TenNguoiTao = tenNguoiTao,
            AssigneeId = t.AssigneeId,
            TenNguoiThucHien = tenNguoiThucHien,
            StartDate = t.StartDate,
            DueDate = t.DueDate,
            SoNgayConLai = soNgay,
            QuaHan = soNgay < 0 && t.StatusCode != TrangThaiNhiemVu.HoanThanh,
            TienDoPhanTram = tienDo,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };
    }

    private static NhiemVuChiTietDto ChuyenDoiChiTiet(
        TaskItem t,
        IReadOnlyList<TienDoDto> tienDo,
        IReadOnlyList<BaoCaoDto> baoCao,
        IReadOnlyList<TepDinhKemDto> tep)
    {
        var tom = ChuyenDoiTomTat(t, t.Creator?.FullName, t.Assignee?.FullName,
            tienDo.Count > 0 ? tienDo[0].ProgressPercent : null);

        return new NhiemVuChiTietDto
        {
            Id = tom.Id,
            Title = tom.Title,
            Description = t.Description,
            Priority = tom.Priority,
            TenUuTien = tom.TenUuTien,
            StatusCode = tom.StatusCode,
            TenTrangThai = tom.TenTrangThai,
            CreatorId = tom.CreatorId,
            TenNguoiTao = tom.TenNguoiTao,
            AssigneeId = tom.AssigneeId,
            TenNguoiThucHien = tom.TenNguoiThucHien,
            StartDate = tom.StartDate,
            DueDate = tom.DueDate,
            SoNgayConLai = tom.SoNgayConLai,
            QuaHan = tom.QuaHan,
            TienDoPhanTram = tom.TienDoPhanTram,
            CreatedAt = tom.CreatedAt,
            UpdatedAt = tom.UpdatedAt,
            TrangThaiKeTiep = TrangThaiNhiemVu.CacTrangThaiKeTiep(t.StatusCode),
            LichSuTienDo = tienDo,
            DanhSachBaoCao = baoCao,
            TepDinhKem = tep
        };
    }
}
