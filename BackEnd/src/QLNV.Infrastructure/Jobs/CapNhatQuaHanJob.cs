using Microsoft.EntityFrameworkCore;
using QLNV.Core.Common;
using QLNV.Core.Constants;
using QLNV.Core.Dtos;
using QLNV.Infrastructure.Data;

namespace QLNV.Infrastructure.Jobs;

/// <summary>
/// §5.9 I5 / §2.5 — job nen "cap nhat qua han".
///
/// Quy tac (bam ban <c>flow.js jobCapNhatQuaHan</c> da kiem chung):
///  - CHI ap cho truc A = 2 (Dang trien khai) va 3 (Chua trien khai);
///  - dieu kien: <c>hanxulyth &lt; hom nay</c> (khong co han thi KHONG BAO GIO qua han);
///  - ket qua: truc A := 7, cap nhat <c>updatedate</c>.
///
/// KHONG dung cho:
///  - 13 (dang cho duyet gia han) — truc A duoc khoi phuc luc T12/T13;
///  - 1, 5, 6, 97 — da o diem cuoi hoac dang cho nguoi giao xu ly;
///  - da la 7 roi.
///
/// ⚠ §7.2 de xuat Hangfire. Ban nay CO Y rut gon thanh lop thuong co ham
/// <see cref="ChayAsync"/> de controller (§5.9 I5) hoac mot <c>BackgroundService</c> o tang
/// API goi — khong them phu thuoc khong kiem chung duoc o luot nay.
/// </summary>
public sealed class CapNhatQuaHanJob
{
    /// <summary>Ten job tra ve trong <see cref="KetQuaJobDto.Job"/>.</summary>
    public const string TenJob = "CAP_NHAT_QUA_HAN";

    private readonly QlnvDbContext _db;

    public CapNhatQuaHanJob(QlnvDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <summary>
    /// Chay job. <paramref name="homNay"/> cho phep truyen ngay gia lap khi kiem thu.
    /// </summary>
    public async Task<KetQuaJobDto> ChayAsync(DateOnly? homNay = null, CancellationToken ct = default)
    {
        var batDau = NgayUtil.BayGio();
        var ngay = homNay ?? NgayUtil.HomNay();

        // §2.5 — chi 2 va 3, va phai co han xu ly nam TRUOC hom nay
        var dsCanSua = await _db.NhiemVu
            .Where(nv =>
                (nv.TrangThai == TrangThaiNv.DangTrienKhai || nv.TrangThai == TrangThaiNv.ChuaTrienKhai)
                && nv.HanXuLyTh != null
                && nv.HanXuLyTh < ngay)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var bayGio = NgayUtil.BayGio();
        foreach (var nv in dsCanSua)
        {
            nv.TrangThai = TrangThaiNv.DangTrienKhaiQuaHan;
            nv.UpdateDate = bayGio;
        }

        if (dsCanSua.Count > 0)
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return new KetQuaJobDto
        {
            Job = TenJob,
            SoBanGhi = dsCanSua.Count,
            BatDau = batDau,
            KetThuc = NgayUtil.BayGio(),
            ThongBao = dsCanSua.Count == 0
                ? "Không có nhiệm vụ nào cần chuyển sang trạng thái quá hạn."
                : $"Đã chuyển {dsCanSua.Count} nhiệm vụ sang trạng thái \"Đang triển khai - Đã hết hạn\"."
        };
    }
}
