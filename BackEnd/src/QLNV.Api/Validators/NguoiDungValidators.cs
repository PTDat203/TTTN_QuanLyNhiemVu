using FluentValidation;
using QLNV.Core.Constants;
using QLNV.Core.Dtos;

namespace QLNV.Api.Validators;

/// <summary>
/// §5.9 I1 — kiem tra dau vao tao / cap nhat nguoi dung.
///
/// LUU Y: bo kiem tra "mat khau bat buoc" KHONG dat o day vi cung mot DTO dung cho
/// ca tao moi (bat buoc) lan cap nhat (khong bat buoc) — dieu kien do nam trong
/// <c>NguoiDungController</c>.
/// </summary>
public sealed class LuuNguoiDungRequestValidator : AbstractValidator<LuuNguoiDungRequest>
{
    public LuuNguoiDungRequestValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Tên đăng nhập không được để trống.")
            .MaximumLength(100).WithMessage("Tên đăng nhập tối đa 100 ký tự.")
            .Matches("^[A-Za-z0-9._-]+$")
            .WithMessage("Tên đăng nhập chỉ gồm chữ cái không dấu, chữ số và các ký tự . _ -");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống.")
            .MaximumLength(200).WithMessage("Họ tên tối đa 200 ký tự.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Địa chỉ thư điện tử không hợp lệ.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.UnitCode)
            .NotEmpty().WithMessage("Đơn vị không được để trống.")
            .MaximumLength(50).WithMessage("Mã đơn vị tối đa 50 ký tự.");

        RuleFor(x => x.ChucVu)
            .MaximumLength(200).WithMessage("Chức vụ tối đa 200 ký tự.");

        // §6.1: chi MOT truc vai tro duy nhat.
        RuleFor(x => x.VaiTro)
            .NotEmpty().WithMessage("Vai trò không được để trống.")
            .Must(ma => VaiTro.HopLe(ma))
            .WithMessage($"Vai trò chỉ nhận một trong các giá trị: {string.Join(", ", VaiTro.ToanBo())}.");

        RuleFor(x => x.TrangThai)
            .InclusiveBetween(0, 1).WithMessage("Trạng thái tài khoản chỉ nhận giá trị 0 (khoá) hoặc 1 (hoạt động).");

        // §4.7 / §9.4 S4 — nguong tai K.
        RuleFor(x => x.MaxConcurrentTasks)
            .InclusiveBetween(1, 100)
            .WithMessage("Ngưỡng số nhiệm vụ đồng thời phải nằm trong khoảng từ 1 đến 100.");

        RuleFor(x => x.Password)
            .MinimumLength(8).WithMessage("Mật khẩu phải có ít nhất 8 ký tự.")
            .MaximumLength(200).WithMessage("Mật khẩu tối đa 200 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.Password));
    }
}

/// <summary>§5.9 I1 — kiem tra tham so loc danh sach nguoi dung.</summary>
public sealed class NguoiDungLocRequestValidator : AbstractValidator<NguoiDungLocRequest>
{
    public NguoiDungLocRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Số trang phải lớn hơn hoặc bằng 1.");

        RuleFor(x => x.Size)
            .InclusiveBetween(1, GioiHan.KichThuocTrangToiDa)
            .WithMessage($"Cỡ trang phải nằm trong khoảng từ 1 đến {GioiHan.KichThuocTrangToiDa}.");
    }
}
