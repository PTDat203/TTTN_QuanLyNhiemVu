using QLNV.Core.Constants;

namespace QLNV.Core.Services;

/// <summary>
/// §9.6 bang "Sinh cau ly do" + §9.7 "Toi da 4 dong ly do".
///
/// MAU CAU CO DINH — KHONG goi LLM. Dac ta chot nhu vay de ket qua on dinh
/// (cung du lieu ra cung cau chu) va khong ton chi phi goi API ben ngoai.
///
/// Thu tu uu tien §9.7: chuyen mon -> kinh nghiem -> hieu qua -> khoi luong.
/// Hai dong BO SUNG (bi tra lai / dang qua han) xep sau cung: chung chi hien khi
/// con cho trong, va thong tin khong mat vi da co nhan BI_TRA_LAI / CO_QUA_HAN.
///
/// Moi cau deu la tieng Viet CO DAU.
/// </summary>
public static class SinhLyDo
{
    // Thu tu uu tien §9.7 (so cang nho cang duoc giu lai truoc khi cat con 4 dong)
    private const int UuChuyenMon = 1;
    private const int UuKinhNghiem = 2;
    private const int UuHieuQua = 3;
    private const int UuKhoiLuong = 4;
    private const int UuBiTraLai = 5;   // bo sung
    private const int UuQuaHan = 6;     // bo sung

    /// <summary>
    /// Sinh toi da <see cref="GioiHan.SoDongLyDoToiDa"/> dong ly do cho mot ung vien.
    /// </summary>
    /// <param name="dt">Ket qua cham dac trung §9.4 cua chinh ung vien do.</param>
    public static IReadOnlyList<string> Sinh(DacTrungUngVien dt)
    {
        ArgumentNullException.ThrowIfNull(dt);

        var sl = dt.SoLieu;
        var cs = dt.CoSoTinh;
        var ds = new List<(int Uu, string Cau)>();

        // --- (1) Chuyen mon — uu tien 1 (§9.4 S1) ---
        // Ban nay SUY chuyen mon tu lich su (§9.2 / §10.1) nen KHONG co muc thanh thao 1-5;
        // mau cau cua §9.6 dong 1 ("o muc {nhan m} ({m}/5)") khong ap dung duoc, thay bang
        // mau cau theo so viec da hoan thanh.
        if (cs.KhongCoLinhVuc)
        {
            // §9.5 dong "Nhiem vu khong co linh vuc": S1 tinh tren TONG so nhiem vu
            ds.Add((UuChuyenMon,
                "Đã hoàn thành " + cs.SoNvHoanThanhTong +
                " nhiệm vụ (nhiệm vụ chưa gán lĩnh vực nên tính trên mọi lĩnh vực)"));
        }
        else if (cs.SoNvLinhVuc > 0)
        {
            ds.Add((UuChuyenMon,
                "Đã hoàn thành " + cs.SoNvLinhVuc + " nhiệm vụ thuộc lĩnh vực " + TenLinhVuc(cs)));
        }
        else if (cs.SoNvNhomCha > 0)
        {
            // §9.4 S1 nhanh n = 0: xet linh vuc cung nhom cha, chiet khau 50%
            ds.Add((UuChuyenMon,
                "Chưa từng làm " + TenLinhVuc(cs) + ", nhưng đã hoàn thành " + cs.SoNvNhomCha +
                " nhiệm vụ thuộc nhóm " + (string.IsNullOrWhiteSpace(cs.TenNhomCha) ? "cùng nhóm cha" : cs.TenNhomCha)));
        }
        else
        {
            ds.Add((UuChuyenMon, "Chưa từng thực hiện nhiệm vụ thuộc lĩnh vực này"));
        }

        // --- (2) Kinh nghiem / lich su thuc hien — uu tien 2 (§9.4 S2) ---
        if (cs.SoNvHoanThanhTong > 0)
        {
            ds.Add((UuKinhNghiem, "Tổng cộng " + cs.SoNvHoanThanhTong + " nhiệm vụ đã được nghiệm thu"));
        }
        else
        {
            ds.Add((UuKinhNghiem, "Người mới – chưa có nhiệm vụ nào được nghiệm thu"));
        }

        // --- (3) Hieu qua — uu tien 3 (§9.4 S3) ---
        // §9.6 co them ve "diem chat luong trung binh {y}/6" nhung §9.4 ghi chu + §10.2
        // da LOAI hsChatluong khoi cong thuc => bo ve do, khong bia so.
        if (sl.SoNvHoanThanh > 0)
        {
            var phanTram = SoHoc.LamTronNguyen(SoHoc.ChiaAnToan(sl.SoNvDungHan, sl.SoNvHoanThanh) * 100);
            ds.Add((UuHieuQua,
                "Tỷ lệ đúng hạn " + phanTram + "% (" + sl.SoNvDungHan + "/" + sl.SoNvHoanThanh + ")"));
        }

        if (sl.SoNvBiTraLai > 0)
        {
            ds.Add((UuBiTraLai, "Lưu ý: " + sl.SoNvBiTraLai + " nhiệm vụ từng bị trả lại yêu cầu bổ sung"));
        }

        // --- (4) Khoi luong — uu tien 4 (§9.6 bang mau cau) ---
        var tai = SoHoc.SoVN(sl.TaiTrongSo, 1);
        if (dt.S4 >= CauHinhAi.NguongS4Ranh)
        {
            ds.Add((UuKhoiLuong,
                "Đang giữ " + sl.SoNvDangMo + " nhiệm vụ (tải " + tai + "/" + sl.K + " — còn nhiều dư địa)"));
        }
        else if (dt.S4 < CauHinhAi.NguongS4Ban)
        {
            ds.Add((UuKhoiLuong,
                "⚠ Đang khá bận: " + sl.SoNvDangMo + " nhiệm vụ (tải " + tai + "/" + sl.K + ")"));
        }
        else
        {
            // Dac ta khong quy dinh cau cho khoang 0,25 <= S4 < 0,5 -> dung cau trung tinh
            ds.Add((UuKhoiLuong, "Đang giữ " + sl.SoNvDangMo + " nhiệm vụ (tải " + tai + "/" + sl.K + ")"));
        }

        // --- (5) San sang — uu tien 6 (§9.4 S5) ---
        if (sl.SoNvQuaHan > 0)
        {
            ds.Add((UuQuaHan, "⚠ Đang có " + sl.SoNvQuaHan + " nhiệm vụ quá hạn"));
        }

        // §9.7: xep theo dung thu tu uu tien roi cat con 4 dong.
        // OrderBy cua LINQ la sap xep ON DINH -> cac dong cung muc uu tien giu nguyen
        // thu tu them vao, khong phu thuoc cai dat sort.
        var ketQua = new List<string>();
        foreach (var dong in ds.OrderBy(x => x.Uu))
        {
            if (ketQua.Count >= GioiHan.SoDongLyDoToiDa) break;
            ketQua.Add(dong.Cau);
        }
        return ketQua;
    }

    /// <summary>Ten linh vuc de hien thi; khong tra cuu duoc thi dung ma.</summary>
    private static string TenLinhVuc(CoSoTinhAi cs)
    {
        if (!string.IsNullOrWhiteSpace(cs.TenLinhVuc)) return cs.TenLinhVuc!;
        return string.IsNullOrWhiteSpace(cs.MaLinhVuc) ? "lĩnh vực này" : cs.MaLinhVuc!;
    }
}
