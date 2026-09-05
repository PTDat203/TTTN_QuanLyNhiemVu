using Microsoft.EntityFrameworkCore;
using QLNV.Core.Common;
using QLNV.Core.Constants;
using QLNV.Infrastructure.Data;

namespace QLNV.Infrastructure.Ai;

/// <summary>
/// Cai dat <see cref="IAiDataSource"/> bang truy van EF Core.
///
/// Nguyen tac: KHONG tinh diem o day. Lop nay chi lay du lieu tho; toan bo cong thuc
/// §9.4 nam trong bo cham diem (tang tren) de kiem thu duoc bang du lieu gia lap.
///
/// Toi uu da ap dung:
///  - <c>AsNoTracking</c> cho moi truy van (chi doc);
///  - CHI lay nhiem vu con y nghia voi cham diem: da nghiem thu (truc A thuoc {1,5} VA
///    truc B = 11 — §9.4 S1/S2/S3) hoac dang mo (truc A ngoai {1,5,97} — §4.8 view
///    <c>USER_TAI_HIENTAI</c>). Nhiem vu 97 da ket thuc va khong tinh vao tai;
///  - CHI lay ban ghi xu ly loai TUCHOI (§9.3 dieu 5) va ban ghi bi tra lai
///    (truc B = 12 — §9.4 S3b).
/// </summary>
public sealed class EfAiDataSource : IAiDataSource
{
    private readonly QlnvDbContext _db;

    public EfAiDataSource(QlnvDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <inheritdoc />
    public Task<AnhChupAi> TaiAnhChupAsync(CancellationToken ct) => TaiAnhChupAsync(null, ct);

    /// <inheritdoc />
    public async Task<AnhChupAi> TaiAnhChupAsync(IReadOnlyCollection<string>? phamViUnitCode, CancellationToken ct)
    {
        var truyVanNguoiDung = _db.NguoiDung.AsNoTracking()
            .Where(u => u.VaiTro == VaiTro.NguoiThucHien);

        if (phamViUnitCode is { Count: > 0 })
        {
            // Sao ra mang de EF dich duoc thanh menh de IN (...)
            var phamVi = phamViUnitCode.ToArray();
            truyVanNguoiDung = truyVanNguoiDung.Where(u => phamVi.Contains(u.UnitCode));
        }

        var nguoiDung = await truyVanNguoiDung
            .OrderBy(u => u.FullName)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var donVi = await _db.DonVi.AsNoTracking()
            .OrderBy(x => x.CapDonVi).ThenBy(x => x.UnitCode)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var linhVuc = await _db.LinhVuc.AsNoTracking()
            .OrderBy(x => x.ThuTu).ThenBy(x => x.Ma)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // §9.4 — chi can nhiem vu DA NGHIEM THU hoac DANG MO
        var nhiemVu = await _db.NhiemVu.AsNoTracking()
            .Where(nv =>
                ((nv.TrangThai == TrangThaiNv.HoanThanh || nv.TrangThai == TrangThaiNv.HoanThanhSauHan)
                 && nv.TrangThaiDvXuly == TrangThaiPh.DaXacNhan)
                || (nv.TrangThai != TrangThaiNv.HoanThanh
                    && nv.TrangThai != TrangThaiNv.HoanThanhSauHan
                    && nv.TrangThai != TrangThaiNv.DaThuHoi))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var idNhiemVu = nhiemVu.Select(x => x.Id).ToHashSet();

        var phanCong = await _db.PhanCong.AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // Chi giu phan cong tro toi cac nhiem vu da lay ve — giam bo nho khi du lieu lon
        phanCong = phanCong.Where(p => idNhiemVu.Contains(p.IdNvChiTiet)).ToList();

        // §9.3 dieu 5 (da tung tu choi) + §9.4 S3b (bi tra lai)
        var xuLy = await _db.XuLy.AsNoTracking()
            .Where(x => x.Loai == LoaiXuLy.TuChoi || x.TrangThaiDvXuly == TrangThaiPh.TuChoi)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new AnhChupAi
        {
            HomNay = NgayUtil.HomNay(),
            NguoiDung = nguoiDung,
            DonVi = donVi,
            LinhVuc = linhVuc,
            NhiemVu = nhiemVu,
            PhanCong = phanCong,
            XuLy = xuLy
        };
    }
}
