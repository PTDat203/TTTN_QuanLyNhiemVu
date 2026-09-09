namespace TaskApp.Api.Entities;

/// <summary>
/// Bang TASK_PROGRESS - lich su cap nhat tien do cua nhiem vu.
/// Moi lan cap nhat ghi them mot dong moi, KHONG ghi de dong cu.
/// Bang nay chi co CREATED_AT nen chi hien thuc <see cref="ICoNgayTao"/>.
/// </summary>
public class TaskProgress : ICoNgayTao
{
    /// <summary>Cot ID - khoa chinh.</summary>
    public long Id { get; set; }

    /// <summary>Cot TASK_ID - khoa ngoai toi TASKS.ID.</summary>
    public long TaskId { get; set; }

    /// <summary>Cot USER_ID - khoa ngoai toi USERS.ID, nguoi ghi nhan tien do.</summary>
    public long UserId { get; set; }

    /// <summary>Cot PROGRESS_PERCENT - phan tram hoan thanh, rang buoc 0..100.</summary>
    public int ProgressPercent { get; set; }

    /// <summary>Cot CONTENT - mo ta cong viec da lam, VARCHAR2(2000 CHAR).</summary>
    public string? Content { get; set; }

    /// <summary>Cot CREATED_AT.</summary>
    public DateTime CreatedAt { get; set; }

    // ----- Navigation property -----

    /// <summary>Nhiem vu duoc cap nhat tien do.</summary>
    public TaskItem? Task { get; set; }

    /// <summary>Nguoi ghi nhan tien do.</summary>
    public User? User { get; set; }
}
