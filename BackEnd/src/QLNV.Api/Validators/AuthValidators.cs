using FluentValidation;
using QLNV.Core.Dtos;

namespace QLNV.Api.Validators;

/// <summary>
/// §5.1 A1 — kiem tra dau vao dang nhap.
/// Moi thong bao deu la tieng Viet co dau vi FE hien thang len man M01.
/// </summary>
public sealed class DangNhapRequestValidator : AbstractValidator<DangNhapRequest>
{
    public DangNhapRequestValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Tên đăng nhập không được để trống.")
            .MaximumLength(100).WithMessage("Tên đăng nhập tối đa 100 ký tự.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.")
            .MaximumLength(200).WithMessage("Mật khẩu tối đa 200 ký tự.");
    }
}

/// <summary>§5.1 A2 — kiem tra dau vao lam moi token.</summary>
public sealed class LamMoiTokenRequestValidator : AbstractValidator<LamMoiTokenRequest>
{
    public LamMoiTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Thiếu refresh token.");
    }
}

/// <summary>Doi mat khau — toi thieu 8 ky tu, khong trung mat khau cu.</summary>
public sealed class DoiMatKhauRequestValidator : AbstractValidator<DoiMatKhauRequest>
{
    public DoiMatKhauRequestValidator()
    {
        RuleFor(x => x.MatKhauCu)
            .NotEmpty().WithMessage("Vui lòng nhập mật khẩu hiện tại.");

        RuleFor(x => x.MatKhauMoi)
            .NotEmpty().WithMessage("Vui lòng nhập mật khẩu mới.")
            .MinimumLength(8).WithMessage("Mật khẩu mới phải có ít nhất 8 ký tự.")
            .MaximumLength(200).WithMessage("Mật khẩu mới tối đa 200 ký tự.");

        RuleFor(x => x.MatKhauMoi)
            .NotEqual(x => x.MatKhauCu).WithMessage("Mật khẩu mới phải khác mật khẩu hiện tại.");
    }
}
