using FluentValidation;
using QLNV.Core.Constants;
using QLNV.Core.Dtos;

namespace QLNV.Api.Validators;

/// <summary>
/// §5.6 F1 — body de xuat gia han.
///
/// LUU Y: rang buoc <c>hanxulydexuat &gt;= hanxulyth</c> (§4.5) KHONG kiem duoc o day
/// vi validator khong nhin thay nhiem vu. No duoc kiem O HAI NOI:
///   1) <c>GiaHanController.XinGiaHan</c> — so voi <c>hanxulyth</c> hien tai, tra 400 kem ngay cu;
///   2) <c>INhiemVuStateMachine.XinGiaHan</c> — chot chan cuoi cung (§6.4: khong tin FE).
/// </summary>
public sealed class GiaHanRequestValidator : AbstractValidator<GiaHanRequest>
{
    public GiaHanRequestValidator()
    {
        RuleFor(x => x.HanXuLyDeXuat)
            .Must(d => d != default)
            .WithMessage("Hạn xử lý đề xuất là bắt buộc.");

        RuleFor(x => x.NoiDung)
            .MaximumLength(GioiHan.DoDaiNoiDung)
            .WithMessage("Lý do gia hạn không được vượt quá " + GioiHan.DoDaiNoiDung + " ký tự.");
    }
}

/// <summary>§5.6 F2 — body duyet / tu choi de xuat gia han.</summary>
public sealed class DuyetGiaHanRequestValidator : AbstractValidator<DuyetGiaHanRequest>
{
    public DuyetGiaHanRequestValidator()
    {
        RuleFor(x => x.KetQua)
            .NotEmpty().WithMessage("Kết quả duyệt gia hạn là bắt buộc.")
            .Must(v => KetQuaDuyetGiaHan.HopLe(v))
            .WithMessage("Kết quả duyệt gia hạn chỉ nhận giá trị DUYET hoặc TU_CHOI.");

        RuleFor(x => x.PhanHoi)
            .MaximumLength(GioiHan.DoDaiNoiDung)
            .WithMessage("Ý kiến người duyệt không được vượt quá " + GioiHan.DoDaiNoiDung + " ký tự.");

        // §10.7 — app moi BAT BUOC nhap ly do khi TU CHOI de xuat gia han.
        RuleFor(x => x.PhanHoi)
            .NotEmpty().When(x => x.KetQua == KetQuaDuyetGiaHan.TuChoi)
            .WithMessage("Phải nhập lý do khi từ chối đề xuất gia hạn.");
    }
}
