namespace QLNV.Core.Entities;

/// <summary>
/// §4.7 — bang <c>SYS_USER</c>. Goc: <c>IUserDataToken</c>.
/// §6.1: app moi dung MOT truc vai tro duy nhat (<see cref="VaiTro"/>), khong bo
/// 3 truc chong cheo cua he goc (Level / Chucvu / roles JWT).
/// </summary>
public class SysUser
{
    /// <summary>Khoa chinh. Cot <c>id</c>.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Ten dang nhap, duy nhat. Cot <c>username</c>.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Chuoi bam mat khau. Cot <c>password_hash</c>.
    /// §10.11: TUYET DOI khong luu mat khau dang ro, khong hard-code trong ma nguon.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Ho ten day du. Cot <c>fullname</c>.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Thu dien tu. Cot <c>email</c>.</summary>
    public string? Email { get; set; }

    /// <summary>FK toi <see cref="SysUnit"/>. Cot <c>unitcode</c>, varchar(50).</summary>
    public string UnitCode { get; set; } = string.Empty;

    /// <summary>Chuc vu dang chuoi tu do (vi du "Chuyen vien"). Cot <c>chucvu</c>.</summary>
    public string? ChucVu { get; set; }

    /// <summary>
    /// QUAN_TRI / NGUOI_GIAO / NGUOI_THUC_HIEN (xem <see cref="Constants.VaiTro"/>).
    /// Cot <c>vaitro</c>.
    /// </summary>
    public string VaiTro { get; set; } = Constants.VaiTro.NguoiThucHien;

    /// <summary>1 = hoat dong, 0 = khoa. §9.3 dieu 1: tai khoan khoa bi loai khoi goi y. Cot <c>trangthai</c>.</summary>
    public int TrangThai { get; set; } = 1;

    /// <summary>
    /// MOI so voi goc (§4.7) — nguong tai cho AI, mac dinh 8.
    /// Cot <c>max_concurrent_tasks</c>. Dung lam <c>K</c> trong §9.4 S4.
    /// </summary>
    public int MaxConcurrentTasks { get; set; } = Constants.GioiHan.MaxConcurrentTasksMacDinh;

    /// <summary>Chuoi refresh token dang hieu luc (§5.1 A2). Null khi da dang xuat.</summary>
    public string? RefreshToken { get; set; }

    /// <summary>Han cua refresh token hien tai.</summary>
    public DateTime? RefreshTokenHetHan { get; set; }

    /// <summary>Thoi diem tao ban ghi.</summary>
    public DateTime CreateDate { get; set; }

    // --- Dieu huong ---

    /// <summary>Don vi cua nguoi dung.</summary>
    public SysUnit? DonVi { get; set; }

    /// <summary>True khi tai khoan con hoat dong (§9.3 dieu 1).</summary>
    public bool DangHoatDong => TrangThai == 1;
}
