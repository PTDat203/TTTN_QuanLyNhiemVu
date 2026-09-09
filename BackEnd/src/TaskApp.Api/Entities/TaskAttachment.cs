namespace TaskApp.Api.Entities;

/// <summary>
/// Bang TASK_ATTACHMENTS - metadata tep dinh kem.
/// <para>
/// Database chi luu ten/duong dan/kich thuoc, KHONG luu noi dung nhi phan cua tep.
/// <see cref="ReportId"/> rong  => tep dinh kem truc tiep cho nhiem vu.
/// <see cref="ReportId"/> co gia tri => tep la tai lieu cua mot bao cao.
/// </para>
/// Bang nay KHONG co quan he nao voi TASK_STATUS_LOOKUP.
/// </summary>
public class TaskAttachment
{
    /// <summary>Cot ID - khoa chinh.</summary>
    public long Id { get; set; }

    /// <summary>Cot TASK_ID - khoa ngoai toi TASKS.ID.</summary>
    public long TaskId { get; set; }

    /// <summary>Cot REPORT_ID - khoa ngoai toi TASK_REPORTS.ID, cho phep rong.</summary>
    public long? ReportId { get; set; }

    /// <summary>Cot FILE_NAME - ten tep goc do nguoi dung tai len.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Cot FILE_PATH - duong dan luu tep tren o dia may chu.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Cot FILE_TYPE - kieu MIME, vi du application/pdf.</summary>
    public string? FileType { get; set; }

    /// <summary>Cot FILE_SIZE - kich thuoc tep tinh bang byte.</summary>
    public long? FileSize { get; set; }

    /// <summary>Cot UPLOADED_BY - khoa ngoai toi USERS.ID, nguoi tai tep len.</summary>
    public long UploadedBy { get; set; }

    /// <summary>Cot UPLOADED_AT - thoi diem tai tep len.</summary>
    public DateTime UploadedAt { get; set; }

    // ----- Navigation property -----

    /// <summary>Nhiem vu chua tep dinh kem.</summary>
    public TaskItem? Task { get; set; }

    /// <summary>Bao cao chua tep dinh kem, co the rong.</summary>
    public TaskReport? Report { get; set; }

    /// <summary>Nguoi tai tep len.</summary>
    public User? Uploader { get; set; }
}
