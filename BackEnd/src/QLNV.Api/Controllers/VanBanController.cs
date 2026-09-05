using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNV.Api.Common;
using QLNV.Api.Services;
using QLNV.Core.Abstractions;
using QLNV.Core.Common;
using QLNV.Core.Constants;
using QLNV.Core.Dtos;
using QLNV.Core.Entities;
using MaVaiTro = QLNV.Core.Constants.VaiTro;

namespace QLNV.Api.Controllers;

/// <summary>
/// §5.2 B1-B5 — Van ban chi dao (tang 1 cua mo hinh 2 tang, §1.1).
///
/// KIEM QUYEN 2 LOP (§6.4):
///   LOP 1: xem (B1/B2) — moi vai deu duoc goi; tao/sua/xoa (B3/B4/B5) — chi ben giao.
///   LOP 2: §6.2 dong 1 (nguoi thuc hien chi thay van ban co nhiem vu giao cho minh),
///          dong 2 (sua: chi nguoi tao + chua co nhiem vu nao khac trang thai 3),
///          dong 3 (xoa: chi nguoi tao + chua co nhiem vu con).
/// </summary>
[Route("api/v1/van-ban")]
public sealed class VanBanController : QlnvControllerBase
{
    private readonly TroGiupNghiepVu _troGiup;
    private readonly IFileStorage _kho;

    public VanBanController(IHienTai hienTai, TroGiupNghiepVu troGiup, IFileStorage kho)
        : base(hienTai)
    {
        _troGiup = troGiup;
        _kho = kho;
    }

    private DbContext Db => _troGiup.Db;

    // =================================================================
    // B1 — GET /api/v1/van-ban
    // =================================================================

    /// <summary>§5.2 B1 — danh sach van ban chi dao, phan trang + loc (M03).</summary>
    [HttpGet]
    public async Task<IActionResult> DanhSach([FromQuery] VanBanLocRequest req, CancellationToken ct)
    {
        var chan = BatBuocDangNhap();
        if (chan is not null) return chan;

        var loi = await KiemTraAsync(req, ct);
        if (loi is not null) return loi;

        var nguoi = await _troGiup.TimNguoiDungAsync(UserId, ct);
        if (nguoi is null || !nguoi.DangHoatDong) return Loi403("Tài khoản của bạn không tồn tại hoặc đã bị khoá.");

        var (trang, kichThuoc) = ChuanHoaTrang(req.Page, req.Size);

        var q = Db.Set<DmVanBan>().AsNoTracking().AsQueryable();

        // §6.2 dong 1 — LOP 2: nguoi thuc hien CHI thay van ban co nhiem vu giao cho minh.
        if (!MaVaiTro.LaBenGiao(nguoi.VaiTro))
        {
            var idVbCuaToi = Db.Set<NhiemVuPhanCong>()
                .Where(p => p.UserId == nguoi.Id && p.TrangThai == TrangThaiPhanCong.ConHieuLuc)
                .Join(Db.Set<DmNhiemVuChiTiet>(), p => p.IdNvChiTiet, n => n.Id, (p, n) => n.IdVb);
            q = q.Where(v => idVbCuaToi.Contains(v.Id));
        }

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var tu = req.Search.Trim();
            q = q.Where(v => (v.SoKyHieu != null && v.SoKyHieu.Contains(tu)) || v.TrichYeu.Contains(tu));
        }

        if (!string.IsNullOrWhiteSpace(req.LinhVuc)) q = q.Where(v => v.LinhVuc == req.LinhVuc);
        if (req.TuNgay.HasValue) q = q.Where(v => v.NgayBanHanh != null && v.NgayBanHanh >= req.TuNgay.Value);
        if (req.DenNgay.HasValue) q = q.Where(v => v.NgayBanHanh != null && v.NgayBanHanh <= req.DenNgay.Value);

        var tongSo = await q.CountAsync(ct);
        var ds = await q.OrderByDescending(v => v.CreateDate)
            .Skip((trang - 1) * kichThuoc)
            .Take(kichThuoc)
            .ToListAsync(ct);

        var items = await SangDtoAsync(ds, nguoi, kemFile: false, ct);
        return Ok(new PagedResult<VanBanDto>(items, tongSo, trang, kichThuoc));
    }

    // =================================================================
    // B2 — GET /api/v1/van-ban/{id}
    // =================================================================

    /// <summary>§5.2 B2 — chi tiet van ban + tep dinh kem.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ChiTiet(Guid id, CancellationToken ct)
    {
        var chan = BatBuocDangNhap();
        if (chan is not null) return chan;

        var nguoi = await _troGiup.TimNguoiDungAsync(UserId, ct);
        if (nguoi is null || !nguoi.DangHoatDong) return Loi403("Tài khoản của bạn không tồn tại hoặc đã bị khoá.");

        var vb = await Db.Set<DmVanBan>().AsNoTracking().FirstOrDefaultAsync(v => v.Id == id, ct);
        if (vb is null) return Loi404("Không tìm thấy văn bản chỉ đạo.");

        // §6.4 LOP 2 — kiem quyen xem theo du lieu.
        if (!await _troGiup.ChoPhepXemVanBanAsync(id, nguoi, ct))
        {
            return Loi403("Bạn không có quyền xem văn bản chỉ đạo này.");
        }

        var ds = await SangDtoAsync(new List<DmVanBan> { vb }, nguoi, kemFile: true, ct);
        return Ok(ds[0]);
    }

    // =================================================================
    // B3 — POST /api/v1/van-ban
    // =================================================================

    /// <summary>§5.2 B3 — tao van ban chi dao moi (M04). §6.2 dong 2: chi ben giao.</summary>
    [HttpPost]
    public async Task<IActionResult> Tao([FromBody] LuuVanBanRequest req, CancellationToken ct)
    {
        // §6.4 LOP 1
        var chan = BatBuocBenGiao("tạo văn bản chỉ đạo");
        if (chan is not null) return chan;

        var loi = await KiemTraAsync(req, ct);
        if (loi is not null) return loi;

        var nguoi = await _troGiup.TimNguoiDungAsync(UserId, ct);
        if (nguoi is null || !nguoi.DangHoatDong) return Loi403("Tài khoản của bạn không tồn tại hoặc đã bị khoá.");

        var bayGio = NgayUtil.BayGio();
        var vb = new DmVanBan
        {
            Id = Guid.NewGuid(),
            SoKyHieu = Chuan(req.SoKyHieu),
            TrichYeu = req.TrichYeu.Trim(),
            LoaiVb = Chuan(req.LoaiVb),
            NgayBanHanh = req.NgayBanHanh,
            CoQuanBanHanh = Chuan(req.CoQuanBanHanh),
            DoKhan = req.DoKhan,
            LinhVuc = Chuan(req.LinhVuc),
            ThoiGianChiDao = req.ThoiGianChiDao ?? bayGio, // §4.1 — mac dinh = ngay tao
            NguonNv = Chuan(req.NguonNv),
            NguoiTheoDoi = Chuan(req.NguoiTheoDoi),
            UnitCode = nguoi.UnitCode,
            UserIdCreate = nguoi.Id,
            CreateDate = bayGio
        };

        Db.Set<DmVanBan>().Add(vb);

        var ganFile = await _troGiup.GanFileAsync(req.FileIds, vb.Id, LoaiBanGhiFile.VanBan, nguoi.Id, ct);
        var loiFile = NeuLoi(ganFile);
        if (loiFile is not null) return loiFile;

        await Db.SaveChangesAsync(ct);

        var ds = await SangDtoAsync(new List<DmVanBan> { vb }, nguoi, kemFile: true, ct);
        return CreatedAtAction(nameof(ChiTiet), new { id = vb.Id }, ds[0]);
    }

    // =================================================================
    // B4 — PUT /api/v1/van-ban/{id}
    // =================================================================

    /// <summary>
    /// §5.2 B4 — cap nhat van ban chi dao.
    /// §6.2 dong 2 — LOP 2: chi nguoi tao, va van ban CHUA co nhiem vu nao o trang thai khac 3.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Sua(Guid id, [FromBody] LuuVanBanRequest req, CancellationToken ct)
    {
        var chan = BatBuocBenGiao("sửa văn bản chỉ đạo");
        if (chan is not null) return chan;

        var loi = await KiemTraAsync(req, ct);
        if (loi is not null) return loi;

        var nguoi = await _troGiup.TimNguoiDungAsync(UserId, ct);
        if (nguoi is null || !nguoi.DangHoatDong) return Loi403("Tài khoản của bạn không tồn tại hoặc đã bị khoá.");

        var vb = await Db.Set<DmVanBan>().FirstOrDefaultAsync(v => v.Id == id, ct);
        if (vb is null) return Loi404("Không tìm thấy văn bản chỉ đạo.");

        // §6.4 LOP 2
        if (!await _troGiup.ChoPhepSuaVanBanAsync(vb, nguoi, ct))
        {
            return Loi403("Bạn không được sửa văn bản này: chỉ người tạo mới được sửa, " +
                          "và văn bản không được có nhiệm vụ nào đã triển khai.");
        }

        vb.SoKyHieu = Chuan(req.SoKyHieu);
        vb.TrichYeu = req.TrichYeu.Trim();
        vb.LoaiVb = Chuan(req.LoaiVb);
        vb.NgayBanHanh = req.NgayBanHanh;
        vb.CoQuanBanHanh = Chuan(req.CoQuanBanHanh);
        vb.DoKhan = req.DoKhan;
        vb.LinhVuc = Chuan(req.LinhVuc);
        vb.ThoiGianChiDao = req.ThoiGianChiDao ?? vb.ThoiGianChiDao;
        vb.NguonNv = Chuan(req.NguonNv);
        vb.NguoiTheoDoi = Chuan(req.NguoiTheoDoi);
        vb.UpdateDate = NgayUtil.BayGio();

        var ganFile = await _troGiup.GanFileAsync(req.FileIds, vb.Id, LoaiBanGhiFile.VanBan, nguoi.Id, ct);
        var loiFile = NeuLoi(ganFile);
        if (loiFile is not null) return loiFile;

        await Db.SaveChangesAsync(ct);

        var ds = await SangDtoAsync(new List<DmVanBan> { vb }, nguoi, kemFile: true, ct);
        return Ok(ds[0]);
    }

    // =================================================================
    // B5 — DELETE /api/v1/van-ban/{id}
    // =================================================================

    /// <summary>§5.2 B5 — xoa van ban chi dao. CHI khi chua co nhiem vu con (§6.2 dong 3).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Xoa(Guid id, CancellationToken ct)
    {
        var chan = BatBuocBenGiao("xoá văn bản chỉ đạo");
        if (chan is not null) return chan;

        var nguoi = await _troGiup.TimNguoiDungAsync(UserId, ct);
        if (nguoi is null || !nguoi.DangHoatDong) return Loi403("Tài khoản của bạn không tồn tại hoặc đã bị khoá.");

        var vb = await Db.Set<DmVanBan>().FirstOrDefaultAsync(v => v.Id == id, ct);
        if (vb is null) return Loi404("Không tìm thấy văn bản chỉ đạo.");

        // Tach thong bao: chua co quyen so huu -> 403; da co nhiem vu con -> 409.
        var laQuanTri = nguoi.VaiTro == MaVaiTro.QuanTri;
        if (!laQuanTri && vb.UserIdCreate != nguoi.Id)
        {
            return Loi403("Chỉ người tạo văn bản chỉ đạo mới được xoá văn bản này.");
        }

        var coNvCon = await Db.Set<DmNhiemVuChiTiet>().AnyAsync(n => n.IdVb == id, ct);
        if (coNvCon)
        {
            return Loi409("Không thể xoá: văn bản chỉ đạo đã có nhiệm vụ con. " +
                          "Hãy thu hồi hoặc xoá các nhiệm vụ trước.", MaLoiChung.ViPhamRangBuoc);
        }

        // Xoa ban ghi tep dinh kem cua van ban (ca ban ghi CSDL lan tep vat ly).
        var files = await Db.Set<NhiemVuFile>()
            .Where(f => f.LoaiBanGhi == LoaiBanGhiFile.VanBan && f.RecordId == id)
            .ToListAsync(ct);

        foreach (var f in files)
        {
            // Loi xoa tep vat ly KHONG duoc chan viec xoa ban ghi nghiep vu:
            // ket qua duoc bo qua co y (kho tep se don rac o job rieng).
            await _kho.XoaAsync(f.FilePath, ct);
        }

        Db.Set<NhiemVuFile>().RemoveRange(files);
        Db.Set<DmVanBan>().Remove(vb);
        await Db.SaveChangesAsync(ct);

        return NoContent();
    }

    // =================================================================
    // Ho tro
    // =================================================================

    private static string? Chuan(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    /// <summary>Anh xa danh sach van ban sang DTO kem 2 co quyen §6.2 dong 2/3.</summary>
    private async Task<List<VanBanDto>> SangDtoAsync(
        IReadOnlyList<DmVanBan> ds, SysUser nguoi, bool kemFile, CancellationToken ct)
    {
        var kq = new List<VanBanDto>();
        if (ds.Count == 0) return kq;

        var ids = ds.Select(v => v.Id).Distinct().ToList();

        // Dem nhiem vu con + phat hien nhiem vu da roi trang thai 3 (dieu kien sua, §6.2 dong 2).
        var thongKe = await Db.Set<DmNhiemVuChiTiet>()
            .Where(n => ids.Contains(n.IdVb))
            .GroupBy(n => n.IdVb)
            .Select(g => new
            {
                IdVb = g.Key,
                TongSo = g.Count(),
                // Dung SUM(CASE...) thay cho Count(predicate) de chac chan dich duoc sang SQL.
                SoDaChay = g.Sum(x => x.TrangThai != TrangThaiNv.ChuaTrienKhai ? 1 : 0)
            })
            .ToListAsync(ct);
        var mapThongKe = thongKe.ToDictionary(x => x.IdVb);

        var idNguoiTao = ds.Select(v => v.UserIdCreate).Where(x => x != Guid.Empty).Distinct().ToList();
        var nguoiTao = await Db.Set<SysUser>()
            .Where(u => idNguoiTao.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToListAsync(ct);
        var mapNguoiTao = nguoiTao.ToDictionary(u => u.Id, u => u.FullName);

        var mapFile = kemFile
            ? await _troGiup.DocFileAsync(LoaiBanGhiFile.VanBan, ids, ct)
            : new Dictionary<Guid, List<FileDto>>();

        var laQuanTri = nguoi.VaiTro == MaVaiTro.QuanTri;
        var laBenGiao = MaVaiTro.LaBenGiao(nguoi.VaiTro);

        foreach (var v in ds)
        {
            var tongSo = 0;
            var soDaChay = 0;
            if (mapThongKe.TryGetValue(v.Id, out var tk))
            {
                tongSo = tk.TongSo;
                soDaChay = tk.SoDaChay;
            }

            var laChuSoHuu = laQuanTri || v.UserIdCreate == nguoi.Id;

            var dto = new VanBanDto
            {
                Id = v.Id,
                SoKyHieu = v.SoKyHieu,
                TrichYeu = v.TrichYeu,
                LoaiVb = v.LoaiVb,
                NgayBanHanh = v.NgayBanHanh,
                CoQuanBanHanh = v.CoQuanBanHanh,
                DoKhan = v.DoKhan,
                LinhVuc = v.LinhVuc,
                ThoiGianChiDao = v.ThoiGianChiDao,
                NguonNv = v.NguonNv,
                NguoiTheoDoi = v.NguoiTheoDoi,
                UnitCode = v.UnitCode,
                UserIdCreate = v.UserIdCreate,
                CreateDate = v.CreateDate,
                TongSoNhiemVu = tongSo,
                // §6.2 dong 2 — sua: ben giao + so huu + chua co nhiem vu nao khac trang thai 3.
                ChoPhepSua = laBenGiao && laChuSoHuu && soDaChay == 0,
                // §6.2 dong 3 — xoa: ben giao + so huu + chua co nhiem vu con.
                ChoPhepXoa = laBenGiao && laChuSoHuu && tongSo == 0
            };

            if (mapNguoiTao.TryGetValue(v.UserIdCreate, out var ten)) dto.NguoiTaoTen = ten;
            if (mapFile.TryGetValue(v.Id, out var fs)) dto.Files = fs;

            kq.Add(dto);
        }

        return kq;
    }
}
