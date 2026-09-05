using QLNV.Core.Common;
using QLNV.Core.Dtos;

namespace QLNV.Core.Abstractions;

/// <summary>
/// §5.7 G1-G3 — truu tuong hoa kho luu tep.
/// Ban dau cai dat bang HE THONG TEP CUC BO (thu muc <c>App_Data/files</c>);
/// sau nay doi sang MinIO ma KHONG phai sua controller.
///
/// <c>khoaLuuTru</c> tra ve la khoa logic (vi du "2026/09/ab12...pdf"), KHONG phai duong dan
/// tuyet doi tren dia. Cai dat phai tu chan duyet thu muc (path traversal).
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Luu mot tep. Tra ve khoa luu tru de ghi vao <c>NHIEMVU_FILE.filePath</c>.
    /// Kiem tra whitelist duoi tep va gioi han 20 MB (§5.7 G1) la trach nhiem cua tang goi,
    /// nhung cai dat NEN kiem lai.
    /// </summary>
    Task<Result<string>> LuuAsync(Stream noiDung, string tenTep, string? contentType, CancellationToken ct);

    /// <summary>Doc mot tep theo khoa luu tru (§5.7 G2). Nguoi goi chiu trach nhiem giai phong Stream.</summary>
    Task<Result<NoiDungTepDto>> DocAsync(string khoaLuuTru, CancellationToken ct);

    /// <summary>Xoa mot tep theo khoa luu tru (§5.7 G3).</summary>
    Task<Result> XoaAsync(string khoaLuuTru, CancellationToken ct);

    /// <summary>Kiem tra tep con ton tai khong.</summary>
    Task<bool> TonTaiAsync(string khoaLuuTru, CancellationToken ct);
}
