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
/// §5.6 F1-F3 — nhanh gia hạn (§1.3, §2.3, §2.4 T11/T12/T13).
///
/// F1 Xin gia han  — vai NGUOI_THUC_HIEN, §6.2 dong 15.
/// F2 Duyet gia han — vai QUAN_TRI / NGUOI_GIAO, §6.2 dong 16.
/// F3 Lich su      — moi nguoi lien quan, §6.2 dong 18.
///
/// Toan bo phep chuyen trang thai do <see cref="INhiemVuStateMachine"/> thuc hien.
/// </summary>
[Route("api/v1")]
public sealed class GiaHanController : QlnvControllerBase
{
    private readonly TroGiupNghiepVu _troGiup;
    private readonly INhiemVuStateMachine _mayTrangThai;

    public GiaHanController(IHienTai hienTai, TroGiupNghiepVu troGiup, INhiemVuStateMachine mayTrangThai)
        : base(hienTai)
    {
        _troGiup = troGiup;
        _mayTrangThai = mayTrangThai;
    }

    private DbContext Db => _troGiup.Db;

    // =================================================================
    // F1 — POST /api/v1/nhiem-vu/{id}/gia-han
    // =================================================================

    /// <summary>
    /// §5.6 F1 / §2.4 T11 — de xuat gia han.
    /// Ket qua: <c>trangthai := 13</c>, <c>trangthaixulygiahan := 10</c>, sinh 1 ban ghi GIAHAN_NHIEMVU.
    /// Chan khi <c>solangiahan &gt;= 2</c> hoac dang co yeu cau treo (§6.2 dong 15).
    /// </summary>
    [HttpPost("nhiem-vu/{id:guid}/gia-han")]
    public async Task<IActionResult> XinGiaHan(Guid id, [FromBody] GiaHanRequest req, CancellationToken ct)
    {
        // §6.4 LOP 1 — §6.2 dong 15: chi NGUOI_THUC_HIEN.
        var chan = BatBuocBenLam("xin gia hạn");
        if (chan is not null) return chan;

        var loi = await KiemTraAsync(req, ct);
        if (loi is not null) return loi;

        var homNay = HomNay;
        var nap = await _troGiup.NapAsync(id, UserId, homNay, ct);
        if (nap is null) return Loi404("Không tìm thấy nhiệm vụ.");

        // §6.4 LOP 2
        if (!nap.Quyen.XinGiaHan)
        {
            return Loi403(MoTaTuChoi("xin gia hạn cho nhiệm vụ này", nap.NhiemVu));
        }

        // §4.5 — hanxulydexuat PHAI >= hanxulyth hien tai. Kiem o day de bao loi kem ngay cu;
        // may trang thai van kiem lai lan nua (§6.4: khong tin FE).
        var hanHienTai = nap.NhiemVu.HanXuLyTh;
        if (hanHienTai.HasValue && req.HanXuLyDeXuat < hanHienTai.Value)
        {
            return Loi400(
                "Hạn xử lý đề xuất phải muộn hơn hoặc bằng hạn hiện tại (" +
                NgayUtil.DinhDang(hanHienTai) + ").");
        }

        var kq = _mayTrangThai.XinGiaHan(nap.NhiemVu, UserId, req, nap.Ctx);
        var loiKq = NeuLoi(kq);
        if (loiKq is not null) return loiKq;

        _troGiup.ApDungKetXuat(nap.Ctx);

        // Tep dinh kem cua de xuat gia han gan vao BAN GHI GIA HAN vua sinh (§4.6 loai GIAHAN).
        var banGhi = nap.Ctx.KetXuat.GiaHanMoi.LastOrDefault();
        if (banGhi is not null && req.FileIds is not null && req.FileIds.Count > 0)
        {
            var ganFile = await _troGiup.GanFileAsync(req.FileIds, banGhi.Id, LoaiBanGhiFile.GiaHan, UserId, ct);
            var loiFile = NeuLoi(ganFile);
            if (loiFile is not null) return loiFile;
        }

        await Db.SaveChangesAsync(ct);

        return Ok(new
        {
            nhiemVu = (await _troGiup.SangDtoAsync(new[] { nap.NhiemVu }, nap.NguoiThaoTac, homNay, true, ct))[0],
            lichSuGiaHan = await _troGiup.LichSuGiaHanAsync(id, ct)
        });
    }

    // =================================================================
    // F2 — POST /api/v1/gia-han/{idGiaHan}/duyet
    // =================================================================

    /// <summary>
    /// §5.6 F2 / §2.4 T12-T13 — duyet hoac tu choi de xuat gia han.
    /// DUYET   =&gt; <c>trangthaixulygiahan := 11</c>, <c>solangiahan += 1</c>, <c>hanxulyth := hanxulydexuat</c>.
    /// TU_CHOI =&gt; <c>trangthaixulygiahan := 12</c>, han giu nguyen.
    /// Truc A duoc khoi phuc tu <c>GIAHAN_NHIEMVU.trangthai_cu</c> co ap quy tac han §2.5.
    /// </summary>
    [HttpPost("gia-han/{idGiaHan:guid}/duyet")]
    public async Task<IActionResult> Duyet(Guid idGiaHan, [FromBody] DuyetGiaHanRequest req, CancellationToken ct)
    {
        // §6.4 LOP 1 — §6.2 dong 16: chi QUAN_TRI / NGUOI_GIAO.
        var chan = BatBuocBenGiao("duyệt đề xuất gia hạn");
        if (chan is not null) return chan;

        var loi = await KiemTraAsync(req, ct);
        if (loi is not null) return loi;

        var gh = await Db.Set<GiaHanNhiemVu>().FirstOrDefaultAsync(g => g.Id == idGiaHan, ct);
        if (gh is null) return Loi404("Không tìm thấy đề xuất gia hạn.");

        if (gh.TrangThai != TrangThaiGiaHan.ChoDuyet)
        {
            return Loi409(
                $"Đề xuất gia hạn này đã được xử lý ({TrangThaiGiaHan.Nhan(gh.TrangThai)}), không thể duyệt lại.",
                MaLoiChung.SaiTrangThai);
        }

        var homNay = HomNay;
        var nap = await _troGiup.NapAsync(gh.IdCtnv, UserId, homNay, ct);
        if (nap is null) return Loi404("Không tìm thấy nhiệm vụ tương ứng với đề xuất gia hạn.");

        // §6.4 LOP 2 — §6.2 dong 16 (+ chan truc A = 97: §2.6 khong cho hoi sinh diem cuoi).
        if (!nap.Quyen.DuyetGiaHan)
        {
            return Loi403(MoTaTuChoi("duyệt đề xuất gia hạn của nhiệm vụ này", nap.NhiemVu));
        }

        // BAT BUOC: ngu canh phai tro dung ban ghi gia han duoc chi dinh tren URL.
        var ctx = nap.Ctx with { GiaHanChoDuyet = gh };

        // Id tren URL la nguon su that; khong tin gia tri FE gui trong body (§6.4).
        req.IdGiaHan = gh.Id;

        var kq = _mayTrangThai.DuyetGiaHan(nap.NhiemVu, UserId, req, ctx);
        var loiKq = NeuLoi(kq);
        if (loiKq is not null) return loiKq;

        _troGiup.ApDungKetXuat(ctx);
        await Db.SaveChangesAsync(ct);

        return Ok(new
        {
            nhiemVu = (await _troGiup.SangDtoAsync(new[] { nap.NhiemVu }, nap.NguoiThaoTac, homNay, true, ct))[0],
            lichSuGiaHan = await _troGiup.LichSuGiaHanAsync(gh.IdCtnv, ct)
        });
    }

    // =================================================================
    // F3 — GET /api/v1/nhiem-vu/{id}/gia-han
    // =================================================================

    /// <summary>§5.6 F3 — lich su gia han cua mot nhiem vu (§6.2 dong 18).</summary>
    [HttpGet("nhiem-vu/{id:guid}/gia-han")]
    public async Task<IActionResult> LichSu(Guid id, CancellationToken ct)
    {
        var chan = BatBuocDangNhap();
        if (chan is not null) return chan;

        var nap = await _troGiup.NapAsync(id, UserId, HomNay, ct);
        if (nap is null) return Loi404("Không tìm thấy nhiệm vụ.");

        if (!nap.Quyen.XemChiTiet)
        {
            return Loi403("Bạn không có quyền xem lịch sử gia hạn của nhiệm vụ này.");
        }

        var ds = await _troGiup.LichSuGiaHanAsync(id, ct);
        return Ok(ds);
    }

    // =================================================================
    // Ho tro
    // =================================================================

    private static string MoTaTuChoi(string tenHanhDong, DmNhiemVuChiTiet nv)
    {
        var a = TrangThaiNv.Nhan(nv.TrangThai);
        var c = TrangThaiGiaHan.Nhan(nv.TrangThaiXuLyGiaHan);
        return $"Bạn không có quyền {tenHanhDong} ở trạng thái hiện tại " +
               $"({a} / gia hạn: {c} / đã gia hạn {nv.SoLanGiaHan} lần).";
    }
}
