using Microsoft.EntityFrameworkCore;
using QLNV.Core.Common;
using QLNV.Core.Constants;
using QLNV.Core.Dtos;
using QLNV.Infrastructure.Data;

namespace QLNV.Infrastructure.Jobs;

/// <summary>
/// §5.9 I4 / §4.8 — job nen "tong hop hieu suat nguoi dung".
///
/// ⚠ QUYET DINH THIET KE (phai ghi vao bao cao): §4.8 mo ta bang tong hop
/// <c>USER_HIEUSUAT</c> nhung ghi ro day "thuan tuy la bo nho dem" va "neu du lieu it
/// (&lt; 5.000 nhiem vu) co the BO HAN bang nay, tinh thang bang truy van luc goi API".
/// Bo du lieu cua de tai co ~350 nhiem vu, nen ban nay KHONG tao bang <c>USER_HIEUSUAT</c>
/// (BAN KE KIEU cua QLNV.Core cung khong co entity tuong ung). Job chi TINH LAI va tra ve
/// ket qua; muon them bang dem sau nay thi chi phai them mot buoc ghi o cuoi ham.
///
/// Cong thuc bam dung §4.8 + §9.4:
///  - <c>so_nv_hoanthanh</c>: truc A thuoc {1,5} VA truc B = 11;
///  - <c>so_nv_dunghan</c>  : dem rieng truc A = 1;
///  - <c>so_nv_bi_tralai</c>: ban ghi XULY_NHIEMVU co truc B = 12 do chinh nguoi do ghi;
///  - <c>so_lan_giahan</c>  : tong <c>solangiahan</c> cua cac nhiem vu da nghiem thu;
///  - view <c>USER_TAI_HIENTAI</c>: chi tinh tren vai CHUTRI con hieu luc,
///    nhiem vu co truc A NGOAI {1, 5, 97}; trong so tai theo <c>dokhan</c> (2,0 / 1,5 / 1,0).
///
/// ⚠ §7.2 de xuat Hangfire — xem ghi chu o <see cref="CapNhatQuaHanJob"/>.
/// </summary>
public sealed class TongHopHieuSuatJob
{
    /// <summary>Ten job tra ve trong <see cref="KetQuaJobDto.Job"/>.</summary>
    public const string TenJob = "CAP_NHAT_HIEU_SUAT";

    private readonly QlnvDbContext _db;

    public TongHopHieuSuatJob(QlnvDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <summary>Chay job va tra ve so nguoi dung da duoc tong hop.</summary>
    public async Task<KetQuaJobDto> ChayAsync(CancellationToken ct = default)
    {
        var batDau = NgayUtil.BayGio();
        var ds = await TinhHieuSuatAsync(null, null, ct).ConfigureAwait(false);

        return new KetQuaJobDto
        {
            Job = TenJob,
            SoBanGhi = ds.Count,
            BatDau = batDau,
            KetThuc = NgayUtil.BayGio(),
            ThongBao = $"Đã tổng hợp hiệu suất của {ds.Count} người thực hiện."
        };
    }

    /// <summary>
    /// Tinh hieu suat cua mot nguoi (hoac tat ca neu <paramref name="userId"/> = null).
    /// </summary>
    /// <param name="userId">Chi tinh cho mot nguoi. Null = moi nguoi thuc hien.</param>
    /// <param name="linhVuc">Chi dem nhiem vu thuoc linh vuc nay. Null = moi linh vuc.</param>
    public async Task<List<HieuSuatNguoiDungDto>> TinhHieuSuatAsync(
        Guid? userId, string? linhVuc, CancellationToken ct = default)
    {
        var truyVanNguoiDung = _db.NguoiDung.AsNoTracking()
            .Where(u => u.VaiTro == VaiTro.NguoiThucHien);

        if (userId.HasValue)
        {
            var id = userId.Value;
            truyVanNguoiDung = _db.NguoiDung.AsNoTracking().Where(u => u.Id == id);
        }

        var nguoiDung = await truyVanNguoiDung.ToListAsync(ct).ConfigureAwait(false);
        if (nguoiDung.Count == 0) return new List<HieuSuatNguoiDungDto>();

        var idNguoiDung = nguoiDung.Select(u => u.Id).ToHashSet();

        // Chi lay phan cong CHU TRI con hieu luc (§4.8 view USER_TAI_HIENTAI cung loc nhu vay)
        var phanCong = await _db.PhanCong.AsNoTracking()
            .Where(p => p.VaiTro == VaiTroPhanCong.ChuTri
                        && p.TrangThai == TrangThaiPhanCong.ConHieuLuc)
            .Select(p => new { p.IdNvChiTiet, p.UserId })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var nhiemVu = await _db.NhiemVu.AsNoTracking()
            .Select(nv => new
            {
                nv.Id,
                nv.LinhVuc,
                nv.DoKhan,
                nv.TrangThai,
                nv.TrangThaiDvXuly,
                nv.SoLanGiaHan
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var nvTheoId = nhiemVu.ToDictionary(x => x.Id);

        // §9.4 S3 (b) / §4.8 so_nv_bi_tralai — dem ban ghi bi cham CHUA DAT theo nguoi ghi bao cao.
        // Loc theo linh vuc lam TRONG BO NHO vi phai tra cuu linh vuc cua nhiem vu tuong ung.
        var banGhiTraLai = await _db.XuLy.AsNoTracking()
            .Where(x => x.TrangThaiDvXuly == TrangThaiPh.TuChoi)
            .Select(x => new { x.UserIdXuLy, x.IdCtnv })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var traLaiTheoUser = new Dictionary<Guid, int>();
        foreach (var x in banGhiTraLai)
        {
            if (linhVuc is not null)
            {
                if (!nvTheoId.TryGetValue(x.IdCtnv, out var nvTraLai)) continue;
                if (!string.Equals(nvTraLai.LinhVuc, linhVuc, StringComparison.Ordinal)) continue;
            }

            traLaiTheoUser[x.UserIdXuLy] = traLaiTheoUser.TryGetValue(x.UserIdXuLy, out var c) ? c + 1 : 1;
        }

        var ketQua = new Dictionary<Guid, HieuSuatNguoiDungDto>();
        foreach (var u in nguoiDung)
        {
            ketQua[u.Id] = new HieuSuatNguoiDungDto
            {
                UserId = u.Id,
                FullName = u.FullName,
                LinhVuc = linhVuc,
                K = u.MaxConcurrentTasks > 0 ? u.MaxConcurrentTasks : GioiHan.MaxConcurrentTasksMacDinh,
                UpdateDate = NgayUtil.BayGio()
            };
        }

        foreach (var pc in phanCong)
        {
            if (!idNguoiDung.Contains(pc.UserId)) continue;
            if (!nvTheoId.TryGetValue(pc.IdNvChiTiet, out var nv)) continue;
            if (linhVuc is not null && !string.Equals(nv.LinhVuc, linhVuc, StringComparison.Ordinal)) continue;

            var o = ketQua[pc.UserId];

            bool daNghiemThu =
                (nv.TrangThai == TrangThaiNv.HoanThanh || nv.TrangThai == TrangThaiNv.HoanThanhSauHan)
                && nv.TrangThaiDvXuly == TrangThaiPh.DaXacNhan;

            if (daNghiemThu)
            {
                o.SoNvHoanThanh++;
                if (nv.TrangThai == TrangThaiNv.HoanThanh) o.SoNvDungHan++;
                o.SoLanGiaHan += nv.SoLanGiaHan;
            }

            // §4.8 view: nhiem vu DANG MO = truc A ngoai {1, 5, 97}
            bool dangMo = nv.TrangThai != TrangThaiNv.HoanThanh
                          && nv.TrangThai != TrangThaiNv.HoanThanhSauHan
                          && nv.TrangThai != TrangThaiNv.DaThuHoi;

            if (dangMo)
            {
                o.SoNvDangMo++;
                o.TaiTrongSo += DoKhan.TrongSo(nv.DoKhan);
                if (nv.TrangThai == TrangThaiNv.DangTrienKhaiQuaHan) o.SoNvQuaHan++;
            }
        }

        foreach (var o in ketQua.Values)
        {
            o.SoNvBiTraLai = traLaiTheoUser.TryGetValue(o.UserId, out var n) ? n : 0;
            o.TyLeDungHan = o.SoNvHoanThanh > 0
                ? Math.Round((double)o.SoNvDungHan / o.SoNvHoanThanh, 4)
                : 0d;
            o.TaiTrongSo = Math.Round(o.TaiTrongSo, 2);
        }

        return ketQua.Values
            .OrderByDescending(x => x.SoNvHoanThanh)
            .ThenBy(x => x.FullName, StringComparer.Ordinal)
            .ToList();
    }
}
