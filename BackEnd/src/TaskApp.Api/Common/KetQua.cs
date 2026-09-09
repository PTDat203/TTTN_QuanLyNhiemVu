namespace TaskApp.Api.Common;

/// <summary>
/// Ket qua cua mot thao tac nghiep vu KHONG kem du lieu tra ve.
/// <para>
/// Quy uoc cua du an: ham nghiep vu trong tang Services KHONG nem ngoai le cho loi nghiep vu.
/// Loi nghiep vu (khong tim thay, khong co quyen, sai vong doi trang thai...) tra ve
/// <see cref="KetQua"/> hoac <see cref="KetQua{T}"/>. Ngoai le chi danh cho su co ky thuat.
/// </para>
/// </summary>
public class KetQua
{
    /// <summary>Khoi tao mac dinh. Dung cac phuong thuc tinh <see cref="Ok"/> / <see cref="ThatBai"/>.</summary>
    protected KetQua()
    {
    }

    /// <summary>Thao tac co thanh cong hay khong.</summary>
    public bool ThanhCong { get; protected init; }

    /// <summary>Thong bao loi tieng Viet cho nguoi dung. Rong khi thanh cong.</summary>
    public string? Loi { get; protected init; }

    /// <summary>Ma loi de controller quy doi sang ma HTTP. Xem <see cref="MaLoiChung"/>.</summary>
    public string? MaLoi { get; protected init; }

    /// <summary>Nguoc lai cua <see cref="ThanhCong"/>, viet cho de doc trong cau dieu kien.</summary>
    public bool ThatBaiRoi => !ThanhCong;

    /// <summary>Tao ket qua thanh cong.</summary>
    /// <returns>Doi tuong <see cref="KetQua"/> co <see cref="ThanhCong"/> bang <c>true</c>.</returns>
    public static KetQua Ok()
    {
        return new KetQua { ThanhCong = true };
    }

    /// <summary>Tao ket qua that bai.</summary>
    /// <param name="loi">Thong bao loi tieng Viet.</param>
    /// <param name="maLoi">Ma loi, mac dinh <see cref="MaLoiChung.LoiNghiepVu"/>.</param>
    /// <returns>Doi tuong <see cref="KetQua"/> co <see cref="ThanhCong"/> bang <c>false</c>.</returns>
    public static KetQua ThatBai(string loi, string maLoi = MaLoiChung.LoiNghiepVu)
    {
        return new KetQua { ThanhCong = false, Loi = loi, MaLoi = maLoi };
    }

    /// <summary>Tao ket qua that bai voi ma <see cref="MaLoiChung.KhongTimThay"/>.</summary>
    /// <param name="loi">Thong bao loi tieng Viet.</param>
    /// <returns>Ket qua that bai.</returns>
    public static KetQua KhongTimThay(string loi)
    {
        return ThatBai(loi, MaLoiChung.KhongTimThay);
    }

    /// <summary>Tao ket qua that bai voi ma <see cref="MaLoiChung.KhongCoQuyen"/>.</summary>
    /// <param name="loi">Thong bao loi tieng Viet.</param>
    /// <returns>Ket qua that bai.</returns>
    public static KetQua KhongCoQuyen(string loi = "Bạn không có quyền thực hiện thao tác này.")
    {
        return ThatBai(loi, MaLoiChung.KhongCoQuyen);
    }

    /// <summary>Tao ket qua that bai voi ma <see cref="MaLoiChung.DuLieuKhongHopLe"/>.</summary>
    /// <param name="loi">Thong bao loi tieng Viet.</param>
    /// <returns>Ket qua that bai.</returns>
    public static KetQua DuLieuKhongHopLe(string loi)
    {
        return ThatBai(loi, MaLoiChung.DuLieuKhongHopLe);
    }
}

/// <summary>
/// Ket qua cua mot thao tac nghiep vu CO kem du lieu tra ve.
/// </summary>
/// <typeparam name="T">Kieu du lieu tra ve khi thanh cong.</typeparam>
public class KetQua<T> : KetQua
{
    /// <summary>Khoi tao mac dinh. Dung cac phuong thuc tinh cua lop.</summary>
    protected KetQua()
    {
    }

    /// <summary>Du lieu tra ve. Rong khi thao tac that bai.</summary>
    public T? DuLieu { get; protected init; }

    /// <summary>Tao ket qua thanh cong kem du lieu.</summary>
    /// <param name="duLieu">Du lieu tra ve.</param>
    /// <returns>Ket qua thanh cong.</returns>
    public static KetQua<T> Ok(T duLieu)
    {
        return new KetQua<T> { ThanhCong = true, DuLieu = duLieu };
    }

    /// <summary>Tao ket qua that bai.</summary>
    /// <param name="loi">Thong bao loi tieng Viet.</param>
    /// <param name="maLoi">Ma loi, mac dinh <see cref="MaLoiChung.LoiNghiepVu"/>.</param>
    /// <returns>Ket qua that bai, <see cref="DuLieu"/> rong.</returns>
    public new static KetQua<T> ThatBai(string loi, string maLoi = MaLoiChung.LoiNghiepVu)
    {
        return new KetQua<T> { ThanhCong = false, Loi = loi, MaLoi = maLoi };
    }

    /// <summary>Tao ket qua that bai voi ma <see cref="MaLoiChung.KhongTimThay"/>.</summary>
    /// <param name="loi">Thong bao loi tieng Viet.</param>
    /// <returns>Ket qua that bai.</returns>
    public new static KetQua<T> KhongTimThay(string loi)
    {
        return ThatBai(loi, MaLoiChung.KhongTimThay);
    }

    /// <summary>Tao ket qua that bai voi ma <see cref="MaLoiChung.KhongCoQuyen"/>.</summary>
    /// <param name="loi">Thong bao loi tieng Viet.</param>
    /// <returns>Ket qua that bai.</returns>
    public new static KetQua<T> KhongCoQuyen(string loi = "Bạn không có quyền thực hiện thao tác này.")
    {
        return ThatBai(loi, MaLoiChung.KhongCoQuyen);
    }

    /// <summary>Tao ket qua that bai voi ma <see cref="MaLoiChung.DuLieuKhongHopLe"/>.</summary>
    /// <param name="loi">Thong bao loi tieng Viet.</param>
    /// <returns>Ket qua that bai.</returns>
    public new static KetQua<T> DuLieuKhongHopLe(string loi)
    {
        return ThatBai(loi, MaLoiChung.DuLieuKhongHopLe);
    }
}
