namespace TaskApp.Api.Entities;

/// <summary>
/// Phòng ban. Bảng DEPARTMENTS.
///
/// Đây là đơn vị lọc chính của chức năng gợi ý: AI đoán nhiệm vụ thuộc phòng nào
/// rồi chỉ xét ứng viên trong phòng đó.
/// </summary>
public class Department : IAuditable
{
    public long Id { get; set; }

    /// <summary>Mã phòng, ví dụ PHAT_TRIEN. Duy nhất.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Mô tả chức năng nhiệm vụ của phòng. AI dùng để đoán phòng cho nhiệm vụ.</summary>
    public string? Description { get; set; }

    /// <summary>Trưởng phòng. Cột HEAD_USER_ID.</summary>
    public long? HeadUserId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<User> ThanhVien { get; set; } = new List<User>();
    public ICollection<TaskItem> NhiemVu { get; set; } = new List<TaskItem>();

    public User? Head { get; set; }
    public ICollection<Team> Nhom { get; set; } = new List<Team>();
}
