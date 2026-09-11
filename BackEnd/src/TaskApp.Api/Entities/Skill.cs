namespace TaskApp.Api.Entities;

/// <summary>
/// Danh mục kỹ năng chuẩn hoá. Bảng SKILLS.
///
/// <para>
/// Mỗi người khai kỹ năng một kiểu — "C#", "C Sharp", ".NET C#" — nên không so khớp được
/// bằng tên. Bảng này quy tất cả về một <see cref="Code"/>.
/// </para>
/// <para>
/// <see cref="Description"/> là phần mô hình nhúng đọc để hiểu kỹ năng. Tên "Kế toán" quá
/// ngắn để mô hình phân biệt với "Kiểm toán"; mô tả liệt kê việc cụ thể thì phân biệt được.
/// </para>
/// </summary>
public class Skill : IAuditable
{
    public long Id { get; set; }

    /// <summary>Mã chuẩn, ví dụ ASPNET_CORE. Duy nhất.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Tên hiển thị, ví dụ "ASP.NET Core". Duy nhất.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Nhóm kỹ năng: BACKEND, FRONTEND, DATA, TESTING, MANAGEMENT, HR, ACCOUNTING, ADMIN.
    /// Khớp ràng buộc CK_SKILLS_CATEGORY.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<UserSkill> NguoiCo { get; set; } = new List<UserSkill>();
    public ICollection<TaskRequiredSkill> NhiemVuCan { get; set; } = new List<TaskRequiredSkill>();
}
