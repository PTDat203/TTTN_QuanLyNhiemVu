using FluentValidation;
using QLNV.Core.Constants;
using QLNV.Core.Dtos;
using MaDoKhan = QLNV.Core.Constants.DoKhan;

namespace QLNV.Api.Validators;

/// <summary>§5.2 B3/B4 — kiem tra body tao/sua van ban chi dao (M04).</summary>
public sealed class LuuVanBanRequestValidator : AbstractValidator<LuuVanBanRequest>
{
    public LuuVanBanRequestValidator()
    {
        // §4.1 — trichyeu BAT BUOC, <= 2000 ky tu.
        RuleFor(x => x.TrichYeu)
            .NotEmpty().WithMessage("Trích yếu văn bản chỉ đạo là bắt buộc.")
            .MaximumLength(GioiHan.DoDaiTrichYeu)
            .WithMessage("Trích yếu không được vượt quá " + GioiHan.DoDaiTrichYeu + " ký tự.");

        // §4.1 — dokhan BAT BUOC, thuoc TRONGTAM / THUONGXUYEN / DOTXUAT.
        RuleFor(x => x.DoKhan)
            .NotEmpty().WithMessage("Độ khẩn là bắt buộc.")
            .Must(v => MaDoKhan.HopLe(v))
            .WithMessage("Độ khẩn chỉ nhận một trong các giá trị: TRONGTAM, THUONGXUYEN, DOTXUAT.");

        RuleFor(x => x.SoKyHieu)
            .MaximumLength(GioiHan.DoDaiSoKyHieu)
            .WithMessage("Số ký hiệu không được vượt quá " + GioiHan.DoDaiSoKyHieu + " ký tự.");

        RuleFor(x => x.CoQuanBanHanh)
            .MaximumLength(GioiHan.DoDaiCoQuanBanHanh)
            .WithMessage("Cơ quan ban hành không được vượt quá " + GioiHan.DoDaiCoQuanBanHanh + " ký tự.");
    }
}

/// <summary>§5.2 B1 — kiem tra tham so loc danh sach van ban.</summary>
public sealed class VanBanLocRequestValidator : AbstractValidator<VanBanLocRequest>
{
    public VanBanLocRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Số trang phải lớn hơn hoặc bằng 1.");

        RuleFor(x => x.Size)
            .InclusiveBetween(1, GioiHan.KichThuocTrangToiDa)
            .WithMessage("Kích thước trang phải nằm trong khoảng 1 đến " + GioiHan.KichThuocTrangToiDa + ".");

        RuleFor(x => x)
            .Must(x => !x.TuNgay.HasValue || !x.DenNgay.HasValue || x.TuNgay.Value <= x.DenNgay.Value)
            .WithMessage("Khoảng ngày không hợp lệ: ngày bắt đầu phải trước hoặc bằng ngày kết thúc.");
    }
}
