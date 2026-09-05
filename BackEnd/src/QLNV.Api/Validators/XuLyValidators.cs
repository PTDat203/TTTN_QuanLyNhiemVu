using FluentValidation;
using QLNV.Core.Constants;
using QLNV.Core.Dtos;

namespace QLNV.Api.Validators;

/// <summary>§5.4 D1 — body tiep nhan (ghi chu KHONG bat buoc).</summary>
public sealed class TiepNhanRequestValidator : AbstractValidator<TiepNhanRequest>
{
    public TiepNhanRequestValidator()
    {
        RuleFor(x => x.NoiDung)
            .MaximumLength(GioiHan.DoDaiNoiDung)
            .WithMessage("Ghi chú tiếp nhận không được vượt quá " + GioiHan.DoDaiNoiDung + " ký tự.");
    }
}

/// <summary>
/// §5.4 D2 — body tu choi nhiem vu. §10.7: he goc KHONG bat buoc ly do,
/// app moi BAT BUOC (yeu cau moi, ghi ro trong tai lieu ban giao).
/// </summary>
public sealed class TuChoiRequestValidator : AbstractValidator<TuChoiRequest>
{
    public TuChoiRequestValidator()
    {
        RuleFor(x => x.LyDo)
            .NotEmpty().WithMessage("Lý do từ chối nhiệm vụ là bắt buộc.")
            .MaximumLength(GioiHan.DoDaiNoiDung)
            .WithMessage("Lý do từ chối không được vượt quá " + GioiHan.DoDaiNoiDung + " ký tự.");
    }
}

/// <summary>§5.4 D3 — body cap nhat tien do. §10.2: goc khong validate 0-100, app moi BAT BUOC.</summary>
public sealed class TienDoRequestValidator : AbstractValidator<TienDoRequest>
{
    public TienDoRequestValidator()
    {
        RuleFor(x => x.MucDoHt)
            .InclusiveBetween(GioiHan.MucDoHtMin, GioiHan.MucDoHtMax)
            .WithMessage("Mức độ hoàn thành phải là số nguyên trong khoảng "
                         + GioiHan.MucDoHtMin + " đến " + GioiHan.MucDoHtMax + ".");

        RuleFor(x => x.NoiDung)
            .MaximumLength(GioiHan.DoDaiNoiDung)
            .WithMessage("Nội dung cập nhật không được vượt quá " + GioiHan.DoDaiNoiDung + " ký tự.");
    }
}

/// <summary>
/// §5.4 D4 — body gui bao cao ket qua.
/// LUU Y: viec <c>trangthai</c> co nam trong danh sach da loc theo han hay khong
/// KHONG kiem o day (validator khong biet <c>hanxulyth</c>); may trang thai kiem lai
/// bang <c>HanUtil.HopLeKhiBaoCao</c> (§5.4 D4, §6.4).
/// </summary>
public sealed class BaoCaoRequestValidator : AbstractValidator<BaoCaoRequest>
{
    public BaoCaoRequestValidator()
    {
        RuleFor(x => x.TrangThai)
            .Must(m => TrangThaiNv.HopLe(m))
            .WithMessage("Trạng thái báo cáo không hợp lệ.");

        // §1.2 buoc 5 — bat buoc noi dung khi bao cao HOAN THANH (1) hoac HOAN THANH SAU HAN (5).
        RuleFor(x => x.NoiDung)
            .NotEmpty()
            .When(x => x.TrangThai == TrangThaiNv.HoanThanh || x.TrangThai == TrangThaiNv.HoanThanhSauHan)
            .WithMessage("Phải nhập nội dung báo cáo khi chọn trạng thái hoàn thành.");

        RuleFor(x => x.NoiDung)
            .MaximumLength(GioiHan.DoDaiNoiDung)
            .WithMessage("Nội dung báo cáo không được vượt quá " + GioiHan.DoDaiNoiDung + " ký tự.");

        RuleFor(x => x.MucDoHt)
            .InclusiveBetween(GioiHan.MucDoHtMin, GioiHan.MucDoHtMax)
            .When(x => x.MucDoHt.HasValue)
            .WithMessage("Mức độ hoàn thành phải là số nguyên trong khoảng "
                         + GioiHan.MucDoHtMin + " đến " + GioiHan.MucDoHtMax + ".");
    }
}

/// <summary>§5.4 D6 — body thu hoi bao cao (ly do KHONG bat buoc).</summary>
public sealed class ThuHoiBaoCaoRequestValidator : AbstractValidator<ThuHoiBaoCaoRequest>
{
    public ThuHoiBaoCaoRequestValidator()
    {
        RuleFor(x => x.LyDo)
            .MaximumLength(GioiHan.DoDaiNoiDung)
            .WithMessage("Lý do thu hồi báo cáo không được vượt quá " + GioiHan.DoDaiNoiDung + " ký tự.");
    }
}

/// <summary>
/// §5.5 E1 — body nghiem thu. <c>phanHoi</c> BAT BUOC (§10.7 — yeu cau moi);
/// <c>hsChatluong</c> 1-6 va CHI ghi khi ket qua = DAT.
/// </summary>
public sealed class NghiemThuRequestValidator : AbstractValidator<NghiemThuRequest>
{
    public NghiemThuRequestValidator()
    {
        RuleFor(x => x.KetQua)
            .NotEmpty().WithMessage("Kết quả kiểm tra là bắt buộc.")
            .Must(v => KetQuaNghiemThu.HopLe(v))
            .WithMessage("Kết quả kiểm tra chỉ nhận giá trị DAT hoặc CHUA_DAT.");

        RuleFor(x => x.PhanHoi)
            .NotEmpty().WithMessage("Nội dung phản hồi khi kiểm tra kết quả là bắt buộc.")
            .MaximumLength(GioiHan.DoDaiNoiDung)
            .WithMessage("Nội dung phản hồi không được vượt quá " + GioiHan.DoDaiNoiDung + " ký tự.");

        RuleFor(x => x.HsChatLuong)
            .InclusiveBetween(GioiHan.HsChatLuongMin, GioiHan.HsChatLuongMax)
            .When(x => x.HsChatLuong.HasValue)
            .WithMessage("Điểm chất lượng phải là số nguyên trong khoảng "
                         + GioiHan.HsChatLuongMin + " đến " + GioiHan.HsChatLuongMax + ".");

        RuleFor(x => x.HsChatLuong)
            .Null().When(x => x.KetQua == KetQuaNghiemThu.ChuaDat)
            .WithMessage("Chỉ chấm điểm chất lượng khi kết quả nghiệm thu là ĐẠT.");
    }
}

/// <summary>§2.4 T4/T5 — body nguoi giao xu ly de nghi tu choi (MO RONG so voi §5).</summary>
public sealed class XuLyTuChoiRequestValidator : AbstractValidator<XuLyTuChoiRequest>
{
    public XuLyTuChoiRequestValidator()
    {
        RuleFor(x => x.KetQua)
            .NotEmpty().WithMessage("Kết quả xử lý đề nghị từ chối là bắt buộc.")
            .Must(v => KetQuaXuLyTuChoi.HopLe(v))
            .WithMessage("Kết quả xử lý chỉ nhận giá trị CHAP_NHAN hoặc BAC_BO.");

        RuleFor(x => x.PhanHoi)
            .MaximumLength(GioiHan.DoDaiNoiDung)
            .WithMessage("Ý kiến người giao không được vượt quá " + GioiHan.DoDaiNoiDung + " ký tự.");
    }
}
