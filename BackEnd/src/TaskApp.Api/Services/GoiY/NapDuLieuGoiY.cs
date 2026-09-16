using Microsoft.EntityFrameworkCore;
using TaskApp.Api.Common;
using TaskApp.Api.Data;
using TaskApp.Api.Entities;
using TaskApp.Api.Services.Ai;

namespace TaskApp.Api.Services.GoiY;

/// <summary>
/// Nạp dữ liệu cho bộ xếp hạng, NHÌN TỪ MỘT MỐC THỜI GIAN.
///
/// <para>
/// Gợi ý thật thì mốc là hiện tại. Phần đánh giá thì đặt mốc ở lúc nhiệm vụ cũ được tạo: chấm
/// nhiệm vụ tạo ngày X chỉ được dùng lịch sử có trước ngày X. Không làm vậy thì mô hình "biết
/// trước tương lai" và kết quả đánh giá đẹp một cách giả tạo.
/// </para>
/// <para>
/// Mỗi loại dữ liệu là một câu truy vấn phẳng rồi ghép trong bộ nhớ. Dữ liệu vài trăm dòng nên
/// cách này đơn giản và chắc chắn hơn các truy vấn lồng mà provider Oracle có thể dịch lệch.
/// </para>
/// </summary>
public static class NapDuLieuGoiY
{
    private const int SoKyNangTieuBieu = 8;
    private const int SoChuyenNganhTieuBieu = 5;

    public static async Task<DauVaoXepHang> NapAsync(
        TaskDbContext db, string noiDung, IReadOnlyCollection<long> ungVienIds,
        DateTime? moc, long? boQuaTaskId, IReadOnlyList<KyNangCan>? kyNangCoSan, CancellationToken ct)
    {
        var baoCaoDat = await BaoCaoDatAsync(db, ct);
        var lichSu = await LichSuAsync(db, baoCaoDat, moc, boQuaTaskId, ct);
        var ungVien = await UngVienAsync(db, ungVienIds.ToList(), lichSu, baoCaoDat, moc, boQuaTaskId, ct);
        var (phong, nhom) = await DonViAsync(db, ct);
        var danhMuc = await DanhMucKyNangAsync(db, ct);

        return new DauVaoXepHang
        {
            NoiDung = noiDung,
            UngVien = ungVien,
            PhongBan = phong,
            Nhom = nhom,
            DanhMucKyNang = danhMuc,
            LichSu = lichSu,
            KyNangCoSan = kyNangCoSan
        };
    }

    /// <summary>Kỹ năng yêu cầu đã lưu cho nhiệm vụ: ưu tiên nguồn MANUAL, không có mới lấy nguồn AI.</summary>
    public static async Task<List<KyNangCan>> KyNangDaLuuAsync(TaskDbContext db, long taskId, CancellationToken ct)
    {
        var ds = await db.TaskRequiredSkills.AsNoTracking()
            .Where(r => r.TaskId == taskId)
            .Select(r => new { r.SkillId, r.RequiredLevel, r.Source, r.Skill!.Code, r.Skill.Name })
            .ToListAsync(ct);

        var nguon = ds.Any(r => r.Source == NguonKyNang.ThuCong) ? NguonKyNang.ThuCong : NguonKyNang.AI;

        return ds.Where(r => r.Source == nguon)
            .Select(r => new KyNangCan
            {
                SkillId = r.SkillId, Code = r.Code, Ten = r.Name, Muc = r.RequiredLevel, Nguon = r.Source
            })
            .ToList();
    }

    // =====================================================================

    private sealed record BaoCaoDat(DateTime NgayDuyet, int? ChatLuong, int? MucHoanThanh);

    /// <summary>Lần duyệt ĐẠT gần nhất của từng nhiệm vụ.</summary>
    private static async Task<Dictionary<long, BaoCaoDat>> BaoCaoDatAsync(TaskDbContext db, CancellationToken ct)
    {
        var ds = await db.TaskReports.AsNoTracking()
            .Where(r => r.Status == TrangThaiBaoCao.DaXacNhan && r.ReviewedAt != null)
            .Select(r => new { r.TaskId, NgayDuyet = r.ReviewedAt!.Value, r.QualityScore, r.CompletionScore })
            .ToListAsync(ct);

        return ds.GroupBy(r => r.TaskId).ToDictionary(
            g => g.Key,
            g => g.OrderByDescending(r => r.NgayDuyet)
                  .Select(r => new BaoCaoDat(r.NgayDuyet, r.QualityScore, r.CompletionScore))
                  .First());
    }

    /// <summary>Mọi nhiệm vụ đã được duyệt đạt trước mốc.</summary>
    private static async Task<List<ViecLichSu>> LichSuAsync(
        TaskDbContext db, Dictionary<long, BaoCaoDat> baoCaoDat, DateTime? moc, long? boQuaTaskId, CancellationToken ct)
    {
        var viec = await db.Tasks.AsNoTracking()
            .Where(t => t.StatusCode == TrangThaiNhiemVu.HoanThanh && t.AssigneeId != null)
            .Select(t => new
            {
                t.Id, t.Title, t.Description, AssigneeId = t.AssigneeId!.Value,
                t.DepartmentId, t.TeamId, t.DueDate
            })
            .ToListAsync(ct);

        return viec
            .Where(t => t.Id != boQuaTaskId
                        && baoCaoDat.TryGetValue(t.Id, out var bc)
                        && (moc is null || bc.NgayDuyet < moc))
            .Select(t =>
            {
                var bc = baoCaoDat[t.Id];
                return new ViecLichSu
                {
                    TaskId = t.Id,
                    TieuDe = t.Title,
                    VanBan = VanBanHoSo.NhiemVu(t.Title, t.Description),
                    AssigneeId = t.AssigneeId,
                    DepartmentId = t.DepartmentId,
                    TeamId = t.TeamId,
                    // Tính theo NGÀY DUYỆT ĐẠT chứ không phải ngày nộp: nộp kịp mà phải làm lại mãi
                    // mới đạt thì không thể tính là đúng hạn.
                    DungHan = t.DueDate is null || bc.NgayDuyet.Date <= t.DueDate.Value.Date,
                    ChatLuong = bc.ChatLuong,
                    MucHoanThanh = bc.MucHoanThanh
                };
            })
            .ToList();
    }

    private static async Task<List<HoSoUngVien>> UngVienAsync(
        TaskDbContext db, List<long> ids, List<ViecLichSu> lichSu, Dictionary<long, BaoCaoDat> baoCaoDat,
        DateTime? moc, long? boQuaTaskId, CancellationToken ct)
    {
        if (ids.Count == 0) return new List<HoSoUngVien>();

        var nguoi = await db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new
            {
                u.Id, u.FullName, u.Role, u.JobTitle, u.DepartmentId,
                TenPhong = u.Department != null ? u.Department.Name : null,
                u.TeamId,
                TenNhom = u.Team != null ? u.Team.Name : null,
                u.HiredDate
            })
            .ToListAsync(ct);

        var kyNang = await db.UserSkills.AsNoTracking()
            .Where(s => ids.Contains(s.UserId))
            .Select(s => new
            {
                s.UserId, s.SkillId, s.SkillLevel, s.YearsExperience, s.Description, s.SkillName,
                TenChuan = s.Skill != null ? s.Skill.Name : null
            })
            .ToListAsync(ct);

        var hocVan = await db.UserQualifications.AsNoTracking()
            .Where(q => ids.Contains(q.UserId))
            .Select(q => new { q.UserId, q.DegreeName, q.Major })
            .ToListAsync(ct);

        var tai = await KhoiLuongAsync(db, ids, baoCaoDat, moc, boQuaTaskId, ct);
        var lichSuTheoNguoi = lichSu.ToLookup(v => v.AssigneeId);

        // Thâm niên phải tính tại MỐC, không phải tại hôm nay — nếu không, khi đánh giá lại lịch
        // sử thì người nào cũng "đã làm nhiều năm", tức là mô hình biết trước tương lai.
        var mocThamNien = moc ?? DateTime.Now;

        return nguoi.Select(u =>
        {
            var kn = kyNang.Where(s => s.UserId == u.Id).ToList();
            var (soDangLam, taiHienTai) = tai.GetValueOrDefault(u.Id);

            return new HoSoUngVien
            {
                UserId = u.Id,
                FullName = u.FullName,
                VaiTro = u.Role,
                ChucDanh = u.JobTitle,
                DepartmentId = u.DepartmentId,
                TenPhong = u.TenPhong,
                TeamId = u.TeamId,
                TenNhom = u.TenNhom,
                VanBanHoSo = VanBanHoSo.NguoiThucHien(
                    u.JobTitle, u.TenPhong, u.TenNhom,
                    kn.Select(s => (s.TenChuan ?? s.SkillName, s.SkillLevel ?? 1, s.YearsExperience, s.Description)),
                    hocVan.Where(h => h.UserId == u.Id).Select(h => (h.DegreeName, h.Major))),
                MucKyNang = kn.Where(s => s.SkillId != null)
                    .GroupBy(s => s.SkillId!.Value)
                    .ToDictionary(g => g.Key, g => g.Max(s => s.SkillLevel ?? 1)),
                ViecDaXong = lichSuTheoNguoi[u.Id].ToList(),
                SoNamLamViec = u.HiredDate is { } ngayVao
                    ? Math.Max(0, (mocThamNien - ngayVao).TotalDays / 365.25)
                    : 0,
                SoDangLam = soDangLam,
                TaiHienTai = taiHienTai
            };
        }).ToList();
    }

    /// <summary>
    /// Việc đang gánh của từng người tại mốc: số việc và tổng trọng số ưu tiên.
    ///
    /// <para>
    /// Mốc là hiện tại thì đọc thẳng trạng thái. Mốc trong quá khứ thì tính "đã được giao trước
    /// mốc và chưa được duyệt đạt trước mốc" — xấp xỉ, vì database không lưu ngày giao riêng mà
    /// lấy ngày tạo thay thế.
    /// </para>
    /// </summary>
    private static async Task<Dictionary<long, (int SoViec, double Tai)>> KhoiLuongAsync(
        TaskDbContext db, List<long> ids, Dictionary<long, BaoCaoDat> baoCaoDat,
        DateTime? moc, long? boQuaTaskId, CancellationToken ct)
    {
        var q = db.Tasks.AsNoTracking().Where(t => t.AssigneeId != null && ids.Contains(t.AssigneeId.Value));
        if (boQuaTaskId is { } boQua) q = q.Where(t => t.Id != boQua);

        List<(long UserId, string Priority)> dangMo;
        if (moc is null)
        {
            dangMo = (await q.Where(t => TrangThaiNhiemVu.DangXuLy.Contains(t.StatusCode))
                    .Select(t => new { UserId = t.AssigneeId!.Value, t.Priority })
                    .ToListAsync(ct))
                .Select(x => (x.UserId, x.Priority)).ToList();
        }
        else
        {
            var mocGiaTri = moc.Value;
            dangMo = (await q.Where(t => t.CreatedAt < mocGiaTri)
                    .Select(t => new { t.Id, UserId = t.AssigneeId!.Value, t.Priority })
                    .ToListAsync(ct))
                .Where(t => !(baoCaoDat.TryGetValue(t.Id, out var bc) && bc.NgayDuyet < mocGiaTri))
                .Select(x => (x.UserId, x.Priority)).ToList();
        }

        // Tính theo trọng số ưu tiên: một việc HIGH nặng gấp ba một việc LOW, đếm đầu việc suông
        // sẽ đánh giá sai mức bận thật sự.
        return dangMo.GroupBy(x => x.UserId).ToDictionary(
            g => g.Key,
            g => (g.Count(), g.Sum(x => (double)Math.Max(1, MucUuTien.TrongSo(x.Priority)))));
    }

    /// <summary>
    /// Các phòng có người nhận việc được, và các nhóm — dựng thành đoạn văn để so với nhiệm vụ.
    /// Phòng chỉ có Giám đốc thì bỏ: không ai ở đó nhận việc, đoán ra phòng đó cũng vô ích.
    /// </summary>
    private static async Task<(List<HoSoDonVi> Phong, List<HoSoDonVi> Nhom)> DonViAsync(
        TaskDbContext db, CancellationToken ct)
    {
        var phong = await db.Departments.AsNoTracking()
            .Select(d => new
            {
                d.Id, d.Name, d.Description,
                CoNguoiNhan = d.ThanhVien.Any(u => u.Status == TrangThaiNguoiDung.HoatDong && u.Role != VaiTro.GiamDoc)
            })
            .ToListAsync(ct);

        var nhom = await db.Teams.AsNoTracking()
            .Select(t => new { t.Id, t.DepartmentId, t.Name, t.Description, TenPhong = t.Department!.Name })
            .ToListAsync(ct);

        var kyNang = await db.UserSkills.AsNoTracking()
            .Where(s => s.SkillId != null && s.User!.Status == TrangThaiNguoiDung.HoatDong)
            .Select(s => new { s.User!.DepartmentId, s.User.TeamId, Ten = s.Skill!.Name })
            .ToListAsync(ct);

        var chuyenNganh = await db.UserQualifications.AsNoTracking()
            .Where(q => q.User!.Status == TrangThaiNguoiDung.HoatDong)
            .Select(q => new { q.User!.DepartmentId, q.Major })
            .ToListAsync(ct);

        static IEnumerable<string> TieuBieu(IEnumerable<string> ds, int soLuong) => ds
            .GroupBy(x => x)
            .OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
            .Take(soLuong)
            .Select(g => g.Key);

        var dsNhom = nhom.Select(n => new HoSoDonVi
        {
            Id = n.Id,
            DepartmentId = n.DepartmentId,
            Ten = n.Name,
            VanBan = VanBanHoSo.Nhom(n.Name, n.TenPhong, n.Description,
                TieuBieu(kyNang.Where(k => k.TeamId == n.Id).Select(k => k.Ten), SoKyNangTieuBieu))
        }).ToList();

        var dsPhong = phong.Where(p => p.CoNguoiNhan).Select(p => new HoSoDonVi
        {
            Id = p.Id,
            Ten = p.Name,
            VanBan = VanBanHoSo.PhongBan(p.Name, p.Description,
                nhom.Where(n => n.DepartmentId == p.Id).Select(n => (n.Name, n.Description)),
                TieuBieu(kyNang.Where(k => k.DepartmentId == p.Id).Select(k => k.Ten), SoKyNangTieuBieu),
                chuyenNganh.Where(c => c.DepartmentId == p.Id).Select(c => c.Major)
                    .Distinct().Take(SoChuyenNganhTieuBieu))
        }).ToList();

        return (dsPhong, dsNhom);
    }

    private static async Task<List<KyNangDanhMuc>> DanhMucKyNangAsync(TaskDbContext db, CancellationToken ct)
    {
        var ds = await db.Skills.AsNoTracking()
            .OrderBy(s => s.Id)
            .Select(s => new { s.Id, s.Code, s.Name, s.Description })
            .ToListAsync(ct);

        return ds.Select(s => new KyNangDanhMuc
        {
            Id = s.Id, Code = s.Code, Ten = s.Name, VanBan = VanBanHoSo.KyNang(s.Name, s.Description)
        }).ToList();
    }
}
