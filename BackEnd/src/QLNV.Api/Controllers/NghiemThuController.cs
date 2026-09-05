using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNV.Api.Common;
using QLNV.Api.Services;
using QLNV.Core.Abstractions;
using QLNV.Core.Constants;
using QLNV.Core.Dtos;

namespace QLNV.Api.Controllers;

/// <summary>
/// §5.5 E1 — Kiem tra ket qua (nghiem thu), §2.4 T9/T10.
///
/// DAT      =&gt; <c>trangthaiDvXuly = 11</c>, truc A giu nguyen — DIEM CUOI §2.6.
/// CHUA_DAT =&gt; <c>trangthaiDvXuly = 12</c>; neu truc A thuoc {1,5} thi ve 2 (con han) / 7 (qua han)
///             — day chinh la mui ten "Yeu cau bo sung -&gt; Thuc hien" cua §1.
///
/// KIEM QUYEN 2 LOP (§6.4):
///   LOP 1 — chi QUAN_TRI / NGUOI_GIAO (§6.2 dong 14, cot NGUOI_THUC_HIEN la ❌).
///   LOP 2 — co <c>KiemTraKetQua</c> cua <see cref="IQuyenService"/>:
///           <c>trangthaiDvXuly = 10</c> VA la nguoi giao/nguoi tao, VA truc A khong thuoc {6, 97}
///           (loai 6 vi cap (6,10) la T4/T5 chu khong phai T9/T10).
/// </summary>
[Route("api/v1/nhiem-vu")]
public sealed class NghiemThuController : QlnvControllerBase
{
    private readonly TroGiupNghiepVu _troGiup;
    private readonly INhiemVuStateMachine _mayTrangThai;

    public NghiemThuController(IHienTai hienTai, TroGiupNghiepVu troGiup, INhiemVuStateMachine mayTrangThai)
        : base(hienTai)
    {
        _troGiup = troGiup;
        _mayTrangThai = mayTrangThai;
    }

    /// <summary>§5.5 E1 — <c>POST /api/v1/nhiem-vu/{id}/nghiem-thu</c>.</summary>
    [HttpPost("{id:guid}/nghiem-thu")]
    public async Task<IActionResult> NghiemThu(Guid id, [FromBody] NghiemThuRequest req, CancellationToken ct)
    {
        // §6.4 LOP 1
        var chan = BatBuocBenGiao("kiểm tra kết quả nhiệm vụ");
        if (chan is not null) return chan;

        var loi = await KiemTraAsync(req, ct);
        if (loi is not null) return loi;

        var homNay = HomNay;
        var nap = await _troGiup.NapAsync(id, UserId, homNay, ct);
        if (nap is null) return Loi404("Không tìm thấy nhiệm vụ.");

        // §6.4 LOP 2 — §6.2 dong 14.
        if (!nap.Quyen.KiemTraKetQua)
        {
            var a = TrangThaiNv.Nhan(nap.NhiemVu.TrangThai);
            var b = TrangThaiPh.Nhan(nap.NhiemVu.TrangThaiDvXuly);
            return Loi403($"Bạn không có quyền kiểm tra kết quả nhiệm vụ này ở trạng thái hiện tại ({a} / {b}).");
        }

        // Quyet dinh chuyen trang thai do MAY TRANG THAI thuc hien (khong viet lai o controller).
        var kq = _mayTrangThai.NghiemThu(nap.NhiemVu, UserId, req, nap.Ctx);
        var loiKq = NeuLoi(kq);
        if (loiKq is not null) return loiKq;

        _troGiup.ApDungKetXuat(nap.Ctx);
        await _troGiup.Db.SaveChangesAsync(ct);

        var dto = await _troGiup.SangDtoAsync(new[] { nap.NhiemVu }, nap.NguoiThaoTac, homNay, kemQuyen: true, ct);
        return Ok(dto[0]);
    }
}
