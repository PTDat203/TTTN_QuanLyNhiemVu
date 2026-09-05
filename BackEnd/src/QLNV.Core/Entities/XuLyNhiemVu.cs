namespace QLNV.Core.Entities;

/// <summary>
/// §4.4 — bang <c>XULY_NHIEMVU</c> (lich su xu ly / bao cao). Goc: <c>XulyNhiemvuModel</c>.
///
/// CAI TIEN QUAN TRONG (§4.4): he goc KHONG luu vet <c>mucdoht</c> theo thoi gian.
/// App moi luu anh chup % moi lan bao cao => ve duoc duong tien do va la dau vao cho
/// dac trung "toc do trien khai" cua AI.
/// </summary>
public class XuLyNhiemVu
{
    /// <summary>Khoa chinh. Cot <c>id</c>.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>FK toi <see cref="DmNhiemVuChiTiet"/>. BAT BUOC. Cot <c>idCtnv</c>.</summary>
    public Guid IdCtnv { get; set; }

    /// <summary>
    /// Loai ban ghi (xem <see cref="Constants.LoaiXuLy"/>). BAT BUOC. Cot <c>loai</c>, varchar(20).
    /// Cai tien so voi goc — goc khong phan loai.
    /// </summary>
    public string Loai { get; set; } = string.Empty;

    /// <summary>
    /// Noi dung. Cot <c>noidung</c>, varchar(2000).
    /// BAT BUOC khi <c>loai = BAOCAO</c> va <c>trangthai</c> thuoc {1,5}; bat buoc khi <c>loai = TUCHOI</c>.
    /// </summary>
    public string? NoiDung { get; set; }

    /// <summary>Anh chup % hoan thanh tai thoi diem ghi. Cot <c>mucdoht</c>, int.</summary>
    public int? MucDoHt { get; set; }

    /// <summary>Trang thai nhiem vu (truc A) duoc chon khi bao cao. Cot <c>trangthai</c>, int.</summary>
    public int? TrangThai { get; set; }

    /// <summary>= 10 khi gui bao cao (§4.4). Cot <c>trangthaiXuly</c>, int.</summary>
    public int? TrangThaiXuLy { get; set; }

    /// <summary>
    /// Anh chup truc B tai thoi diem ghi. Cot <c>trangthaiDvXuly</c>, int.
    /// BO SUNG ngoai §4.4: can thiet de dem "so lan bi tra lai" cho §9.4 S3(b)
    /// (<c>so_nv_bi_tralai</c> cua §4.8 dem ban ghi XULY_NHIEMVU co truc B = 12).
    /// </summary>
    public int? TrangThaiDvXuly { get; set; }

    /// <summary>Nguoi thuc hien thao tac. BAT BUOC. Cot <c>useridXuly</c>.</summary>
    public Guid UserIdXuLy { get; set; }

    /// <summary>Thoi diem xu ly. BAT BUOC. Cot <c>ngayxuly</c>, datetime.</summary>
    public DateTime NgayXuLy { get; set; }

    // --- Dieu huong ---

    /// <summary>Nhiem vu tuong ung.</summary>
    public DmNhiemVuChiTiet? NhiemVu { get; set; }

    /// <summary>Nguoi thuc hien thao tac.</summary>
    public SysUser? NguoiXuLy { get; set; }
}
