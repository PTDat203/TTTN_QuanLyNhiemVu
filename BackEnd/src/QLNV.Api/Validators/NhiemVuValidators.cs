using FluentValidation;
using QLNV.Core.Constants;
using QLNV.Core.Dtos;
using MaDoKhan = QLNV.Core.Constants.DoKhan;

namespace QLNV.Api.Validators;

/// <summary>
/// §5.3 C1 — kiem tra body tao + giao nhieu nhiem vu.
/// Cac rang buoc §4.3 (moi dong >= 1 chu tri, khong ai vua CHUTRI vua PHOIHOP) duoc kiem
/// O CA HAI NOI: validator nay VA trong controller (§6.4 — khong tin FE, kiem lai o server).
/// </summary>
public sealed class TaoNhiemVuRequestValidator : AbstractValidator<TaoNhiemVuRequest>
{
    public TaoNhiemVuRequestValidator()
    {
        RuleFor(x => x.Items)
            .NotNull().WithMessage("Danh sách nhiệm vụ là bắt buộc.")
            .Must(x => x != null && x.Count > 0).WithMessage("Phải có ít nhất 1 dòng nhiệm vụ.");

        RuleForEach(x => x.Items).SetValidator(new TaoNhiemVuItemValidator());
    }
}

/// <summary>§5.3 C1 — mot dong trong luoi phan cong (§1.2 buoc 2, §4.2, §4.3).</summary>
public sealed class TaoNhiemVuItemValidator : AbstractValidator<TaoNhiemVuItem>
{
    public TaoNhiemVuItemValidator()
    {
        // §1.2 buoc 2 — noi dung BAT BUOC, <= 2000 ky tu.
        RuleFor(x => x.NoiDung)
            .NotEmpty().WithMessage("Nội dung nhiệm vụ là bắt buộc.")
            .MaximumLength(GioiHan.DoDaiNoiDung)
            .WithMessage("Nội dung nhiệm vụ không được vượt quá " + GioiHan.DoDaiNoiDung + " ký tự.");

        // §4.2 — dokhan BAT BUOC (dau vao trong so tai cua AI, §9.4 S4).
        RuleFor(x => x.DoKhan)
            .NotEmpty().WithMessage("Độ khẩn của nhiệm vụ là bắt buộc.")
            .Must(v => MaDoKhan.HopLe(v))
            .WithMessage("Độ khẩn chỉ nhận một trong các giá trị: TRONGTAM, THUONGXUYEN, DOTXUAT.");

        // §1.2 buoc 3 / §4.3 — moi dong PHAI co it nhat 1 chu tri.
        RuleFor(x => x.ChuTri)
            .NotNull().WithMessage("Danh sách chủ trì là bắt buộc.")
            .Must(x => x != null && x.Any(u => u != Guid.Empty))
            .WithMessage("Mỗi dòng nhiệm vụ phải có ít nhất 1 người/đơn vị chủ trì.");

        // §4.3 — mot nguoi khong duoc vua CHUTRI vua PHOIHOP tren cung 1 nhiem vu.
        RuleFor(x => x)
            .Must(x => x.ChuTri == null || x.PhoiHop == null || !x.ChuTri.Intersect(x.PhoiHop).Any())
            .WithMessage("Một người không được vừa là chủ trì vừa là phối hợp trên cùng một nhiệm vụ.");

        RuleFor(x => x.SoNgayHxlTh)
            .GreaterThanOrEqualTo(0).When(x => x.SoNgayHxlTh.HasValue)
            .WithMessage("Số ngày thực hiện không được là số âm.");
    }
}

/// <summary>§5.3 C2 — body do trung noi dung.</summary>
public sealed class KiemTraTrungRequestValidator : AbstractValidator<KiemTraTrungRequest>
{
    public KiemTraTrungRequestValidator()
    {
        RuleFor(x => x.Items).NotNull().WithMessage("Danh sách dòng cần dò trùng là bắt buộc.");

        RuleForEach(x => x.Items).ChildRules(dong =>
        {
            dong.RuleFor(x => x.NoiDung)
                .MaximumLength(GioiHan.DoDaiNoiDung)
                .WithMessage("Nội dung nhiệm vụ không được vượt quá " + GioiHan.DoDaiNoiDung + " ký tự.");
        });
    }
}

/// <summary>§5.3 C3 — tham so loc danh sach nhiem vu.</summary>
public sealed class NhiemVuLocRequestValidator : AbstractValidator<NhiemVuLocRequest>
{
    public NhiemVuLocRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Số trang phải lớn hơn hoặc bằng 1.");

        RuleFor(x => x.Size)
            .InclusiveBetween(1, GioiHan.KichThuocTrangToiDa)
            .WithMessage("Kích thước trang phải nằm trong khoảng 1 đến " + GioiHan.KichThuocTrangToiDa + ".");

        RuleFor(x => x.VaiTro)
            .Must(v => BoLocVaiTro.HopLe(v)).When(x => !string.IsNullOrEmpty(x.VaiTro))
            .WithMessage("Tham số vaiTro chỉ nhận giá trị TOI_GIAO hoặc TOI_LAM.");

        RuleFor(x => x.DoKhan)
            .Must(v => MaDoKhan.HopLe(v)).When(x => !string.IsNullOrEmpty(x.DoKhan))
            .WithMessage("Độ khẩn chỉ nhận một trong các giá trị: TRONGTAM, THUONGXUYEN, DOTXUAT.");

        // §2.1 — chi cac ma con dung trong app nho: 1,2,3,5,6,7,13,97 (khong co 4, 8, 100).
        RuleFor(x => x.TrangThai)
            .Must(ds => ds == null || ds.All(m => TrangThaiNv.HopLe(m)))
            .WithMessage("Danh sách trạng thái nhiệm vụ chứa mã không hợp lệ.");

        // §2.2 — truc B chi con 10, 11, 12 (ma 14 da bo).
        RuleFor(x => x.TrangThaiDvXuly)
            .Must(ds => ds == null || ds.All(m => TrangThaiPh.ToanBo().Contains(m)))
            .WithMessage("Danh sách trạng thái phản hồi chứa mã không hợp lệ (chỉ nhận 10, 11, 12).");
    }
}

/// <summary>§5.3 C5 — body sua nhiem vu (chi khi <c>trangthai = 3</c>).</summary>
public sealed class SuaNhiemVuRequestValidator : AbstractValidator<SuaNhiemVuRequest>
{
    public SuaNhiemVuRequestValidator()
    {
        RuleFor(x => x.NoiDung)
            .NotEmpty().WithMessage("Nội dung nhiệm vụ là bắt buộc.")
            .MaximumLength(GioiHan.DoDaiNoiDung)
            .WithMessage("Nội dung nhiệm vụ không được vượt quá " + GioiHan.DoDaiNoiDung + " ký tự.");

        RuleFor(x => x.DoKhan)
            .NotEmpty().WithMessage("Độ khẩn là bắt buộc.")
            .Must(v => MaDoKhan.HopLe(v))
            .WithMessage("Độ khẩn chỉ nhận một trong các giá trị: TRONGTAM, THUONGXUYEN, DOTXUAT.");

        RuleFor(x => x.SoNgayHxlTh)
            .GreaterThanOrEqualTo(0).When(x => x.SoNgayHxlTh.HasValue)
            .WithMessage("Số ngày thực hiện không được là số âm.");
    }
}

/// <summary>§5.3 C6 — body thu hoi nhiem vu (ly do KHONG bat buoc, §10.7).</summary>
public sealed class ThuHoiNhiemVuRequestValidator : AbstractValidator<ThuHoiNhiemVuRequest>
{
    public ThuHoiNhiemVuRequestValidator()
    {
        RuleFor(x => x.LyDo)
            .MaximumLength(GioiHan.DoDaiNoiDung)
            .WithMessage("Lý do thu hồi không được vượt quá " + GioiHan.DoDaiNoiDung + " ký tự.");
    }
}

/// <summary>§5.3 C7 — body thu hoi phan cong.</summary>
public sealed class ThuHoiPhanCongRequestValidator : AbstractValidator<ThuHoiPhanCongRequest>
{
    public ThuHoiPhanCongRequestValidator()
    {
        RuleFor(x => x.UserIds)
            .NotNull().WithMessage("Danh sách người cần thu hồi phân công là bắt buộc.")
            .Must(x => x != null && x.Any(u => u != Guid.Empty))
            .WithMessage("Phải chọn ít nhất 1 người để thu hồi phân công.");

        RuleFor(x => x.LyDo)
            .MaximumLength(GioiHan.DoDaiNoiDung)
            .WithMessage("Lý do không được vượt quá " + GioiHan.DoDaiNoiDung + " ký tự.");
    }
}

/// <summary>§1.3 — body nhac viec. Noi dung BAT BUOC, khong doi trang thai.</summary>
public sealed class NhacViecRequestValidator : AbstractValidator<NhacViecRequest>
{
    public NhacViecRequestValidator()
    {
        RuleFor(x => x.NoiDung)
            .NotEmpty().WithMessage("Nội dung nhắc việc là bắt buộc.")
            .MaximumLength(GioiHan.DoDaiNoiDung)
            .WithMessage("Nội dung nhắc việc không được vượt quá " + GioiHan.DoDaiNoiDung + " ký tự.");
    }
}
