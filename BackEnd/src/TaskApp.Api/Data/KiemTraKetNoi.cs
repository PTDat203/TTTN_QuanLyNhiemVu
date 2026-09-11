using Microsoft.EntityFrameworkCore;

namespace TaskApp.Api.Data;

/// <summary>
/// Ket qua tu kiem tra ket noi Oracle va anh xa 12 bang.
/// </summary>
public sealed class KetQuaKiemTraKetNoi
{
    public bool KetNoiDuoc { get; set; }
    public string? LoiKetNoi { get; set; }
    public string? PhienBanOracle { get; set; }
    public string? Schema { get; set; }
    public List<KetQuaBang> CacBang { get; init; } = new();
    public bool TatCaAnhXaDung => KetNoiDuoc && CacBang.All(b => b.AnhXaDung);
}

/// <summary>Ket qua doc thu mot bang.</summary>
public sealed class KetQuaBang
{
    public string Bang { get; set; } = string.Empty;
    public bool AnhXaDung { get; set; }
    public int SoDong { get; set; }
    public string? Loi { get; set; }
}

/// <summary>
/// Doc thu tung bang de xac nhan anh xa EF Core khop voi schema Oracle thuc te.
///
/// Vi sao can: Oracle viet hoa dinh danh khong co nhay kep, EF Core thi luon dat nhay kep.
/// Neu mot cot bi map thieu hoac sai ten, cau SELECT sinh ra se tham chieu cot khong ton tai
/// va Oracle bao ORA-00904. Loi nay KHONG lo ra luc bien dich — chi lo khi truy van chay.
/// Vi vay phai co mot lan doc thu ngay luc khoi dong.
///
/// Bang rong van tinh la dat: muc tieu la khong co ORA-00904 / ORA-00942, khong phai co du lieu.
/// </summary>
public static class KiemTraKetNoi
{
    public static async Task<KetQuaKiemTraKetNoi> ChayAsync(
        TaskDbContext db,
        CancellationToken ct = default)
    {
        var kq = new KetQuaKiemTraKetNoi { Schema = TaskDbContext.Schema };

        // 1. SELECT FROM DUAL — kiem driver, listener va thong tin dang nhap
        try
        {
            var phienBan = await db.Database
                .SqlQueryRaw<string>("SELECT BANNER AS \"Value\" FROM V$VERSION WHERE ROWNUM = 1")
                .FirstOrDefaultAsync(ct);

            kq.PhienBanOracle = phienBan;
            kq.KetNoiDuoc = true;
        }
        catch (Exception)
        {
            // V$VERSION can quyen doc view he thong. Neu bi tu choi thi lui ve DUAL.
            try
            {
                var thu = await db.Database
                    .SqlQueryRaw<string>("SELECT 'KET NOI ORACLE THANH CONG' AS \"Value\" FROM DUAL")
                    .FirstOrDefaultAsync(ct);

                kq.PhienBanOracle = thu;
                kq.KetNoiDuoc = true;
            }
            catch (Exception exDual)
            {
                kq.KetNoiDuoc = false;
                kq.LoiKetNoi = exDual.Message;
                return kq;
            }
        }

        // 2. Doc thu tung bang — moi bang la mot cau SELECT do EF Core sinh tu anh xa
        await DemAsync(kq, "DEPARTMENTS", () => db.Departments.CountAsync(ct));
        await DemAsync(kq, "TEAMS", () => db.Teams.CountAsync(ct));
        await DemAsync(kq, "USERS", () => db.Users.CountAsync(ct));
        await DemAsync(kq, "USER_SKILLS", () => db.UserSkills.CountAsync(ct));
        await DemAsync(kq, "USER_QUALIFICATIONS", () => db.UserQualifications.CountAsync(ct));
        await DemAsync(kq, "SKILLS", () => db.Skills.CountAsync(ct));
        await DemAsync(kq, "TASK_STATUS_LOOKUP", () => db.TaskStatusLookups.CountAsync(ct));
        await DemAsync(kq, "TASKS", () => db.Tasks.CountAsync(ct));
        await DemAsync(kq, "TASK_REQUIRED_SKILLS", () => db.TaskRequiredSkills.CountAsync(ct));
        await DemAsync(kq, "TASK_PROGRESS", () => db.TaskProgresses.CountAsync(ct));
        await DemAsync(kq, "TASK_REPORTS", () => db.TaskReports.CountAsync(ct));
        await DemAsync(kq, "TASK_ATTACHMENTS", () => db.TaskAttachments.CountAsync(ct));

        return kq;
    }

    /// <summary>
    /// COUNT(*) chi kiem duoc ten bang. De kiem ca ten COT, phai thuc su vat du lieu ra
    /// mot doi tuong — vi vay moi bang con lay them 1 dong bang <c>FirstOrDefault</c>
    /// o cac ham goi ben duoi.
    /// </summary>
    private static async Task DemAsync(
        KetQuaKiemTraKetNoi kq,
        string tenBang,
        Func<Task<int>> dem)
    {
        var muc = new KetQuaBang { Bang = tenBang };
        try
        {
            muc.SoDong = await dem();
            muc.AnhXaDung = true;
        }
        catch (Exception ex)
        {
            muc.AnhXaDung = false;
            muc.Loi = ex.GetBaseException().Message;
        }
        kq.CacBang.Add(muc);
    }

    /// <summary>
    /// Vat thu mot dong cua moi bang de kiem TEN COT (COUNT(*) khong dung toi cot nao).
    /// Goi rieng vi cham hon; dung o endpoint chan doan chu khong chay luc khoi dong.
    /// </summary>
    public static async Task<List<KetQuaBang>> DocThuMotDongAsync(
        TaskDbContext db,
        CancellationToken ct = default)
    {
        var ds = new List<KetQuaBang>();

        // CHAN TRUOC: neu khong ket noi duoc thi DUNG NGAY, khong thu tiep cac bang.
        //
        // Profile DEFAULT cua Oracle khoa tai khoan sau 10 lan dang nhap sai
        // (FAILED_LOGIN_ATTEMPTS = 10, khoa 1 ngay). Neu sai mat khau ma cu thu du tung bang
        // thi chi vai lan goi endpoint nay la tai khoan bi khoa — luc do loi that
        // (ORA-01017 sai mat khau) bi che mat boi loi phu (ORA-28000 khoa tai khoan).
        if (!await db.Database.CanConnectAsync(ct))
        {
            ds.Add(new KetQuaBang
            {
                Bang = "(ket noi)",
                AnhXaDung = false,
                Loi = "Không kết nối được Oracle. Dừng kiểm tra để tránh làm khóa tài khoản " +
                      "do đăng nhập sai quá 10 lần. Kiểm tra lại chuỗi kết nối và mật khẩu."
            });
            return ds;
        }

        await ThuAsync(ds, "DEPARTMENTS", async () => await db.Departments.AsNoTracking().FirstOrDefaultAsync(ct) is not null);
        await ThuAsync(ds, "TEAMS", async () => await db.Teams.AsNoTracking().FirstOrDefaultAsync(ct) is not null);
        await ThuAsync(ds, "USERS", async () => await db.Users.AsNoTracking().FirstOrDefaultAsync(ct) is not null);
        await ThuAsync(ds, "USER_SKILLS", async () => await db.UserSkills.AsNoTracking().FirstOrDefaultAsync(ct) is not null);
        await ThuAsync(ds, "USER_QUALIFICATIONS", async () => await db.UserQualifications.AsNoTracking().FirstOrDefaultAsync(ct) is not null);
        await ThuAsync(ds, "SKILLS", async () => await db.Skills.AsNoTracking().FirstOrDefaultAsync(ct) is not null);
        await ThuAsync(ds, "TASK_STATUS_LOOKUP", async () => await db.TaskStatusLookups.AsNoTracking().FirstOrDefaultAsync(ct) is not null);
        await ThuAsync(ds, "TASKS", async () => await db.Tasks.AsNoTracking().FirstOrDefaultAsync(ct) is not null);
        await ThuAsync(ds, "TASK_REQUIRED_SKILLS", async () => await db.TaskRequiredSkills.AsNoTracking().FirstOrDefaultAsync(ct) is not null);
        await ThuAsync(ds, "TASK_PROGRESS", async () => await db.TaskProgresses.AsNoTracking().FirstOrDefaultAsync(ct) is not null);
        await ThuAsync(ds, "TASK_REPORTS", async () => await db.TaskReports.AsNoTracking().FirstOrDefaultAsync(ct) is not null);
        await ThuAsync(ds, "TASK_ATTACHMENTS", async () => await db.TaskAttachments.AsNoTracking().FirstOrDefaultAsync(ct) is not null);

        return ds;
    }

    private static async Task ThuAsync(List<KetQuaBang> ds, string tenBang, Func<Task<bool>> doc)
    {
        var muc = new KetQuaBang { Bang = tenBang };
        try
        {
            var coDuLieu = await doc();
            muc.SoDong = coDuLieu ? 1 : 0;
            muc.AnhXaDung = true;
        }
        catch (Exception ex)
        {
            muc.AnhXaDung = false;
            muc.Loi = ex.GetBaseException().Message;
        }
        ds.Add(muc);
    }
}
