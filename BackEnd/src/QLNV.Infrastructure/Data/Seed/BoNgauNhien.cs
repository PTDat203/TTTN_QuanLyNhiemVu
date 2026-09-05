namespace QLNV.Infrastructure.Data.Seed;

/// <summary>
/// PRNG mulberry32 co seed co dinh — port nguyen ban tu <c>seed.js</c> muc 1.
///
/// LY DO khong dung <see cref="Random"/> cua .NET: thuat toan cua Random KHONG duoc
/// bao dam giu nguyen giua cac phien ban runtime, nen chay lai co the ra bo du lieu khac.
/// mulberry32 la 30 dong so hoc 32-bit, cho ket qua Y HET tren moi may, moi phien ban.
///
/// Moi phep tinh deu tren <see cref="uint"/> voi <c>unchecked</c> de mo phong dung
/// ngu nghia 32-bit cua JavaScript (<c>|0</c>, <c>&gt;&gt;&gt;</c>, <c>Math.imul</c>).
/// </summary>
public sealed class BoNgauNhien
{
    /// <summary>Seed mac dinh — trung voi <c>SEED_MAC_DINH</c> cua seed.js.</summary>
    public const int SeedMacDinh = 20260905;

    private uint _trangThai;

    public BoNgauNhien(int seed = SeedMacDinh)
    {
        _trangThai = unchecked((uint)seed);
    }

    /// <summary>So thuc trong nua khoang [0, 1).</summary>
    public double So()
    {
        unchecked
        {
            _trangThai += 0x6d2b79f5u;
            uint t = _trangThai;
            t = (t ^ (t >> 15)) * (1u | t);
            t = (t + ((t ^ (t >> 7)) * (61u | t))) ^ t;
            return (t ^ (t >> 14)) / 4294967296.0;
        }
    }

    /// <summary>So nguyen trong doan [min, max] (bao gom hai dau).</summary>
    public int Nguyen(int min, int max)
    {
        if (max < min) max = min;
        return min + (int)Math.Floor(So() * (max - min + 1));
    }

    /// <summary>Chon ngau nhien mot phan tu.</summary>
    public T Chon<T>(IReadOnlyList<T> ds)
    {
        ArgumentNullException.ThrowIfNull(ds);
        if (ds.Count == 0) throw new ArgumentException("Danh sách rỗng, không chọn được phần tử.", nameof(ds));
        return ds[(int)Math.Floor(So() * ds.Count)];
    }

    /// <summary>True voi xac suat <paramref name="p"/>.</summary>
    public bool Kha(double p) => So() < p;

    /// <summary>Tron Fisher-Yates, tra ve DANH SACH MOI (khong sua danh sach goc).</summary>
    public List<T> Tron<T>(IReadOnlyList<T> ds)
    {
        ArgumentNullException.ThrowIfNull(ds);
        var a = new List<T>(ds);
        for (int i = a.Count - 1; i > 0; i--)
        {
            int j = (int)Math.Floor(So() * (i + 1));
            (a[i], a[j]) = (a[j], a[i]);
        }
        return a;
    }

    /// <summary>Day chi so 0..n-1 (tuong duong <c>dayChiSo</c> cua seed.js).</summary>
    public static List<int> DayChiSo(int n)
    {
        var a = new List<int>(n);
        for (int i = 0; i < n; i++) a.Add(i);
        return a;
    }
}
