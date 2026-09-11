namespace TaskApp.Api.Entities;

/// <summary>
/// Nhóm trong một phòng ban. Bảng TEAMS.
///
/// <para>
/// Phòng nhỏ không chia nhóm: nhân viên ở đó có <see cref="User.TeamId"/> rỗng và báo
/// cáo thẳng lên trưởng phòng. Hiện chỉ phòng Phát triển có nhóm (Backend, Frontend).
/// </para>
/// <para>
/// Database có khoá ngoại ghép (DEPARTMENT_ID, TEAM_ID) ở USERS và TASKS, nên một người
/// hay một nhiệm vụ không thể gắn vào nhóm của phòng khác.
/// </para>
/// </summary>
public class Team : IAuditable
{
    public long Id { get; set; }

    /// <summary>Phòng chứa nhóm. Bắt buộc — không có nhóm đứng ngoài phòng.</summary>
    public long DepartmentId { get; set; }

    /// <summary>Mã nhóm, duy nhất trong phạm vi một phòng. Ví dụ BACKEND.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Nhóm làm những việc gì. AI đọc phần này để đoán nhiệm vụ thuộc nhóm nào, nên viết
    /// kiểu liệt kê công việc cụ thể chứ không viết khẩu hiệu.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>Trưởng nhóm. Rỗng khi nhóm tạm chưa có người dẫn.</summary>
    public long? LeaderUserId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Department? Department { get; set; }
    public User? Leader { get; set; }
    public ICollection<User> ThanhVien { get; set; } = new List<User>();
    public ICollection<TaskItem> NhiemVu { get; set; } = new List<TaskItem>();
}
