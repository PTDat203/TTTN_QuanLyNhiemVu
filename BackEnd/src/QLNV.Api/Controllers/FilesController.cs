using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNV.Api.Common;
using QLNV.Api.Services;
using QLNV.Core.Abstractions;
using QLNV.Core.Common;
using QLNV.Core.Constants;
using QLNV.Core.Dtos;
using QLNV.Core.Entities;

namespace QLNV.Api.Controllers;

/// <summary>
/// §5.7 G1-G3 — tep dinh kem. Moi thao tac tep di qua <see cref="IFileStorage"/>
/// (hien tai la he thong tep cuc bo <c>App_Data/files</c>, sau nay doi sang MinIO
/// ma KHONG phai sua controller nay).
///
/// LUONG DUNG: FE upload truoc (G1, tep chua gan vao ban ghi nao), lay <c>id</c>,
/// roi gui kem <c>fileIds[]</c> trong body cua endpoint nghiep vu (B3/B4, C1, C5, D2/D3/D4, F1)
/// — chinh endpoint do moi gan tep vao ban ghi va kiem quyen theo §6.2.
///
/// KIEM QUYEN 2 LOP (§6.4):
///   LOP 1 — G1/G3: moi vai deu duoc goi nhung chi thao tac tren tep CUA CHINH MINH.
///           G2: moi vai, quyen thuc te xet o lop 2.
///   LOP 2 — G2 tai xuong: bam co <c>TaiTep</c> (§6.2 dong 19) cua ban ghi so huu tep;
///           G3 xoa: chi nguoi upload VA ban ghi chua khoa (chua toi diem cuoi §2.6).
/// </summary>
[Route("api/v1/files")]
public sealed class FilesController : QlnvControllerBase
{
    private readonly TroGiupNghiepVu _troGiup;
    private readonly IFileStorage _kho;

    public FilesController(IHienTai hienTai, TroGiupNghiepVu troGiup, IFileStorage kho)
        : base(hienTai)
    {
        _troGiup = troGiup;
        _kho = kho;
    }

    private DbContext Db => _troGiup.Db;

    // =================================================================
    // G1 — POST /api/v1/files  (multipart/form-data)
    // =================================================================

    /// <summary>
    /// §5.7 G1 — tai len mot hoac nhieu tep. Whitelist duoi tep va gioi han 20 MB/tep
    /// duoc kiem O DAY (khong tin content-type do trinh duyet gui).
    /// Tep tra ve co <c>recordId = null</c>; viec gan vao ban ghi do endpoint nghiep vu lam.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> TaiLen([FromForm] List<IFormFile> files, CancellationToken ct)
    {
        var chan = BatBuocDangNhap();
        if (chan is not null) return chan;

        var nguoi = await _troGiup.TimNguoiDungAsync(UserId, ct);
        if (nguoi is null || !nguoi.DangHoatDong) return Loi403("Tài khoản của bạn không tồn tại hoặc đã bị khoá.");

        if (files.Count == 0) return Loi400("Chưa chọn tệp nào để tải lên.");

        // Kiem TOAN BO tep truoc khi ghi tep dau tien (tranh luu nua chung).
        foreach (var f in files)
        {
            if (f.Length <= 0)
            {
                return Loi400($"Tệp \"{TenSach(f.FileName)}\" rỗng, không thể tải lên.");
            }

            if (f.Length > GioiHan.KichThuocTepToiDa)
            {
                var mb = GioiHan.KichThuocTepToiDa / (1024 * 1024);
                return Loi400($"Tệp \"{TenSach(f.FileName)}\" vượt quá dung lượng cho phép ({mb} MB).");
            }

            var duoi = Path.GetExtension(TenSach(f.FileName)).ToLowerInvariant();
            if (!GioiHan.DuoiTepChoPhep.Contains(duoi))
            {
                return Loi400(
                    $"Tệp \"{TenSach(f.FileName)}\" có định dạng không được phép. " +
                    "Chỉ chấp nhận: " + string.Join(", ", GioiHan.DuoiTepChoPhep) + ".");
            }
        }

        var bayGio = NgayUtil.BayGio();
        var kq = new List<FileDto>();

        foreach (var f in files)
        {
            var tenTep = TenSach(f.FileName);
            var contentType = string.IsNullOrWhiteSpace(f.ContentType)
                ? SuyContentType(tenTep)
                : f.ContentType;

            await using var luong = f.OpenReadStream();
            var luu = await _kho.LuuAsync(luong, tenTep, contentType, ct);
            if (luu.ThatBaiRoi)
            {
                return TuLoi(luu.Loi, luu.MaLoi ?? MaLoiChung.LoiTep);
            }

            var banGhi = new NhiemVuFile
            {
                Id = Guid.NewGuid(),
                RecordId = null,        // chua gan vao ban ghi nghiep vu nao
                LoaiBanGhi = null,
                FileName = tenTep,
                FilePath = luu.DuLieu ?? string.Empty,
                FileSize = f.Length,
                ContentType = contentType,
                CreateBy = nguoi.Id,
                CreateDate = bayGio
            };

            Db.Set<NhiemVuFile>().Add(banGhi);
            kq.Add(TroGiupNghiepVu.SangDto(banGhi));
        }

        await Db.SaveChangesAsync(ct);
        return Ok(kq);
    }

    // =================================================================
    // G2 — GET /api/v1/files/{id}
    // =================================================================

    /// <summary>§5.7 G2 — tai xuong. Kiem co <c>TaiTep</c> (§6.2 dong 19) tren ban ghi so huu tep.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> TaiXuong(Guid id, CancellationToken ct)
    {
        var chan = BatBuocDangNhap();
        if (chan is not null) return chan;

        var nguoi = await _troGiup.TimNguoiDungAsync(UserId, ct);
        if (nguoi is null || !nguoi.DangHoatDong) return Loi403("Tài khoản của bạn không tồn tại hoặc đã bị khoá.");

        var f = await Db.Set<NhiemVuFile>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (f is null) return Loi404("Không tìm thấy tệp đính kèm.");

        // §6.4 LOP 2
        var choPhep = await ChoPhepTaiAsync(f, nguoi, ct);
        if (!choPhep) return Loi403("Bạn không có quyền tải tệp đính kèm này.");

        var doc = await _kho.DocAsync(f.FilePath, ct);
        if (doc.ThatBaiRoi || doc.DuLieu is null)
        {
            return TuLoi(doc.Loi ?? "Không đọc được nội dung tệp.", doc.MaLoi ?? MaLoiChung.LoiTep);
        }

        var noiDung = doc.DuLieu;
        var contentType = string.IsNullOrWhiteSpace(noiDung.ContentType)
            ? (f.ContentType ?? SuyContentType(f.FileName))
            : noiDung.ContentType;

        return File(noiDung.NoiDung, contentType, f.FileName);
    }

    // =================================================================
    // G3 — DELETE /api/v1/files/{id}
    // =================================================================

    /// <summary>
    /// §5.7 G3 — xoa tep. CHI nguoi upload, va CHI khi ban ghi chua khoa.
    /// "Chua khoa" duoc dinh nghia (dac ta khong noi ro): tep chua gan vao ban ghi nao,
    /// HOAC nhiem vu so huu chua toi diem cuoi §2.6 (chua nghiem thu xong / chua bi thu hoi).
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Xoa(Guid id, CancellationToken ct)
    {
        var chan = BatBuocDangNhap();
        if (chan is not null) return chan;

        var nguoi = await _troGiup.TimNguoiDungAsync(UserId, ct);
        if (nguoi is null || !nguoi.DangHoatDong) return Loi403("Tài khoản của bạn không tồn tại hoặc đã bị khoá.");

        var f = await Db.Set<NhiemVuFile>().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (f is null) return Loi404("Không tìm thấy tệp đính kèm.");

        // §5.7 G3 — chi nguoi upload.
        if (f.CreateBy != nguoi.Id)
        {
            return Loi403("Chỉ người đã tải tệp lên mới được xoá tệp này.");
        }

        var idNhiemVu = await TimNhiemVuSoHuuAsync(f, ct);
        if (idNhiemVu.HasValue)
        {
            var nv = await Db.Set<DmNhiemVuChiTiet>().AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == idNhiemVu.Value, ct);

            if (nv is not null && TrangThaiNv.LaDiemCuoi(nv.TrangThai, nv.TrangThaiDvXuly) is not null)
            {
                return Loi409(
                    "Không thể xoá tệp: nhiệm vụ liên quan đã kết thúc (đã nghiệm thu hoặc đã thu hồi).",
                    MaLoiChung.ViPhamRangBuoc);
            }
        }

        var xoa = await _kho.XoaAsync(f.FilePath, ct);
        if (xoa.ThatBaiRoi)
        {
            return TuLoi(xoa.Loi, xoa.MaLoi ?? MaLoiChung.LoiTep);
        }

        Db.Set<NhiemVuFile>().Remove(f);
        await Db.SaveChangesAsync(ct);

        return NoContent();
    }

    // =================================================================
    // Ho tro
    // =================================================================

    /// <summary>§6.2 dong 19 — quyen tai tep, xet theo ban ghi so huu tep.</summary>
    private async Task<bool> ChoPhepTaiAsync(NhiemVuFile f, SysUser nguoi, CancellationToken ct)
    {
        // Tep chua gan vao ban ghi nao: chi nguoi upload duoc xem.
        if (!f.RecordId.HasValue || string.IsNullOrEmpty(f.LoaiBanGhi))
        {
            return f.CreateBy == nguoi.Id;
        }

        if (f.LoaiBanGhi == LoaiBanGhiFile.VanBan)
        {
            return await _troGiup.ChoPhepXemVanBanAsync(f.RecordId.Value, nguoi, ct);
        }

        var idNhiemVu = await TimNhiemVuSoHuuAsync(f, ct);
        if (!idNhiemVu.HasValue) return f.CreateBy == nguoi.Id;

        var nap = await _troGiup.NapAsync(idNhiemVu.Value, nguoi.Id, HomNay, ct);
        if (nap is null) return false;

        return nap.Quyen.TaiTep;
    }

    /// <summary>Tra ve id nhiem vu so huu tep (qua ban ghi NHIEMVU / XULY / GIAHAN).</summary>
    private async Task<Guid?> TimNhiemVuSoHuuAsync(NhiemVuFile f, CancellationToken ct)
    {
        if (!f.RecordId.HasValue || string.IsNullOrEmpty(f.LoaiBanGhi)) return null;
        var recordId = f.RecordId.Value;

        if (f.LoaiBanGhi == LoaiBanGhiFile.NhiemVu) return recordId;

        if (f.LoaiBanGhi == LoaiBanGhiFile.XuLy)
        {
            var x = await Db.Set<XuLyNhiemVu>().AsNoTracking()
                .Where(v => v.Id == recordId)
                .Select(v => (Guid?)v.IdCtnv)
                .FirstOrDefaultAsync(ct);
            return x;
        }

        if (f.LoaiBanGhi == LoaiBanGhiFile.GiaHan)
        {
            var g = await Db.Set<GiaHanNhiemVu>().AsNoTracking()
                .Where(v => v.Id == recordId)
                .Select(v => (Guid?)v.IdCtnv)
                .FirstOrDefaultAsync(ct);
            return g;
        }

        return null;
    }

    /// <summary>Bo duong dan trong ten tep do trinh duyet gui (chan duyet thu muc).</summary>
    private static string TenSach(string? tenGoc)
    {
        if (string.IsNullOrWhiteSpace(tenGoc)) return "tep-khong-ten";
        var ten = Path.GetFileName(tenGoc.Trim());
        return string.IsNullOrWhiteSpace(ten) ? "tep-khong-ten" : ten;
    }

    /// <summary>Suy content-type tu duoi tep (chi cho cac duoi nam trong whitelist §5.7 G1).</summary>
    private static string SuyContentType(string tenTep) => Path.GetExtension(tenTep).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xls" => "application/vnd.ms-excel",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".png" => "image/png",
        ".jpg" => "image/jpeg",
        ".jpeg" => "image/jpeg",
        _ => "application/octet-stream"
    };
}
