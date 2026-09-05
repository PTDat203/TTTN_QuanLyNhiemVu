using QLNV.Core.Constants;

namespace QLNV.Infrastructure.Data.Seed;

/// <summary>
/// Kho cau tieng Viet cho bo du lieu mau — port tu seed.js muc 6, 7, 8.
///
/// YEU CAU: noi dung nhiem vu phai la CAU HANH CHINH THAT, dung voi linh vuc.
/// 4 mau/linh vuc x 8 linh vuc = 32 mau, ghep 12 duoi bo sung =&gt; ~380 bien the co nghia.
/// TUYET DOI khong sinh chuoi ngau nhien vo nghia.
/// </summary>
public static class MauNoiDung
{
    /// <summary>Mau noi dung nhiem vu theo linh vuc — <c>{p}</c> la cho ghep duoi bo sung.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> TheoLinhVuc =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["CNTT_HT"] = new[]
            {
                "Rà soát, nâng cấp hạ tầng máy chủ và thiết bị mạng cho Trung tâm dữ liệu của Sở{p}",
                "Xây dựng phương án sao lưu, dự phòng dữ liệu cho hệ thống thư điện tử công vụ{p}",
                "Kiểm tra, bảo trì định kỳ mạng nội bộ và đường truyền số liệu chuyên dùng{p}",
                "Triển khai mở rộng hệ thống hội nghị truyền hình trực tuyến tới các đơn vị trực thuộc{p}"
            },
            ["CNTT_PM"] = new[]
            {
                "Kiểm thử và nghiệm thu phân hệ quản lý văn bản điều hành trước khi vận hành chính thức{p}",
                "Xây dựng tài liệu hướng dẫn sử dụng phần mềm một cửa điện tử cho cán bộ cấp xã{p}",
                "Phối hợp khắc phục lỗi đồng bộ dữ liệu giữa Cổng dịch vụ công tỉnh và Cổng dịch vụ công quốc gia{p}",
                "Bổ sung chức năng thống kê, báo cáo cho phần mềm quản lý nhiệm vụ theo yêu cầu của lãnh đạo Sở{p}"
            },
            ["CNTT_ATTT"] = new[]
            {
                "Rà soát, đánh giá an toàn thông tin cho hệ thống một cửa điện tử trước khi nâng cấp{p}",
                "Xây dựng hồ sơ đề xuất cấp độ an toàn hệ thống thông tin cho Cổng thông tin điện tử tỉnh{p}",
                "Tổ chức diễn tập ứng cứu sự cố an toàn thông tin mạng cho đội ứng cứu sự cố của tỉnh{p}",
                "Kiểm tra, xử lý cảnh báo mã độc và lỗ hổng bảo mật trên máy trạm của các cơ quan nhà nước{p}"
            },
            ["CNTT_BCVT"] = new[]
            {
                "Kiểm tra hoạt động cung cấp dịch vụ bưu chính công ích tại các điểm phục vụ{p}",
                "Rà soát, chỉnh trang hệ thống cáp viễn thông treo trên các tuyến phố chính{p}",
                "Thẩm định hồ sơ xin cấp phép xây dựng trạm thu phát sóng thông tin di động{p}",
                "Tổng hợp số liệu phát triển hạ tầng viễn thông phục vụ báo cáo Bộ Thông tin và Truyền thông{p}"
            },
            ["HC_VT"] = new[]
            {
                "Số hoá và lập cơ sở dữ liệu hồ sơ lưu trữ của Sở giai đoạn 2015 - 2020{p}",
                "Rà soát, chỉnh lý tài liệu tồn đọng tại kho lưu trữ cơ quan{p}",
                "Xây dựng quy chế công tác văn thư, lưu trữ theo quy định mới{p}",
                "Kiểm tra việc chấp hành quy định về thể thức và kỹ thuật trình bày văn bản hành chính{p}"
            },
            ["HC_TCCB"] = new[]
            {
                "Xây dựng kế hoạch đào tạo, bồi dưỡng công chức, viên chức của Sở{p}",
                "Rà soát, cập nhật hồ sơ công chức, viên chức trên phần mềm quản lý cán bộ{p}",
                "Tổng hợp kết quả đánh giá, xếp loại chất lượng công chức, viên chức{p}",
                "Xây dựng đề án vị trí việc làm của các phòng chuyên môn thuộc Sở{p}"
            },
            ["KH_DT"] = new[]
            {
                "Lập báo cáo đề xuất chủ trương đầu tư dự án chuyển đổi số giai đoạn 2026 - 2030{p}",
                "Tổng hợp, xây dựng kế hoạch ứng dụng công nghệ thông tin của tỉnh{p}",
                "Thẩm định thuyết minh và dự toán các dự án công nghệ thông tin do đơn vị trình{p}",
                "Theo dõi, đôn đốc tiến độ giải ngân các dự án đầu tư công do Sở làm chủ đầu tư{p}"
            },
            ["KH_TC"] = new[]
            {
                "Lập báo cáo quyết toán ngân sách nhà nước của Sở{p}",
                "Rà soát, xây dựng dự toán thu chi ngân sách năm sau{p}",
                "Kiểm tra hồ sơ thanh toán các gói thầu mua sắm thiết bị công nghệ thông tin{p}",
                "Hướng dẫn các đơn vị trực thuộc thực hiện chế độ tự chủ tài chính{p}"
            }
        };

    /// <summary>12 duoi bo sung ghep vao <c>{p}</c> — 3 duoi rong de cau goc van xuat hien.</summary>
    public static readonly IReadOnlyList<string> DuoiBoSung = new[]
    {
        "", "", "",
        " (đợt 1)",
        " (đợt 2)",
        " giai đoạn 1",
        " giai đoạn 2",
        " trên địa bàn tỉnh",
        " theo chỉ đạo của Ủy ban nhân dân tỉnh",
        " phục vụ công tác chuyển đổi số",
        " báo cáo Giám đốc Sở trước hạn",
        " đối với các đơn vị trực thuộc"
    };

    /// <summary>Trich yeu van ban chi dao lich su — 2 mau/linh vuc.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> TrichYeuTheoLinhVuc =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["CNTT_HT"] = new[]
            {
                "Về việc bảo đảm hạ tầng kỹ thuật công nghệ thông tin phục vụ hoạt động của cơ quan nhà nước",
                "Kế hoạch đầu tư, nâng cấp hạ tầng Trung tâm dữ liệu và mạng truyền số liệu chuyên dùng"
            },
            ["CNTT_PM"] = new[]
            {
                "Về việc triển khai, hoàn thiện các phần mềm dùng chung phục vụ chính quyền điện tử",
                "Kế hoạch kiểm thử, nghiệm thu và đưa vào vận hành các phân hệ phần mềm quản lý điều hành"
            },
            ["CNTT_ATTT"] = new[]
            {
                "Về việc tăng cường bảo đảm an toàn thông tin mạng cho các hệ thống thông tin của tỉnh",
                "Kế hoạch kiểm tra, đánh giá và ứng cứu sự cố an toàn thông tin mạng"
            },
            ["CNTT_BCVT"] = new[]
            {
                "Về việc tăng cường công tác quản lý nhà nước về bưu chính, viễn thông trên địa bàn",
                "Kế hoạch phát triển hạ tầng bưu chính, viễn thông và chỉnh trang cáp viễn thông"
            },
            ["HC_VT"] = new[]
            {
                "Về việc chấn chỉnh công tác văn thư, lưu trữ và thể thức văn bản hành chính",
                "Kế hoạch số hoá, chỉnh lý tài liệu lưu trữ của cơ quan"
            },
            ["HC_TCCB"] = new[]
            {
                "Về việc kiện toàn tổ chức bộ máy và công tác cán bộ của các phòng chuyên môn",
                "Kế hoạch đào tạo, bồi dưỡng và đánh giá công chức, viên chức"
            },
            ["KH_DT"] = new[]
            {
                "Về việc xây dựng kế hoạch đầu tư công lĩnh vực công nghệ thông tin",
                "Kế hoạch tổng hợp, thẩm định danh mục dự án ứng dụng công nghệ thông tin"
            },
            ["KH_TC"] = new[]
            {
                "Về việc lập dự toán, quyết toán ngân sách nhà nước và quản lý mua sắm công",
                "Kế hoạch kiểm tra công tác tài chính, kế toán tại các đơn vị trực thuộc"
            }
        };

    // --- Cac cau chuan cho ban ghi XULY_NHIEMVU ---

    public static readonly IReadOnlyList<string> LyDoTraLai = new[]
    {
        "Báo cáo chưa nêu rõ kết quả so với yêu cầu, đề nghị bổ sung số liệu minh chứng.",
        "Thiếu phụ lục kèm theo, đề nghị hoàn thiện và gửi lại.",
        "Nội dung còn chung chung, đề nghị làm rõ phần đề xuất và lộ trình thực hiện.",
        "Số liệu chưa khớp với báo cáo của đơn vị, đề nghị rà soát lại."
    };

    public static readonly IReadOnlyList<string> YKienDat = new[]
    {
        "Kết quả đạt yêu cầu, đồng ý nghiệm thu.",
        "Đã hoàn thành theo đúng nội dung chỉ đạo, đồng ý nghiệm thu.",
        "Báo cáo đầy đủ, đúng yêu cầu. Đồng ý nghiệm thu."
    };

    public static readonly IReadOnlyList<string> NoiDungTienDo = new[]
    {
        "Đã khảo sát hiện trạng và tổng hợp số liệu ban đầu.",
        "Đang phối hợp với các đơn vị liên quan để lấy ý kiến.",
        "Đã hoàn thiện dự thảo, đang rà soát trước khi trình.",
        "Đã xử lý phần lớn khối lượng công việc, còn khâu hoàn thiện hồ sơ.",
        "Đang tổng hợp tài liệu và chuẩn bị báo cáo kết quả."
    };

    public static readonly IReadOnlyList<string> NoiDungBaoCao = new[]
    {
        "Đã hoàn thành nội dung được giao, kính trình lãnh đạo xem xét, nghiệm thu.",
        "Báo cáo kết quả thực hiện kèm theo hồ sơ, tài liệu liên quan.",
        "Đã hoàn thành và bàn giao sản phẩm cho các đơn vị liên quan."
    };

    public const string NoiDungTiepNhan = "Đã tiếp nhận nhiệm vụ và xây dựng kế hoạch thực hiện.";

    // ---------------------------------------------------------------------------------
    // 12 van ban chi dao "noi bat" — chua toan bo 28 nhiem vu DANG SONG.
    // ---------------------------------------------------------------------------------
    public static readonly IReadOnlyList<VanBanNoiBatMau> VanBanNoiBat = new[]
    {
        new VanBanNoiBatMau("VB01", "142/QĐ-STTTT", "QUYETDINH", "CNTT_PM", "Sở Thông tin và Truyền thông", "Giám đốc Sở", DoKhan.TrongTam, -35, "Quyết định phê duyệt Kế hoạch phát triển, nâng cấp các phần mềm dùng chung của Sở năm 2026"),
        new VanBanNoiBatMau("VB02", "87/KH-STTTT", "KEHOACH", "CNTT_HT", "Sở Thông tin và Truyền thông", "Giám đốc Sở", DoKhan.TrongTam, -30, "Kế hoạch nâng cấp hạ tầng Trung tâm dữ liệu và mạng truyền số liệu chuyên dùng năm 2026"),
        new VanBanNoiBatMau("VB03", "2145/UBND-KGVX", "CONGVAN", "CNTT_HT", "Ủy ban nhân dân tỉnh", "Ủy ban nhân dân tỉnh", DoKhan.DotXuat, -45, "Về việc bảo đảm hạ tầng kỹ thuật phục vụ vận hành hệ thống một cửa điện tử của tỉnh"),
        new VanBanNoiBatMau("VB04", "56/KH-STTTT", "KEHOACH", "KH_DT", "Sở Thông tin và Truyền thông", "Giám đốc Sở", DoKhan.TrongTam, -26, "Kế hoạch xây dựng danh mục dự án đầu tư công lĩnh vực công nghệ thông tin giai đoạn 2026 - 2030"),
        new VanBanNoiBatMau("VB05", "318/STTTT-BCVT", "CONGVAN", "CNTT_BCVT", "Sở Thông tin và Truyền thông", "Lãnh đạo Sở", DoKhan.ThuongXuyen, -33, "Về việc tăng cường quản lý hoạt động bưu chính, viễn thông trên địa bàn tỉnh"),
        new VanBanNoiBatMau("VB06", "1207/BTTTT-CATTT", "CONGVAN", "CNTT_ATTT", "Bộ Thông tin và Truyền thông", "Bộ Thông tin và Truyền thông", DoKhan.DotXuat, -34, "Về việc tăng cường bảo đảm an toàn thông tin mạng cho hệ thống thông tin của cơ quan nhà nước"),
        new VanBanNoiBatMau("VB07", "95/KH-STTTT", "KEHOACH", "CNTT_PM", "Sở Thông tin và Truyền thông", "Giám đốc Sở", DoKhan.TrongTam, -25, "Kế hoạch kiểm thử, nghiệm thu và đưa vào vận hành các phân hệ phần mềm quản lý điều hành"),
        new VanBanNoiBatMau("VB08", "41/TB-STTTT", "THONGBAO", "HC_TCCB", "Sở Thông tin và Truyền thông", "Giám đốc Sở", DoKhan.ThuongXuyen, -12, "Thông báo kết luận của Giám đốc Sở về công tác tổ chức cán bộ quý III năm 2026"),
        new VanBanNoiBatMau("VB09", "276/STTTT-BCVT", "CONGVAN", "CNTT_BCVT", "Sở Thông tin và Truyền thông", "Lãnh đạo Sở", DoKhan.TrongTam, -28, "Về việc rà soát, chỉnh trang hạ tầng cáp viễn thông và trạm thu phát sóng thông tin di động"),
        new VanBanNoiBatMau("VB10", "63/QĐ-STTTT", "QUYETDINH", "HC_VT", "Sở Thông tin và Truyền thông", "Giám đốc Sở", DoKhan.ThuongXuyen, -38, "Quyết định ban hành Quy chế công tác văn thư, lưu trữ của Sở Thông tin và Truyền thông"),
        new VanBanNoiBatMau("VB11", "188/STTTT-KHTC", "CONGVAN", "KH_TC", "Sở Thông tin và Truyền thông", "Giám đốc Sở", DoKhan.TrongTam, -20, "Về việc lập báo cáo quyết toán ngân sách nhà nước năm 2025 và xây dựng dự toán năm 2027"),
        new VanBanNoiBatMau("VB12", "3012/UBND-KGVX", "CONGVAN", "CNTT_PM", "Ủy ban nhân dân tỉnh", "Ủy ban nhân dân tỉnh", DoKhan.DotXuat, -31, "Về việc đẩy nhanh tiến độ xây dựng, hoàn thiện các phần mềm phục vụ chuyển đổi số của tỉnh")
    };

    private static readonly string[] KhongPhoiHop = Array.Empty<string>();

    // ---------------------------------------------------------------------------------
    // 28 nhiem vu DANG SONG — rai het cac o cua ma tran trang thai §2.4:
    //   (3,null) (2,null) (7,null) (1,10) (5,10) (2,12) (6,10) (13,*) (1,11) (97,null)
    //   + (7,null) voi truc C = 12 (gia han bi tu choi) + (2,null) sau khi thu hoi bao cao.
    // ---------------------------------------------------------------------------------
    public static readonly IReadOnlyList<NhiemVuSongMau> NhiemVuSong = new[]
    {
        // --- (3, null) chua tiep nhan: 6 viec ---
        new NhiemVuSongMau("LV01", "VB01", "CNTT_PM", "U13", null, new[] { "U21" }, DoKhan.DotXuat, 3, null, null, 0, -2, 12, KichBanSong.ChuaTiepNhan, "Rà soát danh mục phần mềm dùng chung, đề xuất phương án nâng cấp trong năm 2026"),
        new NhiemVuSongMau("LV02", "VB01", "CNTT_PM", "U13", null, KhongPhoiHop, DoKhan.DotXuat, 3, null, null, 0, -1, 15, KichBanSong.ChuaTiepNhan, "Xây dựng yêu cầu kỹ thuật cho gói thầu nâng cấp phần mềm quản lý văn bản điều hành"),
        new NhiemVuSongMau("LV03", "VB02", "CNTT_HT", "U13", null, KhongPhoiHop, DoKhan.TrongTam, 3, null, null, 0, -4, 20, KichBanSong.ChuaTiepNhan, "Khảo sát hiện trạng thiết bị mạng tại Trung tâm dữ liệu, lập danh mục thiết bị cần thay thế"),
        new NhiemVuSongMau("LV04", "VB03", "CNTT_HT", "U16", null, new[] { "U05" }, DoKhan.ThuongXuyen, 3, null, null, 0, -8, 10, KichBanSong.ChuaTiepNhan, "Kiểm tra, bảo trì định kỳ đường truyền số liệu chuyên dùng phục vụ hệ thống một cửa điện tử"),
        // ChuTriCu: ban ghi phan cong CHUTRI cu da bi THU HOI (§4.3 trangthai = 0), sau do giao lai cho U20
        new NhiemVuSongMau("LV05", "VB04", "KH_DT", "U20", "U10", KhongPhoiHop, DoKhan.TrongTam, 3, null, null, 0, -3, 21, KichBanSong.ChuaTiepNhan, "Tổng hợp nhu cầu đầu tư công nghệ thông tin của các đơn vị, lập danh mục dự án giai đoạn 2026 - 2030"),
        new NhiemVuSongMau("LV06", "VB05", "CNTT_BCVT", "U12", null, KhongPhoiHop, DoKhan.ThuongXuyen, 3, null, null, 0, -5, 30, KichBanSong.ChuaTiepNhan, "Kiểm tra chất lượng dịch vụ bưu chính công ích tại các điểm phục vụ trên địa bàn tỉnh"),

        // --- (2, null) dang trien khai: 7 viec ---
        new NhiemVuSongMau("LV07", "VB12", "CNTT_PM", "U13", null, KhongPhoiHop, DoKhan.DotXuat, 2, null, null, 35, -10, 20, KichBanSong.DangLam, "Khắc phục lỗi hiển thị báo cáo thống kê trên phần mềm quản lý nhiệm vụ"),
        new NhiemVuSongMau("LV08", "VB06", "CNTT_ATTT", "U13", null, new[] { "U07", "U21" }, DoKhan.DotXuat, 2, null, null, 60, -14, 25, KichBanSong.DangLam, "Rà soát, đánh giá an toàn thông tin cho hệ thống một cửa điện tử trước khi nâng cấp"),
        new NhiemVuSongMau("LV09", "VB02", "CNTT_HT", "U13", null, KhongPhoiHop, DoKhan.TrongTam, 2, null, null, 10, -6, 30, KichBanSong.DangLam, "Xây dựng phương án sao lưu và khôi phục dữ liệu cho các hệ thống dùng chung của tỉnh"),
        new NhiemVuSongMau("LV10", "VB02", "CNTT_HT", "U05", null, new[] { "U16" }, DoKhan.DotXuat, 2, null, null, 80, -18, 25, KichBanSong.DangLam, "Triển khai mở rộng hệ thống hội nghị truyền hình trực tuyến tới 12 đơn vị trực thuộc"),
        new NhiemVuSongMau("LV11", "VB04", "KH_DT", "U10", null, new[] { "U11" }, DoKhan.TrongTam, 2, null, null, 60, -20, 30, KichBanSong.DangLam, "Lập báo cáo đề xuất chủ trương đầu tư dự án Trung tâm điều hành thông minh của tỉnh"),
        new NhiemVuSongMau("LV12", "VB07", "CNTT_PM", "U21", null, KhongPhoiHop, DoKhan.ThuongXuyen, 2, null, null, 45, -12, 21, KichBanSong.DangLam, "Xây dựng tài liệu hướng dẫn sử dụng phân hệ báo cáo thống kê cho cán bộ cấp xã"),
        new NhiemVuSongMau("LV13", "VB08", "HC_TCCB", "U09", null, new[] { "U22" }, DoKhan.ThuongXuyen, 2, null, null, 20, -9, 25, KichBanSong.DangLam, "Rà soát, cập nhật hồ sơ công chức, viên chức trên phần mềm quản lý cán bộ của Sở"),

        // --- (7, null) qua han: 3 viec, deu cua U17 (chan dung f) ---
        new NhiemVuSongMau("LV14", "VB05", "CNTT_BCVT", "U17", null, KhongPhoiHop, DoKhan.TrongTam, 7, null, null, 50, -30, 21, KichBanSong.QuaHan, "Tổng hợp số liệu phát triển hạ tầng viễn thông quý III phục vụ báo cáo Bộ Thông tin và Truyền thông"),
        new NhiemVuSongMau("LV15", "VB09", "CNTT_BCVT", "U17", null, KhongPhoiHop, DoKhan.ThuongXuyen, 7, null, null, 25, -25, 20, KichBanSong.QuaHan, "Thẩm định hồ sơ xin cấp phép xây dựng 08 trạm thu phát sóng thông tin di động"),
        new NhiemVuSongMau("LV16", "VB03", "CNTT_HT", "U17", null, KhongPhoiHop, DoKhan.ThuongXuyen, 7, null, null, 70, -40, 28, KichBanSong.QuaHan, "Bổ sung thiết bị lưu điện cho phòng máy chủ của Trung tâm dữ liệu tỉnh"),

        // --- (1, 10) va (5, 10) cho nguoi giao kiem tra ket qua ---
        new NhiemVuSongMau("LV17", "VB07", "CNTT_PM", "U06", null, KhongPhoiHop, DoKhan.TrongTam, 1, 10, null, 100, -22, 25, KichBanSong.ChoXacNhan, "Kiểm thử chức năng đồng bộ dữ liệu giữa Cổng dịch vụ công tỉnh và Cổng dịch vụ công quốc gia"),
        new NhiemVuSongMau("LV18", "VB10", "HC_VT", "U08", null, new[] { "U14" }, DoKhan.ThuongXuyen, 1, 10, null, 100, -26, 30, KichBanSong.ChoXacNhan, "Số hoá và nhập cơ sở dữ liệu hồ sơ lưu trữ của Sở giai đoạn 2015 - 2020"),
        new NhiemVuSongMau("LV19", "VB11", "KH_TC", "U11", null, new[] { "U20" }, DoKhan.DotXuat, 1, 10, null, 100, -15, 20, KichBanSong.ChoXacNhan, "Lập báo cáo quyết toán ngân sách nhà nước năm 2025 của Sở Thông tin và Truyền thông"),
        new NhiemVuSongMau("LV20", "VB10", "HC_VT", "U14", null, KhongPhoiHop, DoKhan.ThuongXuyen, 5, 10, null, 100, -35, 25, KichBanSong.ChoXacNhanSauHan, "Chỉnh lý tài liệu tồn đọng tại kho lưu trữ cơ quan, lập mục lục hồ sơ"),

        // --- (2, 12) bi tra lai, dang lam lai ---
        new NhiemVuSongMau("LV21", "VB12", "CNTT_PM", "U13", null, KhongPhoiHop, DoKhan.DotXuat, 2, 12, null, 70, -28, 30, KichBanSong.BiTraLai, "Bổ sung chức năng xuất báo cáo theo đơn vị cho phần mềm quản lý nhiệm vụ"),
        new NhiemVuSongMau("LV22", "VB03", "CNTT_HT", "U21", null, KhongPhoiHop, DoKhan.TrongTam, 2, 12, null, 85, -32, 35, KichBanSong.BiTraLai, "Rà soát cấu hình tường lửa và phân vùng mạng của hệ thống một cửa điện tử"),

        // --- (6, 10) tu choi, cho nguoi giao xu ly (§2.4 T4/T5) ---
        new NhiemVuSongMau("LV23", "VB10", "HC_VT", "U22", null, KhongPhoiHop, DoKhan.ThuongXuyen, 6, 10, null, 0, -3, 20, KichBanSong.TuChoi, "Kiểm tra việc chấp hành quy định về thể thức và kỹ thuật trình bày văn bản hành chính tại các phòng"),

        // --- (13, *) dang xin gia han ---
        new NhiemVuSongMau("LV24", "VB09", "CNTT_BCVT", "U17", null, new[] { "U12" }, DoKhan.TrongTam, 13, null, 10, 40, -24, 25, KichBanSong.XinGiaHan, "Rà soát, chỉnh trang hệ thống cáp viễn thông treo trên các tuyến phố trung tâm"),

        // --- (1, 11) da nghiem thu — diem cuoi ---
        new NhiemVuSongMau("LV25", "VB06", "CNTT_ATTT", "U07", null, new[] { "U19" }, DoKhan.TrongTam, 1, 11, null, 100, -30, 28, KichBanSong.DaNghiemThu, "Xây dựng hồ sơ đề xuất cấp độ an toàn hệ thống thông tin cho Cổng thông tin điện tử tỉnh"),

        // --- (97, null) da thu hoi — diem cuoi ---
        new NhiemVuSongMau("LV26", "VB11", "KH_TC", "U20", null, KhongPhoiHop, DoKhan.ThuongXuyen, 97, null, null, 15, -16, 30, KichBanSong.DaThuHoi, "Kiểm tra hồ sơ thanh toán các gói thầu mua sắm thiết bị công nghệ thông tin năm 2025"),

        // --- (7, null) + truc C = 12: nguoi giao TU CHOI gia han (§2.4 T13) ---
        new NhiemVuSongMau("LV27", "VB11", "KH_TC", "U20", null, KhongPhoiHop, DoKhan.TrongTam, 7, null, 12, 55, -34, 25, KichBanSong.GiaHanBiTuChoi, "Đối chiếu số liệu quyết toán kinh phí công nghệ thông tin của các đơn vị trực thuộc năm 2025"),

        // --- (2, null) sau khi nguoi thuc hien THU HOI BAO CAO (§2.4 T8) ---
        new NhiemVuSongMau("LV28", "VB08", "HC_TCCB", "U22", null, KhongPhoiHop, DoKhan.ThuongXuyen, 2, null, null, 90, -18, 30, KichBanSong.ThuHoiBaoCao, "Tổng hợp kết quả đánh giá, xếp loại chất lượng công chức, viên chức 6 tháng đầu năm 2026")
    };

    /// <summary>12 nhiem vu dang song duoc gan nhat ky goi y AI (§9.8).</summary>
    public static readonly IReadOnlyList<string> NhiemVuSongCoLogAi = new[]
    {
        "LV01", "LV02", "LV05", "LV06", "LV07", "LV10", "LV11", "LV12", "LV13", "LV17", "LV18", "LV19"
    };
}
