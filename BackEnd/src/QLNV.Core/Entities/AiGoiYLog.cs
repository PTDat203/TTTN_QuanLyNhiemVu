namespace QLNV.Core.Entities;

/// <summary>
/// §4.8 — bang <c>AI_GOIY_LOG</c> (nhat ky goi y).
///
/// "Day la bang quan trong nhat cho phan bao ve": tu no tinh ra ty le chap nhan goi y,
/// Precision@1/@3, MRR (§9.8). Khong co bang nay thi khong co so lieu de bao cao.
/// </summary>
public class AiGoiYLog
{
    /// <summary>Khoa chinh — chinh la <c>goiyId</c> tra ve cho FE o §9.6. Cot <c>id</c>.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Nhiem vu duoc goi y. Cot <c>idnvchitiet</c>.
    /// Null khi goi y cho dong nhiem vu DANG TAO MOI (chua co ma) — §3.2 M05/M06.
    /// </summary>
    public Guid? IdNvChiTiet { get; set; }

    /// <summary>Linh vuc luc goi y. Cot <c>linhvuc</c>, varchar(50).</summary>
    public string? LinhVuc { get; set; }

    /// <summary>
    /// Top-N ung vien + diem + diem thanh phan, luu dang JSON. Cot <c>ket_qua_json</c>, nvarchar(max).
    /// Cau truc moi phan tu: { thuHang, userid, fullname, diemTong, doTinCay, diemThanhPhan, nhan[] }.
    /// </summary>
    public string? KetQuaJson { get; set; }

    /// <summary>Nguoi giao thuc te da chon ai. Null neu bo qua goi y. Cot <c>userid_da_chon</c>.</summary>
    public Guid? UserIdDaChon { get; set; }

    /// <summary>
    /// Thu hang cua nguoi duoc chon trong danh sach goi y, dem tu 1. Null neu bo qua.
    /// Cot <c>thu_hang_da_chon</c> (§5.8 H2). Dung cho MRR va Precision@k (§9.8).
    /// </summary>
    public int? ThuHangDaChon { get; set; }

    /// <summary>
    /// Nguoi duoc chon co nam trong TOP-5 khong. Cot <c>co_trong_goi_y</c>, bit.
    /// CHOT CUNG o 5, khong phu thuoc <c>soLuong</c> cua request — de Precision@3,
    /// ty le chap nhan va MRR so sanh duoc giua cac lan goi (kien quyet giu tu ban ai-engine).
    /// </summary>
    public bool CoTrongGoiY { get; set; }

    /// <summary>
    /// Phien ban bo trong so luc goi y (vi du "v1.0"). Cot <c>phien_ban_trongso</c>.
    /// Dac ta ghi kieu int; dung CHUOI de con ghi duoc "v1.0"/"v1.1" nhu §9.6 tra ve.
    /// </summary>
    public string? PhienBanTrongSo { get; set; }

    /// <summary>Thoi diem goi y. Cot <c>createdate</c>.</summary>
    public DateTime CreateDate { get; set; }

    /// <summary>Nguoi giao da bam nut goi y. BO SUNG ngoai §4.8 — huu ich cho man M13.</summary>
    public Guid? UserIdGoiY { get; set; }

    /// <summary>Che do cham diem luc goi y: DAY_DU / KHOI_TAO (§9.5). BO SUNG ngoai §4.8.</summary>
    public string? CheDo { get; set; }

    /// <summary>So ung vien duoc tra ve trong lan goi y nay. BO SUNG ngoai §4.8.</summary>
    public int SoUngVien { get; set; }
}
