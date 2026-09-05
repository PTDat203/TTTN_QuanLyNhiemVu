namespace QLNV.Core.Abstractions;

/// <summary>
/// §4.7 <c>password_hash</c> / §10.11 — bam va kiem tra mat khau.
/// Cai dat phai dung thuat toan co "muoi" va co chi phi (PBKDF2 / BCrypt / Argon2).
/// TUYET DOI khong luu mat khau dang ro, khong dung MD5/SHA1 tran.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Bam mat khau dang ro thanh chuoi luu duoc vao <c>SYS_USER.password_hash</c>.</summary>
    string Bam(string matKhau);

    /// <summary>
    /// Kiem tra mat khau dang ro co khop chuoi bam khong.
    /// Cai dat nen so sanh theo thoi gian hang so.
    /// </summary>
    bool KiemTra(string matKhau, string chuoiBam);
}
