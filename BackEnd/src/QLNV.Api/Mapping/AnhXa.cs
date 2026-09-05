using QLNV.Core.Dtos;
using QLNV.Core.Entities;

namespace QLNV.Api.Mapping;

/// <summary>
/// Anh xa entity -> DTO cho ba controller nen (Auth, DanhMuc, NguoiDung).
/// Ten khoa JSON da duoc chot bang <c>[JsonPropertyName]</c> ngay tren DTO o QLNV.Core
/// (§5, §9.6, §7.4 muc 1) — o day chi gan gia tri, KHONG dat lai ten.
/// </summary>
public static class AnhXa
{
    /// <summary>§5.1 A1 — thong tin nguoi dung tra kem token.</summary>
    public static NguoiDungDto SangDto(SysUser nguoiDung, string? tenDonVi = null) => new()
    {
        Id = nguoiDung.Id,
        UserName = nguoiDung.UserName,
        FullName = nguoiDung.FullName,
        Email = nguoiDung.Email,
        ChucVu = nguoiDung.ChucVu,
        UnitCode = nguoiDung.UnitCode,
        UnitName = tenDonVi ?? nguoiDung.DonVi?.TenDonVi,
        VaiTro = nguoiDung.VaiTro,
        TrangThai = nguoiDung.TrangThai,
        MaxConcurrentTasks = nguoiDung.MaxConcurrentTasks
    };

    /// <summary>§5.1 A6 / §5.3 C4 — thong tin nguoi dung rut gon.</summary>
    public static NguoiDungTomTatDto SangTomTat(SysUser nguoiDung, string? tenDonVi = null) => new()
    {
        UserId = nguoiDung.Id,
        FullName = nguoiDung.FullName,
        ChucVu = nguoiDung.ChucVu,
        UnitCode = nguoiDung.UnitCode,
        UnitName = tenDonVi ?? nguoiDung.DonVi?.TenDonVi,
        VaiTro = nguoiDung.VaiTro
    };

    /// <summary>§5.1 A4 — mot muc danh muc DM_TUDIEN.</summary>
    public static TuDienDto SangDto(DmTuDien muc) => new()
    {
        Type = muc.Type,
        Ma = muc.Ma,
        Nhan = muc.Nhan,
        Mau = muc.Mau,
        MoTa = muc.MoTa,
        ThuTu = muc.ThuTu
    };

    /// <summary>§5.1 A5 — mot nut linh vuc (chua gan con).</summary>
    public static LinhVucDto SangDto(DmLinhVuc linhVuc) => new()
    {
        Ma = linhVuc.Ma,
        Ten = linhVuc.Ten,
        NhomCha = linhVuc.NhomCha,
        TrangThai = linhVuc.TrangThai,
        ThuTu = linhVuc.ThuTu
    };

    /// <summary>§5.1 A6 — mot nut don vi (chua gan con, chua gan nguoi dung).</summary>
    public static DonViDto SangDto(SysUnit donVi) => new()
    {
        UnitCode = donVi.UnitCode,
        TenDonVi = donVi.TenDonVi,
        MaCha = donVi.MaCha,
        CapDonVi = donVi.CapDonVi,
        TrangThai = donVi.TrangThai
    };

    /// <summary>
    /// §5.1 A5 — dung cay linh vuc tu danh sach phang.
    /// Nut mo coi (co <c>NhomCha</c> nhung khong tim thay cha) duoc dua len goc de
    /// khong bi mat khoi ket qua.
    /// </summary>
    public static List<LinhVucDto> DungCayLinhVuc(IEnumerable<DmLinhVuc> danhSach)
    {
        ArgumentNullException.ThrowIfNull(danhSach);

        var theoMa = new Dictionary<string, LinhVucDto>(StringComparer.Ordinal);
        var thuTuNguon = new List<DmLinhVuc>();

        foreach (var nguon in danhSach)
        {
            if (string.IsNullOrWhiteSpace(nguon.Ma) || theoMa.ContainsKey(nguon.Ma))
            {
                continue;
            }

            theoMa[nguon.Ma] = SangDto(nguon);
            thuTuNguon.Add(nguon);
        }

        var goc = new List<LinhVucDto>();
        foreach (var nguon in thuTuNguon)
        {
            var nut = theoMa[nguon.Ma];
            if (!string.IsNullOrWhiteSpace(nguon.NhomCha)
                && theoMa.TryGetValue(nguon.NhomCha!, out var cha)
                && !ReferenceEquals(cha, nut))
            {
                cha.Con.Add(nut);
            }
            else
            {
                goc.Add(nut);
            }
        }

        return goc;
    }

    /// <summary>
    /// §5.1 A6 — dung cay don vi kem nguoi dung tu hai danh sach phang.
    /// </summary>
    /// <param name="donVi">Danh sach don vi (nen loc <c>trangthai = 1</c> truoc khi goi).</param>
    /// <param name="nguoiDung">Danh sach nguoi dung se gan vao dung don vi.</param>
    public static List<DonViDto> DungCayDonVi(IEnumerable<SysUnit> donVi, IEnumerable<SysUser> nguoiDung)
    {
        ArgumentNullException.ThrowIfNull(donVi);
        ArgumentNullException.ThrowIfNull(nguoiDung);

        var theoMa = new Dictionary<string, DonViDto>(StringComparer.Ordinal);
        var thuTuNguon = new List<SysUnit>();

        foreach (var nguon in donVi)
        {
            if (string.IsNullOrWhiteSpace(nguon.UnitCode) || theoMa.ContainsKey(nguon.UnitCode))
            {
                continue;
            }

            theoMa[nguon.UnitCode] = SangDto(nguon);
            thuTuNguon.Add(nguon);
        }

        foreach (var nd in nguoiDung)
        {
            if (!string.IsNullOrWhiteSpace(nd.UnitCode) && theoMa.TryGetValue(nd.UnitCode, out var nut))
            {
                nut.NguoiDung.Add(SangTomTat(nd, nut.TenDonVi));
            }
        }

        var goc = new List<DonViDto>();
        foreach (var nguon in thuTuNguon)
        {
            var nut = theoMa[nguon.UnitCode];
            if (!string.IsNullOrWhiteSpace(nguon.MaCha)
                && theoMa.TryGetValue(nguon.MaCha!, out var cha)
                && !ReferenceEquals(cha, nut))
            {
                cha.Con.Add(nut);
            }
            else
            {
                goc.Add(nut);
            }
        }

        return goc;
    }
}
