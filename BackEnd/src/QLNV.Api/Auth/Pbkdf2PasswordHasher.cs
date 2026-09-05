using System.Security.Cryptography;
using QLNV.Core.Abstractions;

namespace QLNV.Api.Auth;

/// <summary>
/// §4.7 / §10.11 — cai dat <see cref="IPasswordHasher"/> bang PBKDF2-HMAC-SHA256.
/// Dinh dang chuoi luu: <c>PBKDF2$SHA256${soVongLap}${muoiBase64}${bamBase64}</c>.
///
/// Dung BCL thuan (khong them goi NuGet). So sanh bang
/// <see cref="CryptographicOperations.FixedTimeEquals"/> de tranh tan cong do thoi gian.
///
/// Dang ky bang <c>TryAddSingleton</c> SAU <c>AddInfrastructure</c>: neu tang
/// Infrastructure da dang ky ban cai dat rieng thi ban do duoc giu nguyen.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string TienTo = "PBKDF2";
    private const string TenBam = "SHA256";
    private const int SoVongLapMacDinh = 120_000;
    private const int DoDaiMuoi = 16;
    private const int DoDaiBam = 32;

    /// <inheritdoc />
    public string Bam(string matKhau)
    {
        if (matKhau is null)
        {
            throw new ArgumentNullException(nameof(matKhau));
        }

        var muoi = RandomNumberGenerator.GetBytes(DoDaiMuoi);
        var bam = Rfc2898DeriveBytes.Pbkdf2(
            password: matKhau,
            salt: muoi,
            iterations: SoVongLapMacDinh,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: DoDaiBam);

        return string.Join('$',
            TienTo,
            TenBam,
            SoVongLapMacDinh.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToBase64String(muoi),
            Convert.ToBase64String(bam));
    }

    /// <inheritdoc />
    public bool KiemTra(string matKhau, string chuoiBam)
    {
        if (string.IsNullOrEmpty(matKhau) || string.IsNullOrWhiteSpace(chuoiBam))
        {
            return false;
        }

        var phan = chuoiBam.Split('$');
        if (phan.Length != 5 || phan[0] != TienTo || phan[1] != TenBam)
        {
            return false;
        }

        if (!int.TryParse(phan[2], System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var soVongLap) || soVongLap <= 0)
        {
            return false;
        }

        byte[] muoi;
        byte[] bamMongDoi;
        try
        {
            muoi = Convert.FromBase64String(phan[3]);
            bamMongDoi = Convert.FromBase64String(phan[4]);
        }
        catch (FormatException)
        {
            // Ban ghi hong / khong dung dinh dang -> coi nhu sai mat khau, khong nem ra ngoai.
            return false;
        }

        var bamThucTe = Rfc2898DeriveBytes.Pbkdf2(
            password: matKhau,
            salt: muoi,
            iterations: soVongLap,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: bamMongDoi.Length);

        return CryptographicOperations.FixedTimeEquals(bamThucTe, bamMongDoi);
    }
}
