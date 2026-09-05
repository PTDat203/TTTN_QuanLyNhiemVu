using System.Security.Cryptography;
using System.Text;

namespace QLNV.Infrastructure.Data.Seed;

/// <summary>
/// Sinh <see cref="Guid"/> TAT DINH tu mot khoa chuoi.
///
/// Ban seed.js dung ma chuoi de doc ("U05", "NV0123", "VB01"). Mo hinh du lieu §4 lai
/// dung <c>uuid</c> lam khoa chinh. Ham nay bac cau hai the gioi: cung mot khoa chuoi
/// LUON cho cung mot Guid, ke ca sau khi xoa va seed lai CSDL — nho vay tai lieu demo,
/// anh chup man hinh va kich ban kiem thu van tro dung ban ghi.
///
/// MD5 o day CHI dung lam ham bam TAT DINH de sinh dinh danh, KHONG dung cho bao mat.
/// </summary>
public static class MaGuid
{
    /// <summary>Tien to khong gian ten — doi tien to nay se doi TOAN BO id cua bo du lieu mau.</summary>
    private const string KhongGianTen = "QLNV-SEED-v1:";

    /// <summary>Doi khoa chuoi ("U05", "NV0123"...) thanh Guid tat dinh.</summary>
    public static Guid Tu(string khoa)
    {
        ArgumentNullException.ThrowIfNull(khoa);
        byte[] bam = MD5.HashData(Encoding.UTF8.GetBytes(KhongGianTen + khoa));
        return new Guid(bam);
    }

    /// <summary>Ghep tien to voi so thu tu co dem 0 o dau ("NV", 12, 4) =&gt; "NV0012".</summary>
    public static string Ma(string tienTo, int so, int rong) =>
        tienTo + so.ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(rong, '0');
}
