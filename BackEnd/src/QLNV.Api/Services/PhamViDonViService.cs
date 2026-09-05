using Microsoft.EntityFrameworkCore;
using QLNV.Core.Entities;

namespace QLNV.Api.Services;

/// <summary>
/// §6.2 dong 20 va 22 — tinh PHAM VI DU LIEU theo cay don vi:
/// "nguoi giao chi thay don vi minh + don vi con", "nguoi giao sua duoc ho so cua
/// cap duoi trong don vi".
///
/// Dat o tang API vi day la quy tac phan quyen, khong phai quy tac may trang thai.
/// </summary>
public sealed class PhamViDonViService
{
    private readonly DbContext _db;

    public PhamViDonViService(DbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <summary>
    /// Tra ve tap ma don vi gom <paramref name="unitCode"/> va TAT CA don vi con
    /// (de quy, khong gioi han cap). Tra tap rong khi <paramref name="unitCode"/> rong.
    /// </summary>
    public async Task<HashSet<string>> DonViVaConAsync(string? unitCode, CancellationToken ct)
    {
        var ketQua = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(unitCode))
        {
            return ketQua;
        }

        var canh = await _db.Set<SysUnit>()
            .AsNoTracking()
            .Select(x => new { x.UnitCode, x.MaCha })
            .ToListAsync(ct);

        // Gom con theo cha de duyet mot lan, tranh truy van long nhau.
        var conTheoCha = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var c in canh)
        {
            if (string.IsNullOrWhiteSpace(c.MaCha))
            {
                continue;
            }

            if (!conTheoCha.TryGetValue(c.MaCha!, out var ds))
            {
                ds = new List<string>();
                conTheoCha[c.MaCha!] = ds;
            }

            ds.Add(c.UnitCode);
        }

        var hangDoi = new Queue<string>();
        hangDoi.Enqueue(unitCode!);
        ketQua.Add(unitCode!);

        while (hangDoi.Count > 0)
        {
            var hienTai = hangDoi.Dequeue();
            if (!conTheoCha.TryGetValue(hienTai, out var dsCon))
            {
                continue;
            }

            foreach (var con in dsCon)
            {
                // ketQua.Add tra false neu da co -> dong thoi chan vong lap du lieu ban.
                if (ketQua.Add(con))
                {
                    hangDoi.Enqueue(con);
                }
            }
        }

        return ketQua;
    }
}
