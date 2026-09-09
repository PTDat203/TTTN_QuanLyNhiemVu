namespace TaskApp.Api.Common;

/// <summary>
/// Mot trang du lieu kem thong tin phan trang, dung cho moi API tra ve danh sach dai
/// (danh sach nhiem vu, lich su tien do, danh sach bao cao...).
/// </summary>
/// <typeparam name="T">Kieu phan tu trong danh sach.</typeparam>
public class KetQuaPhanTrang<T>
{
    /// <summary>Kich thuoc trang mac dinh khi client khong truyen.</summary>
    public const int KichThuocTrangMacDinh = 20;

    /// <summary>Kich thuoc trang toi da cho phep, chan client xin ca bang.</summary>
    public const int KichThuocTrangToiDa = 200;

    /// <summary>Danh sach ban ghi cua trang hien tai.</summary>
    public IReadOnlyList<T> DanhSach { get; init; } = Array.Empty<T>();

    /// <summary>So thu tu trang hien tai, dem tu 1.</summary>
    public int TrangHienTai { get; init; } = 1;

    /// <summary>So ban ghi toi da tren mot trang.</summary>
    public int KichThuocTrang { get; init; } = KichThuocTrangMacDinh;

    /// <summary>Tong so ban ghi thoa dieu kien loc, tinh tren toan bo bang.</summary>
    public int TongSoDong { get; init; }

    /// <summary>Tong so trang, tinh tu <see cref="TongSoDong"/> va <see cref="KichThuocTrang"/>.</summary>
    public int TongSoTrang =>
        KichThuocTrang <= 0 ? 0 : (int)Math.Ceiling(TongSoDong / (double)KichThuocTrang);

    /// <summary>Con trang truoc hay khong.</summary>
    public bool CoTrangTruoc => TrangHienTai > 1;

    /// <summary>Con trang sau hay khong.</summary>
    public bool CoTrangSau => TrangHienTai < TongSoTrang;

    /// <summary>Tao mot trang du lieu.</summary>
    /// <param name="danhSach">Danh sach ban ghi cua trang hien tai.</param>
    /// <param name="trangHienTai">So thu tu trang, dem tu 1.</param>
    /// <param name="kichThuocTrang">So ban ghi tren mot trang.</param>
    /// <param name="tongSoDong">Tong so ban ghi thoa dieu kien loc.</param>
    /// <returns>Doi tuong <see cref="KetQuaPhanTrang{T}"/>.</returns>
    public static KetQuaPhanTrang<T> Tao(
        IReadOnlyList<T> danhSach,
        int trangHienTai,
        int kichThuocTrang,
        int tongSoDong)
    {
        return new KetQuaPhanTrang<T>
        {
            DanhSach = danhSach,
            TrangHienTai = trangHienTai < 1 ? 1 : trangHienTai,
            KichThuocTrang = ChuanHoaKichThuocTrang(kichThuocTrang),
            TongSoDong = tongSoDong < 0 ? 0 : tongSoDong
        };
    }

    /// <summary>Tao mot trang rong.</summary>
    /// <param name="trangHienTai">So thu tu trang.</param>
    /// <param name="kichThuocTrang">So ban ghi tren mot trang.</param>
    /// <returns>Trang khong co ban ghi nao.</returns>
    public static KetQuaPhanTrang<T> Rong(int trangHienTai = 1, int kichThuocTrang = KichThuocTrangMacDinh)
    {
        return Tao(Array.Empty<T>(), trangHienTai, kichThuocTrang, 0);
    }

    /// <summary>
    /// Ep kich thuoc trang ve khoang hop le: nho hon 1 thi lay mac dinh,
    /// lon hon <see cref="KichThuocTrangToiDa"/> thi cat bot.
    /// </summary>
    /// <param name="kichThuocTrang">Gia tri client gui len.</param>
    /// <returns>Kich thuoc trang da chuan hoa.</returns>
    public static int ChuanHoaKichThuocTrang(int kichThuocTrang)
    {
        if (kichThuocTrang < 1)
        {
            return KichThuocTrangMacDinh;
        }

        if (kichThuocTrang > KichThuocTrangToiDa)
        {
            return KichThuocTrangToiDa;
        }

        return kichThuocTrang;
    }
}
