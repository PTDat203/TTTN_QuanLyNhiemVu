namespace QLNV.Core.Entities;

/// <summary>
/// §4.1 — bang <c>DM_VANBAN</c> (Van ban chi dao). Goc: <c>DmNhiemvuModel</c>.
///
/// §7.4 muc 1: ten COT trong CSDL giu nguyen chu thuong nhu he goc
/// (sokyhieu, trichyeu, loaivb, ngaybanhanh, coquanbanhanh, dokhan, linhvuc,
/// thoigianchidao, nguonnv, nguoitheodoi, unitcode, useridcreate, createdate).
/// Anh xa bang <c>HasColumnName</c> o tang Infrastructure.
///
/// LUOC BO so voi goc (§4.1): domat, isencrypt, password, idvbden, isbutphechidao,
/// isvbdexuat, capduyetfinal, usertiepnhan, vanbanuutien, appid, idnvchitietgoc, dmNhiemvu2[].
/// </summary>
public class DmVanBan
{
    /// <summary>Khoa chinh. Cot <c>id</c>.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>So ky hieu van ban. Cot <c>sokyhieu</c>, varchar(100).</summary>
    public string? SoKyHieu { get; set; }

    /// <summary>Tom tat noi dung chi dao. BAT BUOC. Cot <c>trichyeu</c>, varchar(2000).</summary>
    public string TrichYeu { get; set; } = string.Empty;

    /// <summary>Loai van ban — FK logic toi DM_TUDIEN type LOAIVB. Cot <c>loaivb</c>, varchar(50).</summary>
    public string? LoaiVb { get; set; }

    /// <summary>Ngay ban hanh. Cot <c>ngaybanhanh</c>, date.</summary>
    public DateOnly? NgayBanHanh { get; set; }

    /// <summary>Co quan ban hanh. Cot <c>coquanbanhanh</c>, varchar(500).</summary>
    public string? CoQuanBanHanh { get; set; }

    /// <summary>
    /// Do khan. BAT BUOC. TRONGTAM / THUONGXUYEN / DOTXUAT (§7.4 muc 4).
    /// Cot <c>dokhan</c>, varchar(50).
    /// </summary>
    public string DoKhan { get; set; } = Constants.DoKhan.ThuongXuyen;

    /// <summary>Linh vuc — FK logic toi DM_LINHVUC. Cot <c>linhvuc</c>, varchar(50).</summary>
    public string? LinhVuc { get; set; }

    /// <summary>Thoi gian chi dao, mac dinh = ngay tao. Cot <c>thoigianchidao</c>, datetime.</summary>
    public DateTime? ThoiGianChiDao { get; set; }

    /// <summary>Co quan / don vi giao nhiem vu. Cot <c>nguonnv</c>, varchar(200).</summary>
    public string? NguonNv { get; set; }

    /// <summary>CSV userid lanh dao phu trach (giu dung dang CSV nhu goc). Cot <c>nguoitheodoi</c>, varchar(500).</summary>
    public string? NguoiTheoDoi { get; set; }

    /// <summary>Don vi so huu ban ghi. BAT BUOC. Cot <c>unitcode</c>, varchar(50).</summary>
    public string UnitCode { get; set; } = string.Empty;

    /// <summary>Nguoi tao. BAT BUOC. Cot <c>useridcreate</c>.</summary>
    public Guid UserIdCreate { get; set; }

    /// <summary>Thoi diem tao. BAT BUOC. Cot <c>createdate</c>.</summary>
    public DateTime CreateDate { get; set; }

    /// <summary>Thoi diem cap nhat gan nhat. Cot <c>updatedate</c>.</summary>
    public DateTime? UpdateDate { get; set; }

    // --- Dieu huong (§4.9) ---

    /// <summary>Cac nhiem vu chi tiet thuoc van ban nay (1 - n).</summary>
    public ICollection<DmNhiemVuChiTiet> NhiemVuChiTiet { get; set; } = new List<DmNhiemVuChiTiet>();
}
