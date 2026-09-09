namespace TaskApp.Api.Entities;

/// <summary>
/// Bang TASK_STATUS_LOOKUP - danh muc trang thai cua NHIEM VU.
/// <para>
/// Khoa chinh la chuoi <see cref="Code"/>, KHONG phai so tu tang.
/// Trang thai cua BAO CAO nam rieng o <see cref="TaskReport.Status"/> voi rang buoc CHECK,
/// khong dung chung danh muc nay.
/// </para>
/// </summary>
public class TaskStatusLookup
{
    /// <summary>Cot CODE - khoa chinh, xem <see cref="Common.TrangThaiNhiemVu"/>.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Cot NAME - ten hien thi tieng Viet, vi du "Dang thuc hien".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Cot DESCRIPTION - mo ta y nghia trang thai.</summary>
    public string? Description { get; set; }

    /// <summary>Cot SORT_ORDER - thu tu hien thi tren giao dien, 1..6.</summary>
    public int? SortOrder { get; set; }

    // ----- Navigation property -----

    /// <summary>Cac nhiem vu dang o trang thai nay.</summary>
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
