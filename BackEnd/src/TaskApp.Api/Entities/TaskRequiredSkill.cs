namespace TaskApp.Api.Entities;

/// <summary>
/// Nhiệm vụ đòi hỏi kỹ năng gì, ở mức nào. Bảng TASK_REQUIRED_SKILLS.
///
/// <para>
/// <see cref="Source"/> phân biệt kỹ năng AI tự trích từ mô tả với kỹ năng người giao nhập
/// tay. Giữ riêng hai nguồn để đo được AI trích đúng bao nhiêu phần trăm so với người.
/// </para>
/// </summary>
public class TaskRequiredSkill : ICoNgayTao
{
    public long Id { get; set; }
    public long TaskId { get; set; }
    public long SkillId { get; set; }

    /// <summary>Mức tối thiểu mong muốn, 1..5. Rỗng khi chỉ cần biết, không đòi mức cụ thể.</summary>
    public int? RequiredLevel { get; set; }

    /// <summary>AI hoặc MANUAL, xem <see cref="NguonKyNang"/>.</summary>
    public string Source { get; set; } = NguonKyNang.ThuCong;

    public DateTime CreatedAt { get; set; }

    public TaskItem? Task { get; set; }
    public Skill? Skill { get; set; }
}

/// <summary>Nguồn của một dòng kỹ năng yêu cầu. Khớp ràng buộc CK_TASK_REQ_SOURCE.</summary>
public static class NguonKyNang
{
    public const string AI = "AI";
    public const string ThuCong = "MANUAL";
}
