using System.Text.Json.Serialization;

namespace QLNV.Core.Dtos;

/// <summary>
/// §5.8 H1 / §9.6 — body <c>POST /api/v1/ai/goi-y-nguoi-thuc-hien</c>.
/// </summary>
public sealed class GoiYRequest
{
    /// <summary>
    /// Ma nhiem vu DA TON TAI. BAT BUOC khi giao lai / them nguoi cho nhiem vu da co,
    /// vi §9.3 dieu 4 va 5 (loai nguoi da phan cong, loai nguoi da tung tu choi) chi tra
    /// cuu duoc khi biet ma nhiem vu. Null khi dang tao moi tren man M05.
    /// </summary>
    [JsonPropertyName("idnvchitiet")]
    public Guid? IdNvChiTiet { get; set; }

    /// <summary>Noi dung nhiem vu (hien chi dung de hien thi lai; §9.9 se dung cho so khop ngu nghia).</summary>
    [JsonPropertyName("noidung")]
    public string? NoiDung { get; set; }

    /// <summary>Linh vuc. Null =&gt; §9.5 "nhiem vu khong co linh vuc": S1 tinh tren TONG so nhiem vu.</summary>
    [JsonPropertyName("linhvuc")]
    public string? LinhVuc { get; set; }

    /// <summary>TRONGTAM / THUONGXUYEN / DOTXUAT.</summary>
    [JsonPropertyName("dokhan")]
    public string? DoKhan { get; set; }

    [JsonPropertyName("hanxulyth")]
    public DateOnly? HanXuLyTh { get; set; }

    /// <summary>§9.3 dieu 3 — pham vi don vi duoc phep giao. Rong = khong gioi han.</summary>
    [JsonPropertyName("phamViUnitCode")]
    public List<string> PhamViUnitCode { get; set; } = new();

    /// <summary>§9.3 dieu 4 — cac userid nguoi giao da chon hoac muon loai tru.</summary>
    [JsonPropertyName("loaiTru")]
    public List<Guid> LoaiTru { get; set; } = new();

    /// <summary>So ung vien can tra. Mac dinh 5; goi lai voi so lon hon cho nut "Xem them 5 nguoi".</summary>
    [JsonPropertyName("soLuong")]
    public int SoLuong { get; set; } = Constants.GioiHan.SoUngVienMacDinh;

    /// <summary>
    /// Thay the cho §9.3 dieu 4 khi nhiem vu CHUA co ma (dang tao moi tren luoi M05):
    /// danh sach userid da duoc gan o cac dong khac.
    /// </summary>
    [JsonPropertyName("daPhanCong")]
    public List<Guid> DaPhanCong { get; set; } = new();

    /// <summary>Thay the cho §9.3 dieu 5 khi nhiem vu chua co ma: danh sach userid da tung tu choi.</summary>
    [JsonPropertyName("daTuChoi")]
    public List<Guid> DaTuChoi { get; set; } = new();
}

/// <summary>§9.6 — response cua API H1.</summary>
public sealed class GoiYResponse
{
    /// <summary>Id lan goi y, chinh la khoa cua <c>AI_GOIY_LOG</c> (§4.8).</summary>
    [JsonPropertyName("goiyId")]
    public Guid GoiYId { get; set; }

    /// <summary>Phien ban bo trong so, vi du "v1.0" (§4.8 <c>phien_ban_trongso</c>).</summary>
    [JsonPropertyName("phienBanTrongSo")]
    public string PhienBanTrongSo { get; set; } = string.Empty;

    /// <summary>"DAY_DU" | "KHOI_TAO" (§9.5 dong cuoi).</summary>
    [JsonPropertyName("cheDo")]
    public string CheDo { get; set; } = Constants.CheDoGoiY.DayDu;

    /// <summary>Cac canh bao hien tren dau popup M06 (§9.5).</summary>
    [JsonPropertyName("canhBao")]
    public List<string> CanhBao { get; set; } = new();

    /// <summary>Danh sach ung vien, da sap xep giam dan theo diem; nguoi QUA_TAI luon o cuoi.</summary>
    [JsonPropertyName("ungVien")]
    public List<UngVienDto> UngVien { get; set; } = new();

    // --- BO SUNG ngoai §9.6, phuc vu M06 / M13 ---

    /// <summary>Ma nhiem vu duoc goi y (echo lai request).</summary>
    [JsonPropertyName("idnvchitiet")]
    public Guid? IdNvChiTiet { get; set; }

    /// <summary>Linh vuc luc goi y (echo lai request).</summary>
    [JsonPropertyName("linhvuc")]
    public string? LinhVuc { get; set; }

    /// <summary>Tong so ung vien qua duoc bo loc cung (§9.3), truoc khi cat theo <c>soLuong</c>.</summary>
    [JsonPropertyName("tongSoUngVien")]
    public int TongSoUngVien { get; set; }

    /// <summary>§9.3 dieu 4 + 5 co duoc ap dung khong (false = request khong du thong tin).</summary>
    [JsonPropertyName("daLocTrungLap")]
    public bool DaLocTrungLap { get; set; }

    /// <summary>Cac nguoi bi loai boi bo loc cung, kem ly do — huu ich khi go loi va giai trinh.</summary>
    [JsonPropertyName("loaiBo")]
    public List<UngVienBiLoaiDto> LoaiBo { get; set; } = new();
}

/// <summary>§9.6 — mot ung vien trong danh sach goi y.</summary>
public sealed class UngVienDto
{
    [JsonPropertyName("userid")]
    public Guid UserId { get; set; }

    [JsonPropertyName("fullname")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("chucvu")]
    public string? ChucVu { get; set; }

    [JsonPropertyName("unitname")]
    public string? UnitName { get; set; }

    /// <summary>Diem tong 0-100, lam tron 1 chu so. BANG dung tong 5 truong <c>gopPhan</c>.</summary>
    [JsonPropertyName("diemTong")]
    public double DiemTong { get; set; }

    /// <summary>He so tin cay 0-1 (§9.4). KHONG nhan vao diem, chi hien thi. &lt; 0,4 =&gt; nhan "Du lieu con it".</summary>
    [JsonPropertyName("doTinCay")]
    public double DoTinCay { get; set; }

    /// <summary>Cac nhan hien thi (§9.5 / §9.7).</summary>
    [JsonPropertyName("nhan")]
    public List<NhanDto> Nhan { get; set; } = new();

    /// <summary>Ban chuoi cua <see cref="Nhan"/> — tien cho FE in thang.</summary>
    [JsonPropertyName("nhanText")]
    public List<string> NhanText { get; set; } = new();

    /// <summary>5 thanh phan diem (§9.4 S1..S5).</summary>
    [JsonPropertyName("diemThanhPhan")]
    public DiemThanhPhanDto DiemThanhPhan { get; set; } = new();

    /// <summary>Toi da 4 dong ly do tieng Viet, thu tu uu tien chuyen mon -&gt; kinh nghiem -&gt; hieu qua -&gt; khoi luong (§9.7).</summary>
    [JsonPropertyName("lyDo")]
    public List<string> LyDo { get; set; } = new();

    /// <summary>So lieu tho de nguoi giao kiem chung tung con so (§9.1 "giai thich duoc").</summary>
    [JsonPropertyName("soLieu")]
    public SoLieuDto SoLieu { get; set; } = new();
}

/// <summary>§9.6 — 5 thanh phan diem, moi thanh phan gom diem / trong so / phan gop.</summary>
public sealed class DiemThanhPhanDto
{
    /// <summary>S1 — Chuyen mon, trong so mac dinh 0,30 (§9.4 S1).</summary>
    [JsonPropertyName("chuyenMon")]
    public ThanhPhanDto ChuyenMon { get; set; } = new();

    /// <summary>S2 — Lich su thuc hien, trong so mac dinh 0,20 (§9.4 S2).</summary>
    [JsonPropertyName("lichSu")]
    public ThanhPhanDto LichSu { get; set; } = new();

    /// <summary>S3 — Hieu qua cong viec, trong so mac dinh 0,25 (§9.4 S3).</summary>
    [JsonPropertyName("hieuQua")]
    public ThanhPhanDto HieuQua { get; set; } = new();

    /// <summary>S4 — Khoi luong hien tai, trong so mac dinh 0,20 (§9.4 S4).</summary>
    [JsonPropertyName("khoiLuong")]
    public ThanhPhanDto KhoiLuong { get; set; } = new();

    /// <summary>S5 — Tinh san sang, trong so mac dinh 0,05 (§9.4 S5).</summary>
    [JsonPropertyName("sanSang")]
    public ThanhPhanDto SanSang { get; set; } = new();

    /// <summary>Tong 5 phan gop — phai bang <c>UngVienDto.DiemTong</c>. Khong serialize.</summary>
    [JsonIgnore]
    public double TongGopPhan =>
        ChuyenMon.GopPhan + LichSu.GopPhan + HieuQua.GopPhan + KhoiLuong.GopPhan + SanSang.GopPhan;
}

/// <summary>
/// §9.6 — mot thanh phan diem.
/// RANG BUOC DA KIEM CHUNG: <c>GopPhan</c> phai tinh tu chinh con so <c>Diem</c> DA LAM TRON
/// (2 chu so), roi lam tron 1 chu so; nho vay tong 5 <c>GopPhan</c> bang <c>DiemTong</c>
/// va bang phan ra tren M06 cong lai dung ra tong.
/// </summary>
public sealed class ThanhPhanDto
{
    public ThanhPhanDto()
    {
    }

    public ThanhPhanDto(double diem, double trongSo, double gopPhan)
    {
        Diem = diem;
        TrongSo = trongSo;
        GopPhan = gopPhan;
    }

    /// <summary>Gia tri Si trong khoang [0, 1], lam tron 2 chu so.</summary>
    [JsonPropertyName("diem")]
    public double Diem { get; set; }

    /// <summary>Trong so wi dang ap dung (che do KHOI_TAO dung 0,6 / 0,4 cho S1 / S4 — §9.5).</summary>
    [JsonPropertyName("trongSo")]
    public double TrongSo { get; set; }

    /// <summary>100 x TrongSo x Diem, lam tron 1 chu so.</summary>
    [JsonPropertyName("gopPhan")]
    public double GopPhan { get; set; }
}

/// <summary>§9.6 — mot nhan hien thi kem ung vien (§9.5 / §9.7).</summary>
public sealed class NhanDto
{
    public NhanDto()
    {
    }

    public NhanDto(string ma, string nhan, string mau)
    {
        Ma = ma;
        Nhan = nhan;
        Mau = mau;
    }

    /// <summary>NGUOI_MOI | DU_LIEU_IT | QUA_TAI | CO_QUA_HAN | BI_TRA_LAI.</summary>
    [JsonPropertyName("ma")]
    public string Ma { get; set; } = string.Empty;

    [JsonPropertyName("nhan")]
    public string Nhan { get; set; } = string.Empty;

    /// <summary>"xam" | "vang" | "do" | "cam".</summary>
    [JsonPropertyName("mau")]
    public string Mau { get; set; } = string.Empty;
}

/// <summary>
/// §9.6 <c>soLieu</c> — cac con so tho dung de sinh ly do va de nguoi giao kiem chung.
/// </summary>
public sealed class SoLieuDto
{
    /// <summary>§9.4 S1 — so nhiem vu DA NGHIEM THU thuoc dung linh vuc L.</summary>
    [JsonPropertyName("soNvHoanThanhLinhVuc")]
    public int SoNvHoanThanhLinhVuc { get; set; }

    /// <summary>§9.4 S2/S3 — TONG so nhiem vu da nghiem thu (trangthai thuoc {1,5} VA truc B = 11).</summary>
    [JsonPropertyName("soNvHoanThanh")]
    public int SoNvHoanThanh { get; set; }

    /// <summary>§9.4 S3(a) — so nhiem vu co trangthai = 1 (dung han) va truc B = 11.</summary>
    [JsonPropertyName("soNvDungHan")]
    public int SoNvDungHan { get; set; }

    /// <summary>§9.4 S3(b) / §4.8 <c>so_nv_bi_tralai</c> — so lan bi cham CHUA DAT (truc B = 12).</summary>
    [JsonPropertyName("soNvBiTraLai")]
    public int SoNvBiTraLai { get; set; }

    /// <summary>§9.4 S3(c) / §4.8 <c>so_lan_giahan</c> — tong <c>solangiahan</c>.</summary>
    [JsonPropertyName("soLanGiaHan")]
    public int SoLanGiaHan { get; set; }

    /// <summary>§4.8 view <c>so_nv_dang_mo</c> — dang giu vai CHUTRI, trangthai NOT IN (1,5,97).</summary>
    [JsonPropertyName("soNvDangMo")]
    public int SoNvDangMo { get; set; }

    /// <summary>§4.8 view <c>tai_trong_so</c> — tong trong so do khan cua cac nhiem vu dang giu.</summary>
    [JsonPropertyName("taiTrongSo")]
    public double TaiTrongSo { get; set; }

    /// <summary>§4.8 view <c>so_nv_qua_han</c> / §9.4 S5 — so nhiem vu dang o trangthai = 7.</summary>
    [JsonPropertyName("soNvQuaHan")]
    public int SoNvQuaHan { get; set; }

    /// <summary>§4.7 <c>max_concurrent_tasks</c> — nguong tai K trong §9.4 S4.</summary>
    [JsonPropertyName("K")]
    public int K { get; set; } = Constants.GioiHan.MaxConcurrentTasksMacDinh;

    /// <summary>Ty le dung han = SoNvDungHan / SoNvHoanThanh (0 khi chua co du lieu).</summary>
    [JsonPropertyName("tyLeDungHan")]
    public double TyLeDungHan { get; set; }

    /// <summary>
    /// §9.6 giu khoa nay de FE khong in "undefined". LUON null o ban v1:
    /// §9.2 / §10.1 da bo bang khai bao nang luc, chuyen mon suy tu lich su.
    /// </summary>
    [JsonPropertyName("mucThanhThao")]
    public int? MucThanhThao { get; set; }

    /// <summary>
    /// §9.6 giu khoa nay de FE khong in "undefined". LUON null o ban v1:
    /// §9.4 ghi chu + §10.2 da loai <c>hsChatluong</c> khoi cong thuc.
    /// </summary>
    [JsonPropertyName("diemChatLuongTb")]
    public double? DiemChatLuongTb { get; set; }

    /// <summary>BO SUNG — tong so nhiem vu tung duoc giao voi vai CHUTRI. = 0 =&gt; nhan NGUOI_MOI.</summary>
    [JsonPropertyName("soNvDuocGiao")]
    public int SoNvDuocGiao { get; set; }
}

/// <summary>Mot nguoi bi bo loc cung §9.3 loai bo, kem ly do.</summary>
public sealed class UngVienBiLoaiDto
{
    [JsonPropertyName("userid")]
    public Guid UserId { get; set; }

    [JsonPropertyName("fullname")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("lyDo")]
    public string LyDo { get; set; } = string.Empty;
}

/// <summary>§5.8 H2 — body <c>POST /api/v1/ai/goi-y/{goiyId}/ket-qua</c>.</summary>
public sealed class GhiKetQuaGoiYRequest
{
    /// <summary>Userid nguoi giao thuc te da chon. Null = bo qua goi y, chon thu cong.</summary>
    [JsonPropertyName("useridDaChon")]
    public Guid? UserIdDaChon { get; set; }
}

/// <summary>
/// §9.8 — so lieu do hieu qua AI cho man M13 va bao cao thuc tap.
/// Moi ty le lam tron 2 chu so; khi so voi nguong muc tieu thi dung GIA TRI THO.
/// </summary>
public sealed class ThongKeAiDto
{
    /// <summary>Tong so lan mo popup goi y (so ban ghi AI_GOIY_LOG).</summary>
    [JsonPropertyName("soLanGoiY")]
    public int SoLanGoiY { get; set; }

    /// <summary>So lan nguoi giao chon tu danh sach goi y.</summary>
    [JsonPropertyName("soLanChapNhan")]
    public int SoLanChapNhan { get; set; }

    /// <summary>So lan bo qua goi y (dong gop 0 vao MRR).</summary>
    [JsonPropertyName("soLanBoQua")]
    public int SoLanBoQua { get; set; }

    /// <summary>§9.8 — so lan chon tu goi y / tong so lan mo popup. Nguong muc tieu &gt;= 0,70.</summary>
    [JsonPropertyName("tyLeChapNhan")]
    public double TyLeChapNhan { get; set; }

    /// <summary>§9.8 — so lan chon dung hang 1 / tong. Nguong muc tieu &gt;= 0,35.</summary>
    [JsonPropertyName("precision1")]
    public double Precision1 { get; set; }

    /// <summary>§9.8 — so lan nguoi duoc chon nam trong top-3 / tong. Nguong muc tieu &gt;= 0,60.</summary>
    [JsonPropertyName("precision3")]
    public double Precision3 { get; set; }

    /// <summary>§9.8 — trung binh 1 / thu_hang_da_chon. Nguong muc tieu &gt;= 0,50.</summary>
    [JsonPropertyName("mrr")]
    public double Mrr { get; set; }

    /// <summary>§9.8 — he so Gini cua phan bo <c>so_nv_dang_mo</c>. Cang thap cang deu tai.</summary>
    [JsonPropertyName("giniTai")]
    public double GiniTai { get; set; }

    /// <summary>Bieu do cot phan bo thu hang duoc chon (M13).</summary>
    [JsonPropertyName("phanBoThuHang")]
    public List<PhanBoThuHangDto> PhanBoThuHang { get; set; } = new();

    /// <summary>Bieu do phan bo diem tong cua moi ung vien tung duoc goi y (M13).</summary>
    [JsonPropertyName("phanBoDiem")]
    public List<PhanBoDiemDto> PhanBoDiem { get; set; } = new();

    /// <summary>Nguong muc tieu §9.8 de FE to mau dat / chua dat.</summary>
    [JsonPropertyName("nguong")]
    public NguongMucTieuDto Nguong { get; set; } = new();

    /// <summary>Co dat nguong hay khong, so bang gia tri THO (khong dung gia tri da lam tron).</summary>
    [JsonPropertyName("dat")]
    public DatNguongDto Dat { get; set; } = new();
}

/// <summary>§9.8 — mot cot cua bieu do phan bo thu hang.</summary>
public sealed class PhanBoThuHangDto
{
    /// <summary>Thu hang (1-based). Null = "Bo qua goi y".</summary>
    [JsonPropertyName("thuHang")]
    public int? ThuHang { get; set; }

    [JsonPropertyName("nhan")]
    public string Nhan { get; set; } = string.Empty;

    [JsonPropertyName("soLan")]
    public int SoLan { get; set; }
}

/// <summary>§9.8 — mot cot cua bieu do phan bo diem.</summary>
public sealed class PhanBoDiemDto
{
    /// <summary>Nhan khoang diem, vi du "0-20", "20-40".</summary>
    [JsonPropertyName("khoang")]
    public string Khoang { get; set; } = string.Empty;

    [JsonPropertyName("soLan")]
    public int SoLan { get; set; }
}

/// <summary>§9.8 — nguong muc tieu cua tung chi so.</summary>
public sealed class NguongMucTieuDto
{
    [JsonPropertyName("precision1")]
    public double Precision1 { get; set; } = 0.35;

    [JsonPropertyName("precision3")]
    public double Precision3 { get; set; } = 0.60;

    [JsonPropertyName("tyLeChapNhan")]
    public double TyLeChapNhan { get; set; } = 0.70;

    [JsonPropertyName("mrr")]
    public double Mrr { get; set; } = 0.50;
}

/// <summary>§9.8 — co dat nguong hay khong.</summary>
public sealed class DatNguongDto
{
    [JsonPropertyName("precision1")]
    public bool Precision1 { get; set; }

    [JsonPropertyName("precision3")]
    public bool Precision3 { get; set; }

    [JsonPropertyName("tyLeChapNhan")]
    public bool TyLeChapNhan { get; set; }

    [JsonPropertyName("mrr")]
    public bool Mrr { get; set; }
}

/// <summary>§9.8 — mot dong bang so sanh voi baseline (bat buoc co trong bao cao).</summary>
public sealed class SoSanhBaselineDto
{
    /// <summary>"MO_HINH_DAY_DU" | "NGAU_NHIEN" | "RANH_NHAT" | "CHUYEN_MON_CAO_NHAT".</summary>
    [JsonPropertyName("chienLuoc")]
    public string ChienLuoc { get; set; } = string.Empty;

    [JsonPropertyName("nhan")]
    public string Nhan { get; set; } = string.Empty;

    [JsonPropertyName("precision1")]
    public double Precision1 { get; set; }

    [JsonPropertyName("precision3")]
    public double Precision3 { get; set; }

    [JsonPropertyName("mrr")]
    public double Mrr { get; set; }

    [JsonPropertyName("giniTai")]
    public double GiniTai { get; set; }
}

/// <summary>
/// §5.8 H4 — bo trong so va hang so cua mo hinh cham diem.
/// Doc/sua qua <c>GET|PUT /api/v1/ai/cau-hinh</c>. Rang buoc: w1 + ... + w5 = 1,00.
/// </summary>
public sealed class CauHinhAiDto
{
    /// <summary>§9.2 S1 Chuyen mon — mac dinh 0,30.</summary>
    [JsonPropertyName("w1")]
    public double W1 { get; set; } = 0.30;

    /// <summary>§9.2 S2 Lich su thuc hien — mac dinh 0,20.</summary>
    [JsonPropertyName("w2")]
    public double W2 { get; set; } = 0.20;

    /// <summary>§9.2 S3 Hieu qua cong viec — mac dinh 0,25.</summary>
    [JsonPropertyName("w3")]
    public double W3 { get; set; } = 0.25;

    /// <summary>§9.2 S4 Khoi luong hien tai — mac dinh 0,20.</summary>
    [JsonPropertyName("w4")]
    public double W4 { get; set; } = 0.20;

    /// <summary>§9.2 S5 Tinh san sang — mac dinh 0,05.</summary>
    [JsonPropertyName("w5")]
    public double W5 { get; set; } = 0.05;

    /// <summary>§9.4 S1 — nguong <c>min(1, n / nguong)</c>, mac dinh 5.</summary>
    [JsonPropertyName("nguongChuyenMon")]
    public double NguongChuyenMon { get; set; } = 5;

    /// <summary>§9.4 S2 — nguong trong <c>ln(1+N) / ln(1+nguong)</c>, mac dinh 10.</summary>
    [JsonPropertyName("nguongLichSu")]
    public double NguongLichSu { get; set; } = 10;

    /// <summary>§9.4 S3 — tien nghiem ty le dung han toan he thong, mac dinh 0,70.</summary>
    [JsonPropertyName("p0")]
    public double P0 { get; set; } = 0.70;

    /// <summary>§9.4 S3 — so "quan sat ao" khi lam muot Laplace, mac dinh 5.</summary>
    [JsonPropertyName("alpha")]
    public double Alpha { get; set; } = 5;

    /// <summary>§9.4 S3(b) — he so phat bi tra lai, mac dinh 0,30.</summary>
    [JsonPropertyName("phatTraLai")]
    public double PhatTraLai { get; set; } = 0.30;

    /// <summary>§9.4 S3(c) — he so phat gia han, mac dinh 0,15.</summary>
    [JsonPropertyName("phatGiaHan")]
    public double PhatGiaHan { get; set; } = 0.15;

    /// <summary>§9.3 dieu 6 — nguong qua tai: tai &gt;= K x he so, mac dinh 1,5.</summary>
    [JsonPropertyName("heSoQuaTai")]
    public double HeSoQuaTai { get; set; } = 1.5;

    /// <summary>§4.8 <c>phien_ban_trongso</c> — mac dinh "v1.0".</summary>
    [JsonPropertyName("phienBan")]
    public string PhienBan { get; set; } = "v1.0";

    /// <summary>Tong 5 trong so — phai bang 1,00 (§9.2). Khong serialize.</summary>
    [JsonIgnore]
    public double TongTrongSo => W1 + W2 + W3 + W4 + W5;
}

/// <summary>§5.8 H4 — ket qua kiem tra tinh hop le cua bo cau hinh.</summary>
public sealed class KiemTraCauHinhAiDto
{
    [JsonPropertyName("tongTrongSo")]
    public double TongTrongSo { get; set; }

    [JsonPropertyName("hopLe")]
    public bool HopLe { get; set; }

    [JsonPropertyName("canhBao")]
    public List<string> CanhBao { get; set; } = new();
}

/// <summary>§3.4 M13 — mot dong nhat ky goi y AI.</summary>
public sealed class AiGoiYLogDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("idnvchitiet")]
    public Guid? IdNvChiTiet { get; set; }

    [JsonPropertyName("noiDungNhiemVu")]
    public string? NoiDungNhiemVu { get; set; }

    [JsonPropertyName("linhvuc")]
    public string? LinhVuc { get; set; }

    [JsonPropertyName("ungVien")]
    public List<UngVienTomTatLogDto> UngVien { get; set; } = new();

    [JsonPropertyName("useridDaChon")]
    public Guid? UserIdDaChon { get; set; }

    [JsonPropertyName("nguoiDaChonTen")]
    public string? NguoiDaChonTen { get; set; }

    [JsonPropertyName("thuHangDaChon")]
    public int? ThuHangDaChon { get; set; }

    [JsonPropertyName("coTrongGoiY")]
    public bool CoTrongGoiY { get; set; }

    [JsonPropertyName("phienBanTrongSo")]
    public string? PhienBanTrongSo { get; set; }

    [JsonPropertyName("cheDo")]
    public string? CheDo { get; set; }

    [JsonPropertyName("createdate")]
    public DateTime CreateDate { get; set; }
}

/// <summary>§3.4 M13 — mot ung vien da giai ma tu <c>ket_qua_json</c>.</summary>
public sealed class UngVienTomTatLogDto
{
    [JsonPropertyName("thuHang")]
    public int ThuHang { get; set; }

    [JsonPropertyName("userid")]
    public Guid UserId { get; set; }

    [JsonPropertyName("fullname")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("diemTong")]
    public double DiemTong { get; set; }

    [JsonPropertyName("doTinCay")]
    public double DoTinCay { get; set; }

    [JsonPropertyName("nhan")]
    public List<string> Nhan { get; set; } = new();
}
