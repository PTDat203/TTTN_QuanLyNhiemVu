namespace TaskApp.Api.Entities;

/// <summary>
/// Thuc the co cot CREATED_AT.
/// <para>
/// DDL da dat DEFAULT CURRENT_TIMESTAMP nhung EF Core luon gui gia tri cua thuoc tinh
/// xuong khi INSERT, nen van phai tu dat o tang ung dung.
/// Viec dat gia tri duoc lam tap trung trong <c>TaskDbContext.SaveChangesAsync</c>.
/// </para>
/// </summary>
public interface ICoNgayTao
{
    /// <summary>Thoi diem tao ban ghi (cot CREATED_AT).</summary>
    DateTime CreatedAt { get; set; }
}

/// <summary>
/// Thuc the co ca hai cot CREATED_AT va UPDATED_AT.
/// <para>
/// Oracle KHONG tu cap nhat UPDATED_AT khi UPDATE (DEFAULT chi chay luc INSERT),
/// nen <c>TaskDbContext.SaveChangesAsync</c> phai tu ghi gia tri moi.
/// </para>
/// </summary>
public interface IAuditable : ICoNgayTao
{
    /// <summary>Thoi diem sua ban ghi lan gan nhat (cot UPDATED_AT).</summary>
    DateTime UpdatedAt { get; set; }
}
