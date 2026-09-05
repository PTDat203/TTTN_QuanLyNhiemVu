namespace QLNV.Core.Constants;

/// <summary>
/// Cac nguong / gioi han nghiep vu duoc dac ta ghi ro. Tap trung mot cho de
/// tang duoi khong hard-code rai rac.
/// </summary>
public static class GioiHan
{
    /// <summary>§4.2 / §4.4 / §1.2 buoc 2 — <c>noidung</c> toi da 2000 ky tu.</summary>
    public const int DoDaiNoiDung = 2000;

    /// <summary>§4.1 — <c>trichyeu</c> toi da 2000 ky tu.</summary>
    public const int DoDaiTrichYeu = 2000;

    /// <summary>§4.1 — <c>sokyhieu</c> toi da 100 ky tu.</summary>
    public const int DoDaiSoKyHieu = 100;

    /// <summary>§4.1 — <c>coquanbanhanh</c> / <c>nguoitheodoi</c> toi da 500 ky tu.</summary>
    public const int DoDaiCoQuanBanHanh = 500;

    /// <summary>§2.3 / §1.3 — <c>solangiahan</c> toi da 2 lan (guard <c>coTheGiaHan</c>).</summary>
    public const int SoLanGiaHanToiDa = 2;

    /// <summary>§3.1 M02 / §3.3 M08 / §5.10 J1 — nguong "sap het han" = 3 ngay.</summary>
    public const int NguongSapHetHan = 3;

    /// <summary>§1.2 buoc 4 / §10.2 — <c>mucdoht</c> nam trong [0, 100] (goc khong validate).</summary>
    public const int MucDoHtMin = 0;

    /// <summary>§1.2 buoc 4 / §10.2 — <c>mucdoht</c> nam trong [0, 100].</summary>
    public const int MucDoHtMax = 100;

    /// <summary>§4.2 / §5.5 E1 — <c>hsChatluong</c> nam trong [1, 6].</summary>
    public const int HsChatLuongMin = 1;

    /// <summary>§4.2 / §5.5 E1 — <c>hsChatluong</c> nam trong [1, 6].</summary>
    public const int HsChatLuongMax = 6;

    /// <summary>§4.7 — <c>max_concurrent_tasks</c> mac dinh = 8 (nguong tai cho AI).</summary>
    public const int MaxConcurrentTasksMacDinh = 8;

    /// <summary>§9.6 — so ung vien mac dinh cua API H1.</summary>
    public const int SoUngVienMacDinh = 5;

    /// <summary>§9.6 / §3.2 M06 — nut "Xem them 5 nguoi" nen goi lai voi so luong nay.</summary>
    public const int SoUngVienToiDa = 50;

    /// <summary>§9.7 — toi da 4 dong ly do hien thi tren M06.</summary>
    public const int SoDongLyDoToiDa = 4;

    /// <summary>§5.7 G1 — kich thuoc tep toi da 20 MB.</summary>
    public const long KichThuocTepToiDa = 20L * 1024 * 1024;

    /// <summary>§5.7 G1 — whitelist duoi tep duoc phep tai len.</summary>
    public static IReadOnlyList<string> DuoiTepChoPhep => new[]
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg"
    };

    /// <summary>Kich thuoc trang mac dinh cua cac API phan trang (§5.2 B1, §5.3 C3).</summary>
    public const int KichThuocTrangMacDinh = 20;

    /// <summary>Kich thuoc trang toi da — chan truy van qua lon.</summary>
    public const int KichThuocTrangToiDa = 200;
}
