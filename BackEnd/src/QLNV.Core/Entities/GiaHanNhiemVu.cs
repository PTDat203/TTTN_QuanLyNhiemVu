namespace QLNV.Core.Entities;

/// <summary>
/// §4.5 — bang <c>GIAHAN_NHIEMVU</c>. Goc: <c>GiaHanNhiemVuRequest/Response</c>.
///
/// §5.6 CANH BAO BAN GIAO: he goc FE khong co dong nao gan lai <c>hanxulyth</c> sau khi
/// duyet gia han (logic nam hoan toan o backend, khong kiem chung duoc). Hanh vi cua
/// app moi la DAC TA MOI: duyet =&gt; <c>hanxulyth = hanxulydexuat</c>, <c>solangiahan += 1</c>.
/// </summary>
public class GiaHanNhiemVu
{
    /// <summary>Khoa chinh. Cot <c>id</c>.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>FK toi <see cref="DmNhiemVuChiTiet"/>. BAT BUOC. Cot <c>idCtnv</c>.</summary>
    public Guid IdCtnv { get; set; }

    /// <summary>Ly do gia han. Cot <c>noiDung</c>, varchar(2000).</summary>
    public string? NoiDung { get; set; }

    /// <summary>
    /// Thoi han de xuat. BAT BUOC, phai muon hon <c>hanxulyth</c> hien tai (§3.3 M10).
    /// Cot <c>hanxulydexuat</c>, date.
    /// </summary>
    public DateOnly HanXuLyDeXuat { get; set; }

    /// <summary>Anh chup han truoc khi gia han. Cot <c>hanxulyth_cu</c>, date.</summary>
    public DateOnly? HanXuLyThCu { get; set; }

    /// <summary>
    /// 10 cho / 11 duyet / 12 tu choi (§2.3). Cot <c>trangThai</c>, int.
    /// </summary>
    public int TrangThai { get; set; } = Constants.TrangThaiGiaHan.ChoDuyet;

    /// <summary>Y kien nguoi duyet. Cot <c>phanhoi</c>, varchar(2000).</summary>
    public string? PhanHoi { get; set; }

    /// <summary>Nguoi de xuat gia han. BAT BUOC. Cot <c>useridDexuat</c>.</summary>
    public Guid UserIdDeXuat { get; set; }

    /// <summary>Nguoi duyet. Cot <c>useridDuyet</c>.</summary>
    public Guid? UserIdDuyet { get; set; }

    /// <summary>Thoi diem tao de xuat. BAT BUOC. Cot <c>createDate</c>, datetime.</summary>
    public DateTime CreateDate { get; set; }

    /// <summary>Thoi diem duyet / tu choi. Cot <c>ngayduyet</c>, datetime.</summary>
    public DateTime? NgayDuyet { get; set; }

    /// <summary>
    /// BO SUNG ngoai §4.5 (co trong ban flow.js da kiem chung, truong <c>trangthai_cu</c>):
    /// nho lai truc A ngay TRUOC khi chuyen sang 13, de T12/T13 khoi phuc chinh xac.
    /// Dac biet quan trong khi truoc do la 3 (chua tiep nhan) — neu khong nho, nhiem vu se
    /// nhay sang 2 "Dang trien khai" ma bo qua buoc Tiep nhan (§2.4 T2) va khong co
    /// <c>ngaytiepnhan</c>. Cot <c>trangthai_cu</c>, int.
    /// </summary>
    public int? TrangThaiCu { get; set; }

    // --- Dieu huong ---

    /// <summary>Nhiem vu tuong ung.</summary>
    public DmNhiemVuChiTiet? NhiemVu { get; set; }

    /// <summary>True khi de xuat con dang cho duyet (§2.3 = 10).</summary>
    public bool DangChoDuyet => TrangThai == Constants.TrangThaiGiaHan.ChoDuyet;
}
