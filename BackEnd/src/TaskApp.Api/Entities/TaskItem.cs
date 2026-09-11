namespace TaskApp.Api.Entities;

/// <summary>
/// Bang TASKS - nhiem vu, bang trung tam cua he thong.
/// <para>
/// Lop dat ten <c>TaskItem</c> chu KHONG phai <c>Task</c> vi <c>Task</c> trung ten voi
/// <see cref="System.Threading.Tasks.Task"/> - da bat <c>ImplicitUsings</c> nen
/// <c>System.Threading.Tasks</c> luon duoc using san, dat ten trung se gay nhap nhang
/// o moi chu ky ham <c>async Task</c>.
/// </para>
/// </summary>
public class TaskItem : IAuditable
{
    /// <summary>Cot ID - khoa chinh.</summary>
    public long Id { get; set; }

    /// <summary>Cot TITLE - tieu de nhiem vu.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Cot DESCRIPTION - kieu CLOB, mo ta chi tiet.</summary>
    public string? Description { get; set; }

    /// <summary>Cot CREATOR_ID - khoa ngoai toi USERS.ID, nguoi tao/giao nhiem vu.</summary>
    public long CreatorId { get; set; }

    /// <summary>
    /// Cot ASSIGNEE_ID - khoa ngoai toi USERS.ID, nguoi thuc hien.
    /// Cho phep rong vi nhiem vu o trang thai MOI_TAO chua duoc giao cho ai.
    /// </summary>
    public long? AssigneeId { get; set; }

    /// <summary>Cot PRIORITY - xem <see cref="Common.MucUuTien"/>: LOW / MEDIUM / HIGH.</summary>
    public string Priority { get; set; } = Common.MucUuTien.TrungBinh;

    /// <summary>
    /// Cot STATUS_CODE - khoa ngoai toi TASK_STATUS_LOOKUP.CODE,
    /// xem <see cref="Common.TrangThaiNhiemVu"/>.
    /// </summary>
    public string StatusCode { get; set; } = Common.TrangThaiNhiemVu.MoiTao;

    /// <summary>Cot START_DATE - kieu Oracle DATE. Dung DateTime, KHONG dung DateOnly.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Cot DUE_DATE - han hoan thanh, kieu Oracle DATE.</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>Cot CREATED_AT.</summary>
    /// <summary>Phòng ban phụ trách nhiệm vụ. Cột DEPARTMENT_ID.
    /// Do AI đoán từ nội dung khi tạo, người giao chỉnh lại được nếu đoán sai.</summary>
    public long? DepartmentId { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Cot UPDATED_AT.</summary>
    public DateTime UpdatedAt { get; set; }

    // ----- Navigation property -----

    /// <summary>Nguoi tao/giao nhiem vu (CREATOR_ID).</summary>
    public User? Creator { get; set; }

    /// <summary>Nguoi thuc hien nhiem vu (ASSIGNEE_ID), co the rong.</summary>
    public User? Assignee { get; set; }

    /// <summary>Dong danh muc trang thai tuong ung voi STATUS_CODE.</summary>
    public TaskStatusLookup? Status { get; set; }

    /// <summary>Lich su cap nhat tien do.</summary>
    public ICollection<TaskProgress> ProgressUpdates { get; set; } = new List<TaskProgress>();

    /// <summary>Cac lan gui bao cao ket qua.</summary>
    public ICollection<TaskReport> Reports { get; set; } = new List<TaskReport>();

    /// <summary>Cac tep dinh kem cua nhiem vu.</summary>
    public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();

    /// <summary>Phòng ban phụ trách.</summary>
    public Department? Department { get; set; }
}
