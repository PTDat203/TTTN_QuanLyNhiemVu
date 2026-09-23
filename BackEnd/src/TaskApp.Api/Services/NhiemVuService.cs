using Microsoft.EntityFrameworkCore;
using TaskApp.Api.Common;
using TaskApp.Api.Data;
using TaskApp.Api.Dtos;
using TaskApp.Api.Entities;
using TaskApp.Api.Services.Ai;
using TaskApp.Api.Services.GoiY;

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
    private readonly GoiYService _goiY;
    private readonly ILogger<NhiemVuService> _log;

    public NhiemVuService(TaskDbContext db, GoiYService goiY, ILogger<NhiemVuService> log)
    {
        _db = db;
        _goiY = goiY;
        _log = log;
    }

    // =====================================================================
    // TRA CỨU
    // =====================================================================

    /// <summary>
    /// Danh sách nhiệm vụ có lọc, sắp xếp và phân trang.
    ///
    /// <para>
    /// <b>Phạm vi áp ngay trong truy vấn, không lọc sau khi lấy về:</b> ai cũng thấy việc
    /// mình tạo và việc giao cho mình; trưởng nhóm thấy thêm việc của nhóm, trưởng phòng
    /// thấy việc của phòng, Giám đốc thấy tất cả. Quy tắc nằm ở <see cref="PhamViToChuc"/>.
    /// Lọc ở tầng SQL thì dữ liệu ngoài phạm vi không bao giờ rời khỏi database.
    /// </para>
    /// </summary>
    public async Task<KetQuaPhanTrang<NhiemVuTomTatDto>> DanhSachAsync(
        NhiemVuLocRequest loc, long userId, CancellationToken ct = default)
    {
        // Chỉ việc của chính mình. Nhiệm vụ con người khác giao tiếp xuống KHÔNG hiện ở đây —
        // muốn theo dõi nhánh dưới thì mở việc gốc rồi xem khối "Chuỗi giao việc".
        var truyVan = _db.Tasks.AsNoTracking().ViecCuaToi(userId);

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
                TenPhongBan = t.Department != null ? t.Department.Name : null,
                TenNhom = t.Team != null ? t.Team.Name : null,
                // Lấy phần trăm của bản ghi tiến độ mới nhất. Làm trong cùng một truy vấn
                // để tránh N+1: nếu lấy riêng cho từng dòng thì 20 nhiệm vụ = 21 lần gọi DB.
                TienDo = t.ProgressUpdates
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => (int?)p.ProgressPercent)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var danhSach = dsThucThe
            .Select(x => ChuyenDoiTomTat(
                x.NhiemVu, x.TenNguoiTao, x.TenNguoiThucHien, x.TenPhongBan, x.TenNhom, x.TienDo))
            .ToList();

        // Thu tu tham so: (danhSach, trangHienTai, kichThuocTrang, tongSoDong)
        return KetQuaPhanTrang<NhiemVuTomTatDto>.Tao(danhSach, trang, kichThuoc, tongSo);
    }

    /// <summary>
    /// Chi tiết một nhiệm vụ kèm lịch sử tiến độ, báo cáo và tệp đính kèm.
    /// Chỉ trả về khi nhiệm vụ nằm trong phạm vi được xem của người gọi.
    /// </summary>
    public async Task<KetQua<NhiemVuChiTietDto>> ChiTietAsync(
        long id, long userId, CancellationToken ct = default)
    {
        var (tonTai, duocXem) = await PhamViToChuc.QuyenXemAsync(_db, id, userId, ct);
        if (!tonTai)
            return KetQua<NhiemVuChiTietDto>.KhongTimThay($"Không tìm thấy nhiệm vụ #{id}.");
        if (!duocXem)
            return KetQua<NhiemVuChiTietDto>.KhongCoQuyen("Bạn không có quyền xem nhiệm vụ này.");

        return await DocChiTietAsync(id, ct);
    }

    /// <summary>
    /// Đọc chi tiết mà KHÔNG kiểm quyền xem. Chỉ gọi khi quyền đã được kiểm theo cách
    /// khác — ví dụ ngay sau khi người tạo vừa sửa hay vừa giao nhiệm vụ của chính họ.
    /// </summary>
    private async Task<KetQua<NhiemVuChiTietDto>> DocChiTietAsync(long id, CancellationToken ct)
    {
        var nv = await _db.Tasks.AsNoTracking()
            .Include(t => t.Creator)
            .Include(t => t.Assignee)
            .Include(t => t.Department)
            .Include(t => t.Team)
            .Include(t => t.NguoiDung)
            .Include(t => t.Parent)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (nv is null)
        {
            return KetQua<NhiemVuChiTietDto>.KhongTimThay($"Không tìm thấy nhiệm vụ #{id}.");
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
                QualityScore = r.QualityScore,
                CompletionScore = r.CompletionScore,
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

        // Nhiệm vụ con: người sắp dừng nhiệm vụ cha cần nhìn thấy ai đang chịu ảnh hưởng
        // trước khi bấm, thay vì phải tự đi dò.
        var nhiemVuCon = await _db.Tasks.AsNoTracking()
            .Where(t => t.ParentTaskId == id)
            .OrderBy(t => t.Id)
            .Select(t => new NhiemVuConDto
            {
                Id = t.Id,
                Title = t.Title,
                StatusCode = t.StatusCode,
                AssigneeId = t.AssigneeId,
                TenNguoiThucHien = t.Assignee != null ? t.Assignee.FullName : null
            })
            .ToListAsync(ct);

        foreach (var con in nhiemVuCon)
        {
            con.TenTrangThai = TrangThaiNhiemVu.TenHienThi(con.StatusCode);
        }

        var dto = ChuyenDoiChiTiet(nv, tienDo, baoCao, tep);
        dto.LyDoDung = nv.StopReason;
        dto.TenNguoiDung = nv.NguoiDung?.FullName;
        dto.DungLuc = nv.StoppedAt;
        dto.ParentTaskId = nv.ParentTaskId;
        dto.TieuDeNhiemVuCha = nv.Parent?.Title;
        dto.NhiemVuCon = nhiemVuCon;

        return KetQua<NhiemVuChiTietDto>.Ok(dto);
    }

    // =====================================================================
    // TẠO / SỬA / XÓA
    // =====================================================================

    /// <summary>
    /// Tạo nhiệm vụ. Chỉ cấp quản lý. Trạng thái khởi tạo do server đặt, client không gửi lên được.
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
            var (nguoiNhan, loiGiao) =
                await PhamViToChuc.KiemTraGiaoAsync(_db, creatorId, yeuCau.AssigneeId.Value, ct);
            if (nguoiNhan is null) return KetQua<NhiemVuChiTietDto>.DuLieuKhongHopLe(loiGiao!);

            GanNguoiThucHien(nv, nguoiNhan);
            nv.StatusCode = TrangThaiNhiemVu.DaGiao;
        }
        else
        {
            // Chưa giao ai: để AI đoán sẵn phòng thực thi, nhóm phụ trách và kỹ năng cần có,
            // thay vì bắt người giao tự chọn phòng.
            await GanSuyLuanAiAsync(nv, creatorId, ct);
        }

        _db.Tasks.Add(nv);
        await _db.SaveChangesAsync(ct);

        _log.LogInformation("Tạo nhiệm vụ #{Id} bởi người dùng {UserId}, trạng thái {TrangThai}",
            nv.Id, creatorId, nv.StatusCode);

        return await DocChiTietAsync(nv.Id, ct);
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
        return await DocChiTietAsync(id, ct);
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

        var (nguoiNhan, loiGiao) =
            await PhamViToChuc.KiemTraGiaoAsync(_db, userId, yeuCau.AssigneeId, ct);
        if (nguoiNhan is null)
            return KetQua<NhiemVuChiTietDto>.DuLieuKhongHopLe(loiGiao!);

        // Đổi người khi vẫn còn ở DA_GIAO: không phải chuyển trạng thái nên bỏ qua bảng vòng đời.
        if (nv.StatusCode == TrangThaiNhiemVu.DaGiao)
        {
            GanNguoiThucHien(nv, nguoiNhan);
            await _db.SaveChangesAsync(ct);
            return await DocChiTietAsync(id, ct);
        }

        if (!TrangThaiNhiemVu.ChuyenDuoc(nv.StatusCode, TrangThaiNhiemVu.DaGiao))
        {
            return KetQua<NhiemVuChiTietDto>.ThatBai(LoiChuyenTrangThai(nv.StatusCode, "giao nhiệm vụ"),
                MaLoiChung.ChuyenTrangThaiKhongHopLe);
        }

        GanNguoiThucHien(nv, nguoiNhan);
        nv.StatusCode = TrangThaiNhiemVu.DaGiao;
        await _db.SaveChangesAsync(ct);

        _log.LogInformation("Giao nhiệm vụ #{Id} cho người dùng {AssigneeId}", id, yeuCau.AssigneeId);
        return await DocChiTietAsync(id, ct);
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
        return await DocChiTietAsync(id, ct);
    }

    // =====================================================================
    // HỖ TRỢ
    // =====================================================================

    /// <summary>
    /// Nhờ AI đoán phòng thực thi, nhóm phụ trách và kỹ năng cần có cho nhiệm vụ chưa giao.
    ///
    /// <para>
    /// Chỉ gắn phòng khi AI CHẮC CHẮN và phòng đó nằm trong phạm vi người tạo: trưởng nhóm Backend
    /// tạo việc mà AI đoán thuộc phòng Nhân sự thì không tự đẩy việc sang phòng người khác. Kỹ năng
    /// thì luôn gắn, nguồn AI, để lần gợi ý sau dùng lại và để đo được AI trích đúng tới đâu.
    /// </para>
    /// <para>
    /// Làm theo kiểu "được thì tốt": AI lỗi thì bỏ qua, việc tạo nhiệm vụ không bao giờ hỏng vì AI.
    /// Đến lúc giao việc, phòng và nhóm sẽ được ghi đè theo người nhận thật.
    /// </para>
    /// </summary>
    private async Task GanSuyLuanAiAsync(TaskItem nv, long creatorId, CancellationToken ct)
    {
        try
        {
            var kq = await _goiY.SuyLuanAsync(VanBanHoSo.NhiemVu(nv.Title, nv.Description), ct);
            var s = kq.SuyLuan;

            if (s.KetLuan == KetLuanPhongBan.ChacChan)
            {
                var phong = s.CacPhong[0].DonVi.Id;
                var nguoiTao = await PhamViToChuc.NapAsync(_db, creatorId, ct);
                if (nguoiTao?.VaiTro == VaiTro.GiamDoc || nguoiTao?.DepartmentId == phong)
                {
                    nv.DepartmentId = phong;
                    // Nhóm phải thuộc đúng phòng vừa gắn — database chặn bằng khoá ngoại ghép.
                    if (s.Nhom is { } nhom && nhom.DonVi.DepartmentId == phong) nv.TeamId = nhom.DonVi.Id;
                }
            }

            foreach (var k in s.KyNang)
                nv.KyNangYeuCau.Add(new TaskRequiredSkill { SkillId = k.SkillId, Source = NguonKyNang.AI });

            _log.LogInformation("AI đoán nhiệm vụ mới \"{TieuDe}\": {KetLuan}, phòng {Phong}, {SoKyNang} kỹ năng",
                nv.Title, s.KetLuan, nv.DepartmentId, s.KyNang.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.LogWarning(ex, "Không suy luận được phòng ban cho nhiệm vụ mới \"{TieuDe}\"", nv.Title);
        }
    }

    // ------------------------------------------------------------------ tạm dừng / huỷ

    /// <summary>
    /// Tạm dừng hoặc huỷ một nhiệm vụ kèm lý do.
    ///
    /// <para>
    /// Khác nhau ở chỗ mở lại được hay không: tạm dừng ghi lại trạng thái đang dở vào
    /// <c>PrevStatusCode</c> để quay về đúng chỗ, còn huỷ là kết thúc hẳn.
    /// </para>
    /// <para>
    /// Chỉ người tạo được dừng. Cấp trên muốn dừng thì dừng nhiệm vụ CỦA MÌNH, rồi hệ thống
    /// lan xuống các nhiệm vụ con — đúng đường đi của quyền trong tổ chức, không ai với tay
    /// qua đầu cấp trung gian.
    /// </para>
    /// </summary>
    public async Task<KetQua<NhiemVuChiTietDto>> DungAsync(
        long id, DungNhiemVuRequest yeuCau, long userId, bool huyHan, CancellationToken ct = default)
    {
        var lyDo = (yeuCau.LyDo ?? string.Empty).Trim();
        if (lyDo.Length == 0)
        {
            return KetQua<NhiemVuChiTietDto>.DuLieuKhongHopLe(
                huyHan ? "Phải nêu lý do huỷ nhiệm vụ." : "Phải nêu lý do tạm dừng nhiệm vụ.");
        }

        var nv = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (nv is null) return KetQua<NhiemVuChiTietDto>.KhongTimThay($"Không tìm thấy nhiệm vụ #{id}.");

        if (nv.CreatorId != userId)
        {
            return KetQua<NhiemVuChiTietDto>.KhongCoQuyen(
                "Chỉ người tạo nhiệm vụ mới được tạm dừng hoặc huỷ nhiệm vụ này.");
        }

        if (!TrangThaiNhiemVu.DungDuoc(nv.StatusCode))
        {
            return KetQua<NhiemVuChiTietDto>.ThatBai(
                $"Nhiệm vụ đang ở \"{TrangThaiNhiemVu.TenHienThi(nv.StatusCode)}\" nên không dừng được.",
                MaLoiChung.ChuyenTrangThaiKhongHopLe);
        }

        var luc = DateTime.Now;
        DatTrangThaiDung(nv, huyHan, lyDo, userId, luc);

        // Lan xuống các nhiệm vụ con. Đi theo chiều rộng để không bỏ sót cấp cháu, và giữ
        // một tập đã thăm để dữ liệu lỗi tạo vòng lặp cũng không làm treo vòng lặp này.
        var soCon = 0;
        if (yeuCau.KemNhiemVuCon)
        {
            var daTham = new HashSet<long> { nv.Id };
            var dangXet = new Queue<long>();
            dangXet.Enqueue(nv.Id);

            while (dangXet.Count > 0)
            {
                var chaId = dangXet.Dequeue();
                var cac = await _db.Tasks.Where(t => t.ParentTaskId == chaId).ToListAsync(ct);
                foreach (var con in cac)
                {
                    if (!daTham.Add(con.Id)) continue;
                    dangXet.Enqueue(con.Id);
                    if (!TrangThaiNhiemVu.DungDuoc(con.StatusCode)) continue;

                    DatTrangThaiDung(
                        con, huyHan,
                        $"Nhiệm vụ cấp trên #{chaId} đã {(huyHan ? "huỷ" : "tạm dừng")}: {lyDo}",
                        userId, luc);
                    soCon++;
                }
            }
        }

        await _db.SaveChangesAsync(ct);
        _log.LogInformation(
            "{ThaoTac} nhiệm vụ #{Id} bởi {UserId}, kèm {SoCon} nhiệm vụ con",
            huyHan ? "Huỷ" : "Tạm dừng", id, userId, soCon);

        return await DocChiTietAsync(id, ct);
    }

    /// <summary>Mở lại một nhiệm vụ đang tạm dừng, về đúng trạng thái trước khi dừng.</summary>
    public async Task<KetQua<NhiemVuChiTietDto>> MoLaiAsync(
        long id, long userId, CancellationToken ct = default)
    {
        var nv = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (nv is null) return KetQua<NhiemVuChiTietDto>.KhongTimThay($"Không tìm thấy nhiệm vụ #{id}.");

        if (nv.CreatorId != userId)
        {
            return KetQua<NhiemVuChiTietDto>.KhongCoQuyen(
                "Chỉ người tạo nhiệm vụ mới được mở lại nhiệm vụ này.");
        }

        if (nv.StatusCode == TrangThaiNhiemVu.DaHuy)
        {
            return KetQua<NhiemVuChiTietDto>.ThatBai(
                "Nhiệm vụ đã huỷ hẳn nên không mở lại được. Hãy tạo một nhiệm vụ mới.",
                MaLoiChung.ChuyenTrangThaiKhongHopLe);
        }

        if (nv.StatusCode != TrangThaiNhiemVu.TamDung)
        {
            return KetQua<NhiemVuChiTietDto>.ThatBai(
                "Nhiệm vụ không ở trạng thái tạm dừng.", MaLoiChung.ChuyenTrangThaiKhongHopLe);
        }

        // Nhớ mốc dừng TRƯỚC khi xoá, để nhận ra những nhiệm vụ con đã bị dừng theo cùng
        // thao tác này — chúng mang đúng mốc thời gian đó.
        var mocDung = nv.StoppedAt;

        MoTrangThaiDung(nv);

        // Mở lại theo dây chuyền. Chỉ mở những nhiệm vụ con bị dừng CÙNG LÚC với cha, tức
        // bị dừng vì cha chứ không phải do cấp dưới tự dừng vì lý do riêng — mở nhầm những
        // cái đó là ghi đè quyết định của người khác.
        var soCon = 0;
        if (mocDung is { } moc)
        {
            var daTham = new HashSet<long> { nv.Id };
            var dangXet = new Queue<long>();
            dangXet.Enqueue(nv.Id);

            while (dangXet.Count > 0)
            {
                var chaId = dangXet.Dequeue();
                var cac = await _db.Tasks
                    .Where(t => t.ParentTaskId == chaId
                                && t.StatusCode == TrangThaiNhiemVu.TamDung
                                && t.StoppedAt == moc)
                    .ToListAsync(ct);

                foreach (var con in cac)
                {
                    if (!daTham.Add(con.Id)) continue;
                    dangXet.Enqueue(con.Id);
                    MoTrangThaiDung(con);
                    soCon++;
                }
            }
        }

        await _db.SaveChangesAsync(ct);
        _log.LogInformation(
            "Mở lại nhiệm vụ #{Id} về {TrangThai}, kèm {SoCon} nhiệm vụ con", id, nv.StatusCode, soCon);
        return await DocChiTietAsync(id, ct);
    }

    /// <summary>
    /// Giao tiếp một nhiệm vụ mình đang nhận xuống cấp dưới, tạo ra một nhiệm vụ con.
    ///
    /// <para>
    /// Cần thao tác riêng vì chỉ người tạo mới giao được việc: người nhận không thể giao lại
    /// chính nhiệm vụ đó. Nhiệm vụ con là một nhiệm vụ độc lập, do người này làm chủ, chỉ nối
    /// với nhiệm vụ gốc qua <c>ParentTaskId</c>.
    /// </para>
    /// </summary>
    public async Task<KetQua<NhiemVuChiTietDto>> GiaoTiepXuongAsync(
        long chaId, GiaoTiepXuongRequest yeuCau, long userId, CancellationToken ct = default)
    {
        var cha = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == chaId, ct);
        if (cha is null) return KetQua<NhiemVuChiTietDto>.KhongTimThay($"Không tìm thấy nhiệm vụ #{chaId}.");

        if (cha.AssigneeId != userId)
        {
            return KetQua<NhiemVuChiTietDto>.KhongCoQuyen(
                "Chỉ người đang nhận nhiệm vụ mới được giao tiếp xuống cấp dưới.");
        }

        if (!TrangThaiNhiemVu.DungDuoc(cha.StatusCode))
        {
            return KetQua<NhiemVuChiTietDto>.ThatBai(
                $"Nhiệm vụ cha đang ở \"{TrangThaiNhiemVu.TenHienThi(cha.StatusCode)}\" nên không giao tiếp được.",
                MaLoiChung.ChuyenTrangThaiKhongHopLe);
        }

        var (nguoiNhan, loiGiao) = await PhamViToChuc.KiemTraGiaoAsync(_db, userId, yeuCau.AssigneeId, ct);
        if (nguoiNhan is null) return KetQua<NhiemVuChiTietDto>.DuLieuKhongHopLe(loiGiao!);

        var tieuDe = (yeuCau.Title ?? string.Empty).Trim();
        if (tieuDe.Length == 0)
        {
            return KetQua<NhiemVuChiTietDto>.DuLieuKhongHopLe("Tiêu đề nhiệm vụ không được để trống.");
        }

        var con = new TaskItem
        {
            Title = tieuDe,
            Description = yeuCau.Description,
            CreatorId = userId,
            ParentTaskId = cha.Id,
            Priority = string.IsNullOrWhiteSpace(yeuCau.Priority) ? cha.Priority : yeuCau.Priority!,
            StartDate = yeuCau.StartDate ?? cha.StartDate,
            DueDate = yeuCau.DueDate ?? cha.DueDate,
            StatusCode = TrangThaiNhiemVu.DaGiao,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        GanNguoiThucHien(con, nguoiNhan);

        // Giao tiếp xuống tức là đã nhận việc rồi. Tự tiếp nhận nhiệm vụ cha thay vì bắt người
        // dùng bấm hai nút, rồi quay ra tạo nhiệm vụ mới và quên mất nội dung việc gốc.
        var daTuTiepNhan = false;
        if (cha.StatusCode == TrangThaiNhiemVu.DaGiao)
        {
            cha.StatusCode = TrangThaiNhiemVu.DangThucHien;
            cha.UpdatedAt = DateTime.Now;
            daTuTiepNhan = true;
        }

        _db.Tasks.Add(con);
        await _db.SaveChangesAsync(ct);
        _log.LogInformation(
            "Giao tiếp nhiệm vụ #{ChaId} xuống, tạo nhiệm vụ con #{ConId}{TuNhan}",
            chaId, con.Id, daTuTiepNhan ? " (tự tiếp nhận nhiệm vụ cha)" : "");

        return await DocChiTietAsync(con.Id, ct);
    }

    /// <summary>Gỡ trạng thái dừng, đưa nhiệm vụ về đúng chỗ đang dở. Không lưu xuống database.</summary>
    private static void MoTrangThaiDung(TaskItem nv)
    {
        // Dữ liệu cũ có thể thiếu PrevStatusCode; lúc đó lùi về MOI_TAO cho an toàn.
        nv.StatusCode = nv.PrevStatusCode ?? TrangThaiNhiemVu.MoiTao;
        nv.PrevStatusCode = null;
        nv.StopReason = null;
        nv.StoppedBy = null;
        nv.StoppedAt = null;
        nv.UpdatedAt = DateTime.Now;
    }

    /// <summary>Đặt các trường trạng thái dừng cho một nhiệm vụ. Không lưu xuống database.</summary>
    private static void DatTrangThaiDung(
        TaskItem nv, bool huyHan, string lyDo, long nguoiDungId, DateTime luc)
    {
        // Chỉ tạm dừng mới cần nhớ trạng thái cũ; huỷ là kết thúc nên không mở lại.
        nv.PrevStatusCode = huyHan ? null : nv.StatusCode;
        nv.StatusCode = huyHan ? TrangThaiNhiemVu.DaHuy : TrangThaiNhiemVu.TamDung;
        nv.StopReason = lyDo.Length > 500 ? lyDo[..500] : lyDo;
        nv.StoppedBy = nguoiDungId;
        nv.StoppedAt = luc;
        nv.UpdatedAt = luc;
    }

    /// <summary>
    /// Gắn người thực hiện, đồng thời gắn phòng thực thi và nhóm phụ trách theo người đó.
    ///
    /// <para>
    /// Người làm thuộc phòng nào thì việc thuộc phòng đó — kể cả khi lúc tạo AI đã đoán
    /// phòng khác, vì quyết định giao việc của con người mới là nhãn đúng. Phòng và nhóm lấy
    /// cùng từ một người nên luôn thoả khoá ngoại ghép FK_TASKS_TEAM_DEPT.
    /// </para>
    /// </summary>
    private static void GanNguoiThucHien(TaskItem nv, User nguoiNhan)
    {
        nv.AssigneeId = nguoiNhan.Id;
        nv.DepartmentId = nguoiNhan.DepartmentId;
        nv.TeamId = nguoiNhan.TeamId;
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
        TaskItem t, string? tenNguoiTao, string? tenNguoiThucHien,
        string? tenPhongBan, string? tenNhom, int? tienDo)
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
            DepartmentId = t.DepartmentId,
            TenPhongBan = tenPhongBan,
            TeamId = t.TeamId,
            TenNhom = tenNhom,
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
            t.Department?.Name, t.Team?.Name,
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
            DepartmentId = tom.DepartmentId,
            TenPhongBan = tom.TenPhongBan,
            TeamId = tom.TeamId,
            TenNhom = tom.TenNhom,
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
