using QLNV.Core.Abstractions;

namespace QLNV.Infrastructure.Security;

/// <summary>
/// Cai dat <see cref="IPasswordHasher"/> bang BCrypt (goi <c>BCrypt.Net-Next</c>).
///
/// §10.11 — he goc de secret trong ma nguon; app moi TUYET DOI khong lam vay.
/// BCrypt tu sinh "muoi" ngau nhien va nhung vao chuoi ket qua, nen khong can cot rieng.
/// <c>WorkFactor</c> mac dinh 11: du cham de chong do vet can, van du nhanh cho demo.
/// </summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    /// <summary>So vong lap 2^WorkFactor. Tang len 12-13 khi trien khai that.</summary>
    public const int WorkFactorMacDinh = 11;

    private readonly int _workFactor;

    public BcryptPasswordHasher(int workFactor = WorkFactorMacDinh)
    {
        if (workFactor < 4 || workFactor > 16)
        {
            throw new ArgumentOutOfRangeException(nameof(workFactor),
                "Hệ số công việc của BCrypt phải nằm trong khoảng 4 đến 16.");
        }

        _workFactor = workFactor;
    }

    /// <inheritdoc />
    public string Bam(string matKhau)
    {
        if (string.IsNullOrEmpty(matKhau))
        {
            throw new ArgumentException("Mật khẩu không được để trống.", nameof(matKhau));
        }

        return BCrypt.Net.BCrypt.HashPassword(matKhau, _workFactor);
    }

    /// <inheritdoc />
    public bool KiemTra(string matKhau, string chuoiBam)
    {
        // Khong nem ngoai le khi du lieu xau: tra ve false de tang tren tra dung mot
        // thong bao "sai tai khoan hoac mat khau" (khong lo thong tin cho ke tan cong).
        if (string.IsNullOrEmpty(matKhau) || string.IsNullOrEmpty(chuoiBam)) return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(matKhau, chuoiBam);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Chuoi bam trong CSDL khong dung dinh dang BCrypt (du lieu cu / bi sua tay)
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
