namespace QLNV.Core.Common;

/// <summary>
/// Ket qua cua mot thao tac nghiep vu, KHONG kem du lieu tra ve.
///
/// QUY UOC TOAN DU AN: moi ham nghiep vu tra <see cref="Result"/> hoac
/// <see cref="Result{T}"/>. KHONG nem exception cho loi nghiep vu — exception
/// chi danh cho loi ha tang (mat ket noi, hong tep...).
///
/// LUU Y VE TEN: dac ta yeu cau vua co thuoc tinh <c>Loi</c> vua co factory
/// <c>Result.Loi(...)</c>. C# khong cho phep mot kieu vua co thuoc tinh vua co
/// phuong thuc trung ten (CS0102), nen giu THUOC TINH <see cref="Loi"/> (phan
/// du lieu, duoc doc o khap noi) va dat ten factory la <see cref="ThatBai"/>.
/// </summary>
public class Result
{
    protected Result()
    {
    }

    /// <summary>True neu thao tac thanh cong.</summary>
    public bool ThanhCong { get; protected init; }

    /// <summary>Thong bao loi cho nguoi dung — LUON tieng Viet co dau. Null khi thanh cong.</summary>
    public string? Loi { get; protected init; }

    /// <summary>Ma loi may doc duoc (xem <see cref="MaLoiChung"/>). Tuy chon.</summary>
    public string? MaLoi { get; protected init; }

    /// <summary>Nghich dao cua <see cref="ThanhCong"/> — cho de doc o phia goi.</summary>
    public bool ThatBaiRoi => !ThanhCong;

    /// <summary>Tao ket qua thanh cong.</summary>
    public static Result Ok() => new() { ThanhCong = true };

    /// <summary>
    /// Tao ket qua that bai. <paramref name="thongBao"/> phai la tieng Viet co dau.
    /// </summary>
    public static Result ThatBai(string thongBao, string? ma = null) =>
        new() { ThanhCong = false, Loi = thongBao, MaLoi = ma };

    /// <summary>Bi danh cua <see cref="ThatBai(string, string?)"/> — doc xuoi hon o vai cho.</summary>
    public static Result TaoLoi(string thongBao, string? ma = null) => ThatBai(thongBao, ma);
}

/// <summary>
/// Ket qua cua mot thao tac nghiep vu CO du lieu tra ve.
/// Co y KHONG ke thua <see cref="Result"/> de tranh nhap nhang khi phan giai
/// cac phuong thuc tinh cung ten giua lop cha va lop con.
/// </summary>
/// <typeparam name="T">Kieu du lieu tra ve khi thanh cong.</typeparam>
public sealed class Result<T>
{
    private Result()
    {
    }

    /// <summary>True neu thao tac thanh cong.</summary>
    public bool ThanhCong { get; private init; }

    /// <summary>Thong bao loi cho nguoi dung — LUON tieng Viet co dau. Null khi thanh cong.</summary>
    public string? Loi { get; private init; }

    /// <summary>Ma loi may doc duoc (xem <see cref="MaLoiChung"/>). Tuy chon.</summary>
    public string? MaLoi { get; private init; }

    /// <summary>Du lieu tra ve. Chi co nghia khi <see cref="ThanhCong"/> = true.</summary>
    public T? DuLieu { get; private init; }

    /// <summary>Nghich dao cua <see cref="ThanhCong"/>.</summary>
    public bool ThatBaiRoi => !ThanhCong;

    /// <summary>Tao ket qua thanh cong kem du lieu.</summary>
    public static Result<T> Ok(T duLieu) => new() { ThanhCong = true, DuLieu = duLieu };

    /// <summary>
    /// Tao ket qua that bai. <paramref name="thongBao"/> phai la tieng Viet co dau.
    /// </summary>
    public static Result<T> ThatBai(string thongBao, string? ma = null) =>
        new() { ThanhCong = false, Loi = thongBao, MaLoi = ma };

    /// <summary>Chuyen mot ket qua that bai kieu khac sang kieu nay (giu nguyen thong bao va ma loi).</summary>
    public static Result<T> ThatBaiTu(Result nguon) =>
        new() { ThanhCong = false, Loi = nguon.Loi, MaLoi = nguon.MaLoi };

    /// <summary>Bi danh cua <see cref="ThatBai(string, string?)"/>.</summary>
    public static Result<T> TaoLoi(string thongBao, string? ma = null) => ThatBai(thongBao, ma);

    /// <summary>Bo phan du lieu, tra ve <see cref="Result"/> tuong duong.</summary>
    public Result BoDuLieu() => ThanhCong ? Result.Ok() : Result.ThatBai(Loi ?? string.Empty, MaLoi);
}

/// <summary>
/// Ma loi chuan cua he thong. Tang API anh xa ma nay sang HTTP status
/// (§6.4: sai quyen theo du lieu tra 403).
/// </summary>
public static class MaLoiChung
{
    /// <summary>Du lieu dau vao khong hop le (400).</summary>
    public const string DuLieuKhongHopLe = "DU_LIEU_KHONG_HOP_LE";

    /// <summary>Khong tim thay ban ghi (404).</summary>
    public const string KhongTimThay = "KHONG_TIM_THAY";

    /// <summary>Chua dang nhap hoac token het han (401).</summary>
    public const string ChuaDangNhap = "CHUA_DANG_NHAP";

    /// <summary>Khong du quyen theo bang §6.2 (403).</summary>
    public const string KhongCoQuyen = "KHONG_CO_QUYEN";

    /// <summary>Hanh dong khong hop le o trang thai hien tai (409) — §2.4.</summary>
    public const string SaiTrangThai = "SAI_TRANG_THAI";

    /// <summary>Vi pham rang buoc nghiep vu (409) — vi du solangiahan &gt;= 2.</summary>
    public const string ViPhamRangBuoc = "VI_PHAM_RANG_BUOC";

    /// <summary>Noi dung nhiem vu bi trung (§5.3 C2, fail-open).</summary>
    public const string TrungNoiDung = "TRUNG_NOI_DUNG";

    /// <summary>Loi thao tac tep (§5.7 G1-G3).</summary>
    public const string LoiTep = "LOI_TEP";
}
