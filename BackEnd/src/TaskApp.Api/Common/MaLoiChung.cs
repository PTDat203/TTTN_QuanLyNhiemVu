namespace TaskApp.Api.Common;

/// <summary>
/// Bo ma loi nghiep vu dung chung cho toan bo tang Services.
/// Controller doc <see cref="KetQua.MaLoi"/> de quy doi sang ma HTTP tuong ung.
/// <para>
/// Ten lop la <c>MaLoiChung</c> chu khong phai <c>MaLoi</c> de khong trung ten
/// voi thuoc tinh <see cref="KetQua.MaLoi"/>.
/// </para>
/// </summary>
public static class MaLoiChung
{
    /// <summary>Loi nghiep vu khong xep vao nhom nao khac. Quy doi HTTP 400.</summary>
    public const string LoiNghiepVu = "LOI_NGHIEP_VU";

    /// <summary>Du lieu dau vao khong hop le. Quy doi HTTP 400.</summary>
    public const string DuLieuKhongHopLe = "DU_LIEU_KHONG_HOP_LE";

    /// <summary>Khong tim thay ban ghi. Quy doi HTTP 404.</summary>
    public const string KhongTimThay = "KHONG_TIM_THAY";

    /// <summary>Nguoi dung khong du quyen thuc hien thao tac. Quy doi HTTP 403.</summary>
    public const string KhongCoQuyen = "KHONG_CO_QUYEN";

    /// <summary>Chua dang nhap hoac ma thong bao het han. Quy doi HTTP 401.</summary>
    public const string ChuaXacThuc = "CHUA_XAC_THUC";

    /// <summary>Sai ten dang nhap hoac mat khau. Quy doi HTTP 401.</summary>
    public const string XacThucThatBai = "XAC_THUC_THAT_BAI";

    /// <summary>Vi pham rang buoc duy nhat, vi du trung ten dang nhap. Quy doi HTTP 409.</summary>
    public const string TrungDuLieu = "TRUNG_DU_LIEU";

    /// <summary>Buoc chuyen trang thai khong dung vong doi nghiep vu. Quy doi HTTP 409.</summary>
    public const string ChuyenTrangThaiKhongHopLe = "CHUYEN_TRANG_THAI_KHONG_HOP_LE";

    /// <summary>Loi he thong ngoai du kien. Quy doi HTTP 500.</summary>
    public const string LoiHeThong = "LOI_HE_THONG";
}
