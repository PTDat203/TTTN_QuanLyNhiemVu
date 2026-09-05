using QLNV.Core.Entities;

namespace QLNV.Infrastructure.Data.Seed;

/// <summary>
/// Ket qua cua <see cref="BoSinhDuLieuMau"/> — toan bo entity da dung san,
/// CHUA ghi vao CSDL. Thu tu chen bat buoc: don vi -&gt; linh vuc -&gt; tu dien -&gt;
/// nguoi dung -&gt; van ban -&gt; nhiem vu -&gt; phan cong / xu ly / gia han -&gt; nhat ky AI.
/// </summary>
public sealed class DuLieuMau
{
    /// <summary>Ngay lam moc khi sinh du lieu (moi han xu ly deu tinh quanh moc nay).</summary>
    public DateOnly HomNay { get; init; }

    public List<SysUnit> DonVi { get; } = new();

    public List<DmLinhVuc> LinhVuc { get; } = new();

    public List<DmTuDien> TuDien { get; } = new();

    public List<SysUser> NguoiDung { get; } = new();

    public List<DmVanBan> VanBan { get; } = new();

    public List<DmNhiemVuChiTiet> NhiemVu { get; } = new();

    public List<NhiemVuPhanCong> PhanCong { get; } = new();

    public List<XuLyNhiemVu> XuLy { get; } = new();

    public List<GiaHanNhiemVu> GiaHan { get; } = new();

    public List<AiGoiYLog> AiLog { get; } = new();

    /// <summary>Tom tat so luong tung bang — tien de ghi ra nhat ky khi seed.</summary>
    public string TomTat() =>
        $"{DonVi.Count} đơn vị, {LinhVuc.Count} lĩnh vực, {NguoiDung.Count} người dùng, " +
        $"{TuDien.Count} mục từ điển, {VanBan.Count} văn bản, {NhiemVu.Count} nhiệm vụ, " +
        $"{PhanCong.Count} phân công, {XuLy.Count} bản ghi xử lý, {GiaHan.Count} đề xuất gia hạn, " +
        $"{AiLog.Count} bản ghi nhật ký AI.";
}
