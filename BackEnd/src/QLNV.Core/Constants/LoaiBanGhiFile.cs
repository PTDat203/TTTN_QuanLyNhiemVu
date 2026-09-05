namespace QLNV.Core.Constants;

/// <summary>
/// §4.6 — cot <c>loai_ban_ghi</c> cua bang NHIEMVU_FILE.
/// Cho biet <c>recordid</c> tro toi bang nghiep vu nao.
/// </summary>
public static class LoaiBanGhiFile
{
    /// <summary>Tep dinh kem cua DM_VANBAN (§4.1).</summary>
    public const string VanBan = "VANBAN";

    /// <summary>Tep dinh kem cua DM_NHIEMVU_CHITIET (§4.2).</summary>
    public const string NhiemVu = "NHIEMVU";

    /// <summary>Tep dinh kem cua XULY_NHIEMVU (§4.4) — ket qua bao cao.</summary>
    public const string XuLy = "XULY";

    /// <summary>Tep dinh kem cua GIAHAN_NHIEMVU (§4.5).</summary>
    public const string GiaHan = "GIAHAN";

    public static IReadOnlyList<string> ToanBo() => new[] { VanBan, NhiemVu, XuLy, GiaHan };

    public static bool HopLe(string? ma) => ma is not null && ToanBo().Contains(ma);
}
