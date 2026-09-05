using Microsoft.EntityFrameworkCore;
using QLNV.Core.Abstractions;
using QLNV.Core.Common;
using QLNV.Core.Constants;
using QLNV.Core.Dtos;
using QLNV.Core.Entities;
using QLNV.Core.Services;
using MaVaiTro = QLNV.Core.Constants.VaiTro;

namespace QLNV.Api.Services;

/// <summary>
/// Ket qua nap mot nhiem vu kem ngu canh va bang quyen §6.2.
/// </summary>
/// <param name="NhiemVu">Ban ghi nhiem vu DANG DUOC THEO DOI boi DbContext (sua roi SaveChanges la du).</param>
/// <param name="NguoiThaoTac">Nguoi dang dang nhap.</param>
/// <param name="Ctx">Ngu canh day du de goi <see cref="INhiemVuStateMachine"/>.</param>
/// <param name="Quyen">15 co quyen da tinh (LOP 2 cua §6.4).</param>
public sealed record NhiemVuDaNap(
    DmNhiemVuChiTiet NhiemVu,
    SysUser NguoiThaoTac,
    NguCanh Ctx,
    QuyenNhiemVu Quyen);

/// <summary>
/// Dich vu tro giup dung chung cho cac controller nghiep vu: nap ngu canh, ghi ket xuat
/// cua may trang thai, gan/doc tep dinh kem, anh xa entity sang DTO.
///
/// LUU Y KIEN TRUC (quan trong khi rap voi Program.cs):
/// lop nay nhan <see cref="DbContext"/> KIEU CO SO chu khong nhan kieu cu the cua
/// QLNV.Infrastructure. Ly do: controller duoc viet TRUOC khi tang Infrastructure ton tai,
/// nen khong duoc phep phu thuoc ten lop DbContext cu the. Program.cs chi can them 2 dong:
///   builder.Services.AddScoped[DbContext](sp =&gt; sp.GetRequiredService[QlnvDbContext]());
///   builder.Services.AddScoped[TroGiupNghiepVu]();
/// Moi truy van deu dung Set-of-T nen khong phu thuoc ten thuoc tinh DbSet.
/// Cung KHONG dung Include() — quan he duoc nap bang truy van rieng roi ghep trong bo nho,
/// de khong phu thuoc cach cau hinh navigation cua tang Infrastructure.
/// </summary>
public sealed class TroGiupNghiepVu
{
    private readonly DbContext _db;
    private readonly IQuyenService _quyenService;

    public TroGiupNghiepVu(DbContext db, IQuyenService quyenService)
    {
        _db = db;
        _quyenService = quyenService;
    }

    /// <summary>Truy cap thang DbContext cho cac truy van rieng cua controller.</summary>
    public DbContext Db => _db;

    /// <summary>Dich vu tinh quyen §6.2 (LOP 2 cua §6.4).</summary>
    public IQuyenService QuyenService => _quyenService;

    // =================================================================
    // 1. NAP NGU CANH
    // =================================================================

    /// <summary>Tim nguoi dung theo id (khong dung navigation).</summary>
    public Task<SysUser?> TimNguoiDungAsync(Guid userId, CancellationToken ct) =>
        _db.Set<SysUser>().FirstOrDefaultAsync(u => u.Id == userId, ct);

    /// <summary>
    /// Nap nhiem vu + toan bo phan cong + lich su xu ly + de xuat gia han dang cho duyet,
    /// roi tinh san bang quyen §6.2.
    /// Tra null khi khong tim thay nhiem vu hoac khong tim thay nguoi thao tac.
    /// </summary>
    public async Task<NhiemVuDaNap?> NapAsync(Guid idNhiemVu, Guid userId, DateOnly homNay, CancellationToken ct)
    {
        var nv = await _db.Set<DmNhiemVuChiTiet>().FirstOrDefaultAsync(x => x.Id == idNhiemVu, ct);
        if (nv is null) return null;

        var nguoi = await TimNguoiDungAsync(userId, ct);
        if (nguoi is null) return null;

        // Ke ca ban ghi da thu hoi (trangthai = 0) — NguCanh tu loc khi can (§4.3).
        var phanCong = await _db.Set<NhiemVuPhanCong>()
            .Where(p => p.IdNvChiTiet == idNhiemVu)
            .OrderBy(p => p.CreateDate)
            .ToListAsync(ct);

        var lichSu = await _db.Set<XuLyNhiemVu>()
            .Where(x => x.IdCtnv == idNhiemVu)
            .OrderBy(x => x.NgayXuLy)
            .ToListAsync(ct);

        // §2.4 T12/T13 — BAT BUOC nap de xuat gia han dang cho duyet truoc khi goi DuyetGiaHan.
        var giaHanChoDuyet = await _db.Set<GiaHanNhiemVu>()
            .Where(g => g.IdCtnv == idNhiemVu && g.TrangThai == TrangThaiGiaHan.ChoDuyet)
            .OrderByDescending(g => g.CreateDate)
            .FirstOrDefaultAsync(ct);

        var ctx = new NguCanh(homNay, nguoi, phanCong)
        {
            LichSuXuLy = lichSu,
            GiaHanChoDuyet = giaHanChoDuyet
        };

        var quyen = _quyenService.Tinh(nv, userId, ctx);
        return new NhiemVuDaNap(nv, nguoi, ctx, quyen);
    }

    /// <summary>
    /// Ghi cac ban ghi phu do may trang thai sinh ra (KetXuatHanhDong) vao DbContext.
    /// KHONG goi SaveChanges — controller tu goi de gom vao 1 giao dich.
    /// </summary>
    public void ApDungKetXuat(NguCanh ctx)
    {
        foreach (var x in ctx.KetXuat.LichSuXuLyMoi)
        {
            if (_db.Entry(x).State == EntityState.Detached) _db.Set<XuLyNhiemVu>().Add(x);
        }

        foreach (var g in ctx.KetXuat.GiaHanMoi)
        {
            if (_db.Entry(g).State == EntityState.Detached) _db.Set<GiaHanNhiemVu>().Add(g);
        }

        foreach (var g in ctx.KetXuat.GiaHanCapNhat)
        {
            if (_db.Entry(g).State == EntityState.Detached) _db.Set<GiaHanNhiemVu>().Update(g);
        }
    }

    // =================================================================
    // 2. TEP DINH KEM (§5.7)
    // =================================================================

    /// <summary>
    /// Gan cac tep da upload (§5.7 G1, luc do recordid = null) vao mot ban ghi nghiep vu.
    /// Chi cho phep gan tep do CHINH nguoi dang thao tac tai len (§6.4: khong tin FE).
    /// </summary>
    public async Task<Result> GanFileAsync(
        IEnumerable<Guid>? fileIds, Guid recordId, string loaiBanGhi, Guid userId, CancellationToken ct)
    {
        if (fileIds is null) return Result.Ok();

        var ids = fileIds.Where(x => x != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0) return Result.Ok();

        var ds = await _db.Set<NhiemVuFile>().Where(f => ids.Contains(f.Id)).ToListAsync(ct);
        if (ds.Count != ids.Count)
        {
            return Result.ThatBai("Một số tệp đính kèm không tồn tại hoặc đã bị xoá.", MaLoiChung.LoiTep);
        }

        foreach (var f in ds)
        {
            if (f.CreateBy != userId)
            {
                return Result.ThatBai(
                    "Bạn chỉ được đính kèm tệp do chính mình tải lên (tệp " + f.FileName + ").",
                    MaLoiChung.KhongCoQuyen);
            }

            if (f.RecordId.HasValue && f.RecordId.Value != recordId)
            {
                return Result.ThatBai(
                    "Tệp " + f.FileName + " đã được gắn vào một bản ghi khác.", MaLoiChung.LoiTep);
            }

            f.RecordId = recordId;
            f.LoaiBanGhi = loaiBanGhi;
        }

        return Result.Ok();
    }

    /// <summary>Doc tep dinh kem cua nhieu ban ghi cung loai, gom theo recordid.</summary>
    public async Task<Dictionary<Guid, List<FileDto>>> DocFileAsync(
        string loaiBanGhi, IReadOnlyCollection<Guid> recordIds, CancellationToken ct)
    {
        var kq = new Dictionary<Guid, List<FileDto>>();
        if (recordIds.Count == 0) return kq;

        var ids = recordIds.Distinct().ToList();
        var ds = await _db.Set<NhiemVuFile>()
            .Where(f => f.LoaiBanGhi == loaiBanGhi && f.RecordId != null && ids.Contains(f.RecordId.Value))
            .OrderBy(f => f.CreateDate)
            .ToListAsync(ct);

        foreach (var f in ds)
        {
            if (!f.RecordId.HasValue) continue;
            var key = f.RecordId.Value;
            if (!kq.TryGetValue(key, out var list))
            {
                list = new List<FileDto>();
                kq[key] = list;
            }
            list.Add(SangDto(f));
        }

        return kq;
    }

    /// <summary>Anh xa NhiemVuFile sang FileDto (§5.7 G1).</summary>
    public static FileDto SangDto(NhiemVuFile f) => new()
    {
        Id = f.Id,
        FileName = f.FileName,
        FilePath = f.FilePath,
        Size = f.FileSize,
        ContentType = f.ContentType,
        LoaiBanGhi = f.LoaiBanGhi,
        RecordId = f.RecordId,
        CreateBy = f.CreateBy,
        CreateDate = f.CreateDate
    };

    // =================================================================
    // 3. ANH XA NHIEM VU SANG DTO
    // =================================================================

    public sealed record TomTatUser(Guid Id, string FullName, string? ChucVu, string UnitCode, string? VaiTro);

    public sealed record TomTatVb(Guid Id, string? SoKyHieu, string TrichYeu);

    public sealed record TomTatDonVi(string UnitCode, string TenDonVi);

    public sealed record TomTatLinhVuc(string Ma, string Ten);

    public sealed record TomTatTen(Guid Id, string FullName);

    /// <summary>
    /// Anh xa mot danh sach nhiem vu sang DTO, kem 15 co quyen §6.2 cua
    /// <paramref name="nguoiThaoTac"/> tren tung dong.
    /// </summary>
    public async Task<List<NhiemVuDto>> SangDtoAsync(
        IReadOnlyList<DmNhiemVuChiTiet> ds,
        SysUser nguoiThaoTac,
        DateOnly homNay,
        bool kemQuyen,
        CancellationToken ct)
    {
        var kq = new List<NhiemVuDto>();
        if (ds.Count == 0) return kq;

        var idNv = ds.Select(x => x.Id).Distinct().ToList();

        var phanCong = await _db.Set<NhiemVuPhanCong>()
            .Where(p => idNv.Contains(p.IdNvChiTiet))
            .OrderBy(p => p.CreateDate)
            .ToListAsync(ct);

        var idUser = phanCong.Select(p => p.UserId)
            .Concat(ds.Select(x => x.UserIdGiaoViec))
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        var users = await _db.Set<SysUser>()
            .Where(u => idUser.Contains(u.Id))
            .Select(u => new TomTatUser(u.Id, u.FullName, u.ChucVu, u.UnitCode, u.VaiTro))
            .ToListAsync(ct);
        var mapUser = users.ToDictionary(u => u.Id);

        var unitCodes = users.Select(u => u.UnitCode)
            .Concat(phanCong.Select(p => p.UnitCode))
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct()
            .ToList();
        var units = await _db.Set<SysUnit>()
            .Where(u => unitCodes.Contains(u.UnitCode))
            .Select(u => new TomTatDonVi(u.UnitCode, u.TenDonVi))
            .ToListAsync(ct);
        var mapUnit = units.ToDictionary(u => u.UnitCode, u => u.TenDonVi);

        var maLinhVuc = ds.Select(x => x.LinhVuc)
            .Where(x => !string.IsNullOrEmpty(x))
            .Select(x => x!)
            .Distinct()
            .ToList();
        var linhVucs = await _db.Set<DmLinhVuc>()
            .Where(l => maLinhVuc.Contains(l.Ma))
            .Select(l => new TomTatLinhVuc(l.Ma, l.Ten))
            .ToListAsync(ct);
        var mapLinhVuc = linhVucs.ToDictionary(l => l.Ma, l => l.Ten);

        var idVb = ds.Select(x => x.IdVb).Where(x => x != Guid.Empty).Distinct().ToList();
        var vanBans = await _db.Set<DmVanBan>()
            .Where(v => idVb.Contains(v.Id))
            .Select(v => new TomTatVb(v.Id, v.SoKyHieu, v.TrichYeu))
            .ToListAsync(ct);
        var mapVb = vanBans.ToDictionary(v => v.Id);

        foreach (var nv in ds)
        {
            var pcs = phanCong.Where(p => p.IdNvChiTiet == nv.Id).ToList();

            QuyenNhiemVu? quyen = null;
            if (kemQuyen)
            {
                // Ngu canh RIENG cho tung dong — §6.2 chi can bang phan cong + trang thai.
                var ctxDong = new NguCanh(homNay, nguoiThaoTac, pcs);
                quyen = _quyenService.Tinh(nv, nguoiThaoTac.Id, ctxDong);
            }

            kq.Add(TaoDto(nv, pcs, mapUser, mapUnit, mapLinhVuc, mapVb, homNay, quyen));
        }

        return kq;
    }

    private static NhiemVuDto TaoDto(
        DmNhiemVuChiTiet nv,
        IReadOnlyList<NhiemVuPhanCong> pcs,
        IReadOnlyDictionary<Guid, TomTatUser> mapUser,
        IReadOnlyDictionary<string, string> mapUnit,
        IReadOnlyDictionary<string, string> mapLinhVuc,
        IReadOnlyDictionary<Guid, TomTatVb> mapVb,
        DateOnly homNay,
        QuyenNhiemVu? quyen)
    {
        var dto = new NhiemVuDto
        {
            Id = nv.Id,
            IdVb = nv.IdVb,
            NoiDung = nv.NoiDung,
            LinhVuc = nv.LinhVuc,
            DoKhan = nv.DoKhan,
            HanXuLyTh = nv.HanXuLyTh,
            SoNgayHxlTh = nv.SoNgayHxlTh,
            HanXuLyPh = nv.HanXuLyPh,
            NgayGiao = nv.NgayGiao,
            NgayTiepNhan = nv.NgayTiepNhan,
            NgayHoanThanhThucTe = nv.NgayHoanThanhThucTe,
            TrangThai = nv.TrangThai,
            TenTrangThai = TrangThaiNv.Nhan(nv.TrangThai),
            TrangThaiDvXuly = nv.TrangThaiDvXuly,
            TenTrangThaiDvXuly = nv.TrangThaiDvXuly.HasValue ? TrangThaiPh.Nhan(nv.TrangThaiDvXuly) : null,
            TrangThaiXuLyGiaHan = nv.TrangThaiXuLyGiaHan,
            SoLanGiaHan = nv.SoLanGiaHan,
            MucDoHt = nv.MucDoHt,
            PhanHoi = nv.PhanHoi,
            HsChatLuong = nv.HsChatLuong,
            UserIdGiaoViec = nv.UserIdGiaoViec,
            UserIdCreate = nv.UserIdCreate,
            UnitCode = nv.UnitCode,
            CreateDate = nv.CreateDate,
            UpdateDate = nv.UpdateDate,
            AiGoiYId = nv.AiGoiYId,
            SoNgayConLai = NgayUtil.SoNgayConLai(nv.HanXuLyTh, homNay),
            QuaHan = NgayUtil.QuaHan(nv.HanXuLyTh, homNay),
            SapHetHan = NgayUtil.SapHetHan(nv.HanXuLyTh, homNay),
            DiemCuoi = TrangThaiNv.LaDiemCuoi(nv.TrangThai, nv.TrangThaiDvXuly)
        };

        if (nv.LinhVuc is not null && mapLinhVuc.TryGetValue(nv.LinhVuc, out var tenLv)) dto.TenLinhVuc = tenLv;
        if (mapUser.TryGetValue(nv.UserIdGiaoViec, out var nguoiGiao)) dto.NguoiGiaoTen = nguoiGiao.FullName;
        if (mapVb.TryGetValue(nv.IdVb, out var vb))
        {
            dto.SoKyHieuVanBan = vb.SoKyHieu;
            dto.TrichYeuVanBan = vb.TrichYeu;
        }

        foreach (var p in pcs)
        {
            if (p.TrangThai != TrangThaiPhanCong.ConHieuLuc) continue;
            var tt = TaoTomTat(p, mapUser, mapUnit);
            if (p.VaiTro == VaiTroPhanCong.ChuTri) dto.ChuTri.Add(tt);
            else if (p.VaiTro == VaiTroPhanCong.PhoiHop) dto.PhoiHop.Add(tt);
        }

        dto.Quyen = quyen is null ? null : quyen.SangDto();
        return dto;
    }

    private static NguoiDungTomTatDto TaoTomTat(
        NhiemVuPhanCong p,
        IReadOnlyDictionary<Guid, TomTatUser> mapUser,
        IReadOnlyDictionary<string, string> mapUnit)
    {
        var dto = new NguoiDungTomTatDto
        {
            UserId = p.UserId,
            UnitCode = p.UnitCode
        };

        if (mapUser.TryGetValue(p.UserId, out var u))
        {
            dto.FullName = u.FullName;
            dto.ChucVu = u.ChucVu;
            dto.VaiTro = u.VaiTro;
            if (string.IsNullOrEmpty(dto.UnitCode)) dto.UnitCode = u.UnitCode;
        }

        if (!string.IsNullOrEmpty(dto.UnitCode) && mapUnit.TryGetValue(dto.UnitCode, out var tenDv))
        {
            dto.UnitName = tenDv;
        }

        return dto;
    }

    /// <summary>Danh sach phan cong day du (ke ca ban ghi da thu hoi) cua mot nhiem vu (§5.3 C4).</summary>
    public async Task<List<PhanCongDto>> PhanCongAsync(Guid idNhiemVu, CancellationToken ct)
    {
        var pcs = await _db.Set<NhiemVuPhanCong>()
            .Where(p => p.IdNvChiTiet == idNhiemVu)
            .OrderBy(p => p.CreateDate)
            .ToListAsync(ct);

        if (pcs.Count == 0) return new List<PhanCongDto>();

        var idUser = pcs.Select(p => p.UserId).Distinct().ToList();
        var users = await _db.Set<SysUser>()
            .Where(u => idUser.Contains(u.Id))
            .Select(u => new TomTatUser(u.Id, u.FullName, u.ChucVu, u.UnitCode, u.VaiTro))
            .ToListAsync(ct);
        var mapUser = users.ToDictionary(u => u.Id);

        var unitCodes = pcs.Select(p => p.UnitCode)
            .Concat(users.Select(u => u.UnitCode))
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct()
            .ToList();
        var units = await _db.Set<SysUnit>()
            .Where(u => unitCodes.Contains(u.UnitCode))
            .Select(u => new TomTatDonVi(u.UnitCode, u.TenDonVi))
            .ToListAsync(ct);
        var mapUnit = units.ToDictionary(u => u.UnitCode, u => u.TenDonVi);

        var kq = new List<PhanCongDto>();
        foreach (var p in pcs)
        {
            var dto = new PhanCongDto
            {
                Id = p.Id,
                UserId = p.UserId,
                UnitCode = p.UnitCode,
                VaiTro = p.VaiTro,
                TrangThai = p.TrangThai,
                CreateDate = p.CreateDate
            };

            if (mapUser.TryGetValue(p.UserId, out var u))
            {
                dto.FullName = u.FullName;
                dto.ChucVu = u.ChucVu;
                if (string.IsNullOrEmpty(dto.UnitCode)) dto.UnitCode = u.UnitCode;
            }

            if (!string.IsNullOrEmpty(dto.UnitCode) && mapUnit.TryGetValue(dto.UnitCode, out var tenDv))
            {
                dto.UnitName = tenDv;
            }

            kq.Add(dto);
        }

        return kq;
    }

    // =================================================================
    // 4. LICH SU (§5.4 D7, §5.6 F3)
    // =================================================================

    /// <summary>Lich su xu ly cua mot nhiem vu, kem tep dinh kem cua tung ban ghi.</summary>
    public async Task<List<XuLyDto>> LichSuXuLyAsync(Guid idNhiemVu, CancellationToken ct)
    {
        var ds = await _db.Set<XuLyNhiemVu>()
            .Where(x => x.IdCtnv == idNhiemVu)
            .OrderBy(x => x.NgayXuLy)
            .ToListAsync(ct);

        if (ds.Count == 0) return new List<XuLyDto>();

        var idUser = ds.Select(x => x.UserIdXuLy).Distinct().ToList();
        var tenNguoi = await _db.Set<SysUser>()
            .Where(u => idUser.Contains(u.Id))
            .Select(u => new TomTatTen(u.Id, u.FullName))
            .ToListAsync(ct);
        var mapUser = tenNguoi.ToDictionary(u => u.Id, u => u.FullName);

        var files = await DocFileAsync(LoaiBanGhiFile.XuLy, ds.Select(x => x.Id).ToList(), ct);

        var kq = new List<XuLyDto>();
        foreach (var x in ds)
        {
            var dto = new XuLyDto
            {
                Id = x.Id,
                IdCtnv = x.IdCtnv,
                Loai = x.Loai,
                TenLoai = LoaiXuLy.Nhan(x.Loai),
                NoiDung = x.NoiDung,
                MucDoHt = x.MucDoHt,
                TrangThai = x.TrangThai,
                TenTrangThai = x.TrangThai.HasValue ? TrangThaiNv.Nhan(x.TrangThai) : null,
                TrangThaiXuLy = x.TrangThaiXuLy,
                TrangThaiDvXuly = x.TrangThaiDvXuly,
                UserIdXuLy = x.UserIdXuLy,
                NgayXuLy = x.NgayXuLy
            };

            if (mapUser.TryGetValue(x.UserIdXuLy, out var ten)) dto.NguoiXuLyTen = ten;
            if (files.TryGetValue(x.Id, out var fs)) dto.Files = fs;
            kq.Add(dto);
        }

        return kq;
    }

    /// <summary>Lich su gia han cua mot nhiem vu (§5.6 F3), kem tep dinh kem.</summary>
    public async Task<List<GiaHanDto>> LichSuGiaHanAsync(Guid idNhiemVu, CancellationToken ct)
    {
        var ds = await _db.Set<GiaHanNhiemVu>()
            .Where(g => g.IdCtnv == idNhiemVu)
            .OrderBy(g => g.CreateDate)
            .ToListAsync(ct);

        if (ds.Count == 0) return new List<GiaHanDto>();

        var idUser = new List<Guid>();
        foreach (var g in ds)
        {
            idUser.Add(g.UserIdDeXuat);
            if (g.UserIdDuyet.HasValue) idUser.Add(g.UserIdDuyet.Value);
        }
        var idUserLoc = idUser.Where(x => x != Guid.Empty).Distinct().ToList();

        var tenNguoi = await _db.Set<SysUser>()
            .Where(u => idUserLoc.Contains(u.Id))
            .Select(u => new TomTatTen(u.Id, u.FullName))
            .ToListAsync(ct);
        var mapUser = tenNguoi.ToDictionary(u => u.Id, u => u.FullName);

        var files = await DocFileAsync(LoaiBanGhiFile.GiaHan, ds.Select(g => g.Id).ToList(), ct);

        var kq = new List<GiaHanDto>();
        foreach (var g in ds)
        {
            var dto = new GiaHanDto
            {
                Id = g.Id,
                IdCtnv = g.IdCtnv,
                NoiDung = g.NoiDung,
                HanXuLyDeXuat = g.HanXuLyDeXuat,
                HanXuLyThCu = g.HanXuLyThCu,
                TrangThai = g.TrangThai,
                TenTrangThai = TrangThaiGiaHan.Nhan(g.TrangThai),
                PhanHoi = g.PhanHoi,
                UserIdDeXuat = g.UserIdDeXuat,
                UserIdDuyet = g.UserIdDuyet,
                CreateDate = g.CreateDate,
                NgayDuyet = g.NgayDuyet
            };

            if (mapUser.TryGetValue(g.UserIdDeXuat, out var tenDx)) dto.NguoiDeXuatTen = tenDx;
            if (g.UserIdDuyet.HasValue && mapUser.TryGetValue(g.UserIdDuyet.Value, out var tenD))
            {
                dto.NguoiDuyetTen = tenD;
            }
            if (files.TryGetValue(g.Id, out var fs)) dto.Files = fs;

            kq.Add(dto);
        }

        return kq;
    }

    // =================================================================
    // 5. DANH MUC TRANG THAI HOP LE (§5.4 D5)
    // =================================================================

    /// <summary>
    /// §5.4 D5 — danh sach trang thai (truc A) duoc chon khi bao cao, DA LOC THEO HAN.
    /// Nhan/mau lay tu DM_TUDIEN type TRANGTHAINV (§2 — phai seed); neu danh muc thieu
    /// ban ghi thi lay nhan tu hang so TrangThaiNv de FE khong hien thi rong.
    /// </summary>
    public async Task<List<TuDienDto>> TrangThaiHopLeAsync(DmNhiemVuChiTiet nv, DateOnly homNay, CancellationToken ct)
    {
        var maHopLe = HanUtil.TrangThaiHopLeKhiBaoCao(nv, homNay);
        var chuoi = maHopLe.Select(m => m.ToString()).ToList();

        var dm = await _db.Set<DmTuDien>()
            .Where(d => d.Type == MaTypeTuDien.TrangThaiNv && chuoi.Contains(d.Ma))
            .ToListAsync(ct);

        var kq = new List<TuDienDto>();
        foreach (var ma in maHopLe)
        {
            var m = ma.ToString();
            var found = dm.FirstOrDefault(d => d.Ma == m);
            kq.Add(new TuDienDto
            {
                Type = MaTypeTuDien.TrangThaiNv,
                Ma = m,
                Nhan = found is null ? TrangThaiNv.Nhan(ma) : found.Nhan,
                Mau = found is null ? null : found.Mau,
                MoTa = found is null ? null : found.MoTa,
                ThuTu = found is null ? 0 : found.ThuTu
            });
        }

        return kq;
    }

    // =================================================================
    // 6. QUYEN TREN VAN BAN CHI DAO (§6.2 dong 1, 2, 3)
    // =================================================================

    /// <summary>
    /// §6.2 dong 1 — nguoi thuc hien chi thay van ban co nhiem vu giao cho minh.
    /// </summary>
    public async Task<bool> ChoPhepXemVanBanAsync(Guid idVanBan, SysUser nguoi, CancellationToken ct)
    {
        if (!nguoi.DangHoatDong) return false;
        if (MaVaiTro.LaBenGiao(nguoi.VaiTro)) return true;

        var idNv = _db.Set<DmNhiemVuChiTiet>().Where(n => n.IdVb == idVanBan).Select(n => n.Id);
        return await _db.Set<NhiemVuPhanCong>()
            .AnyAsync(p => idNv.Contains(p.IdNvChiTiet)
                           && p.UserId == nguoi.Id
                           && p.TrangThai == TrangThaiPhanCong.ConHieuLuc, ct);
    }

    /// <summary>
    /// §6.2 dong 2 — sua van ban: chi nguoi tao, VA van ban chua co nhiem vu nao o trang thai khac 3.
    /// Quan tri bo qua dieu kien SO HUU nhung VAN phai thoa dieu kien TRANG THAI
    /// (bam quyet dinh da chot o ban flow.js).
    /// </summary>
    public async Task<bool> ChoPhepSuaVanBanAsync(DmVanBan vb, SysUser nguoi, CancellationToken ct)
    {
        if (!nguoi.DangHoatDong) return false;
        if (!MaVaiTro.LaBenGiao(nguoi.VaiTro)) return false;

        var laQuanTri = nguoi.VaiTro == MaVaiTro.QuanTri;
        if (!laQuanTri && vb.UserIdCreate != nguoi.Id) return false;

        var coNvDaChay = await _db.Set<DmNhiemVuChiTiet>()
            .AnyAsync(n => n.IdVb == vb.Id && n.TrangThai != TrangThaiNv.ChuaTrienKhai, ct);
        return !coNvDaChay;
    }

    /// <summary>§6.2 dong 3 / §5.2 B5 — xoa van ban: chi nguoi tao, va CHUA CO nhiem vu con nao.</summary>
    public async Task<bool> ChoPhepXoaVanBanAsync(DmVanBan vb, SysUser nguoi, CancellationToken ct)
    {
        if (!nguoi.DangHoatDong) return false;
        if (!MaVaiTro.LaBenGiao(nguoi.VaiTro)) return false;

        var laQuanTri = nguoi.VaiTro == MaVaiTro.QuanTri;
        if (!laQuanTri && vb.UserIdCreate != nguoi.Id) return false;

        var coNvCon = await _db.Set<DmNhiemVuChiTiet>().AnyAsync(n => n.IdVb == vb.Id, ct);
        return !coNvCon;
    }
}
