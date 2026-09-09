namespace TaskApp.Api.Entities;

/// <summary>
/// Bang TASK_REPORTS - bao cao ket qua va ket luan xac nhan/tu choi cua nguoi giao.
/// <para>
/// Mot nhiem vu co the co nhieu bao cao neu bao cao truoc bi tu choi.
/// Trang thai bao cao (<see cref="Status"/>) dung rang buoc CHECK, KHONG dung
/// danh muc TASK_STATUS_LOOKUP.
/// </para>
/// </summary>
public class TaskReport : ICoNgayTao
{
    /// <summary>Cot ID - khoa chinh.</summary>
    public long Id { get; set; }

    /// <summary>Cot TASK_ID - khoa ngoai toi TASKS.ID.</summary>
    public long TaskId { get; set; }

    /// <summary>Cot REPORTER_ID - khoa ngoai toi USERS.ID, nguoi gui bao cao.</summary>
    public long ReporterId { get; set; }

    /// <summary>Cot CONTENT - kieu CLOB, noi dung bao cao, bat buoc.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Cot STATUS - xem <see cref="Common.TrangThaiBaoCao"/>:
    /// CHO_XAC_NHAN / DA_XAC_NHAN / TU_CHOI.
    /// </summary>
    public string Status { get; set; } = Common.TrangThaiBaoCao.ChoXacNhan;

    /// <summary>Cot REVIEWER_ID - khoa ngoai toi USERS.ID, nguoi duyet. Rong khi chua duyet.</summary>
    public long? ReviewerId { get; set; }

    /// <summary>Cot REVIEW_NOTE - kieu CLOB, ghi chu danh gia cua nguoi duyet.</summary>
    public string? ReviewNote { get; set; }

    /// <summary>Cot CREATED_AT - thoi diem gui bao cao.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Cot REVIEWED_AT - thoi diem duyet. Rong khi chua duyet.</summary>
    public DateTime? ReviewedAt { get; set; }

    // ----- Navigation property -----

    /// <summary>Nhiem vu duoc bao cao.</summary>
    public TaskItem? Task { get; set; }

    /// <summary>Nguoi gui bao cao (REPORTER_ID).</summary>
    public User? Reporter { get; set; }

    /// <summary>Nguoi duyet bao cao (REVIEWER_ID), co the rong.</summary>
    public User? Reviewer { get; set; }

    /// <summary>Cac tep dinh kem thuoc bao cao nay.</summary>
    public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
}
