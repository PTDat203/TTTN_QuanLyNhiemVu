namespace TaskApp.Api.Entities;

/// <summary>
/// Bang USER_SKILLS - ky nang/chuyen mon cua nguoi dung.
/// Day la du lieu dau vao chinh cho chuc nang AI goi y nguoi thuc hien phu hop.
/// </summary>
public class UserSkill : IAuditable
{
    /// <summary>Cot ID - khoa chinh.</summary>
    public long Id { get; set; }

    /// <summary>Cot USER_ID - khoa ngoai toi USERS.ID.</summary>
    public long UserId { get; set; }

    /// <summary>Cot SKILL_NAME - ten ky nang, vi du: C#, Oracle, Bao cao.</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>
    /// Cột SKILL_ID — mã kỹ năng chuẩn trong danh mục SKILLS.
    /// Cho phép rỗng trong giai đoạn chuyển tiếp; script 24 điền cho mọi dòng hiện có.
    /// Code mới so khớp kỹ năng theo cột này, không theo <see cref="SkillName"/>.
    /// </summary>
    public long? SkillId { get; set; }

    /// <summary>Cot SKILL_LEVEL - muc thanh thao tu 1 den 5, cho phep rong.</summary>
    public int? SkillLevel { get; set; }

    /// <summary>Cot DESCRIPTION - mo ta ngan ve ky nang.</summary>
    public string? Description { get; set; }

    /// <summary>Cột YEARS_EXPERIENCE — số năm kinh nghiệm với kỹ năng này, NUMBER(3,1).</summary>
    public decimal? YearsExperience { get; set; }

    /// <summary>Cot CREATED_AT.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Cot UPDATED_AT.</summary>
    public DateTime UpdatedAt { get; set; }

    // ----- Navigation property -----

    /// <summary>Nguoi dung so huu ky nang nay.</summary>
    public User? User { get; set; }

    /// <summary>Kỹ năng chuẩn tương ứng.</summary>
    public Skill? Skill { get; set; }
}
