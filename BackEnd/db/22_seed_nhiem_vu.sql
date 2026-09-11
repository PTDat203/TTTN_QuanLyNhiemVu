--------------------------------------------------------------------------------
-- 22_seed_nhiem_vu.sql
-- Nạp lại dữ liệu nhiệm vụ sau khi 21_seed_to_chuc.sql xoá sạch bảng TASKS.
-- Chạy bằng tài khoản TASK_APP, SAU KHI đã chạy 21_seed_to_chuc.sql.
--
-- VÌ SAO CẦN FILE NÀY
--   Script 21 dựng lại cơ cấu tổ chức và đã xoá toàn bộ 47 nhiệm vụ cũ cùng tiến
--   độ, báo cáo, tệp đính kèm. Dữ liệu cũ gắn với 10 người dùng theo mô hình hai
--   vai trò nên không dùng lại được với cơ cấu bốn cấp. File này nạp bộ mới.
--
-- DỮ LIỆU ĐƯỢC THIẾT KẾ CHỨ KHÔNG SINH NGẪU NHIÊN
--   Phần AI xếp hạng dựa vào lịch sử. Nếu mọi người đều đúng hạn như nhau thì
--   OnTimeScore, HistoricalPerformance và WorkloadScore đều bằng nhau và không
--   phân biệt được ai với ai — lúc đó không đánh giá được mô hình.
--   Nên dữ liệu dưới đây cố tình tạo chênh lệch:
--
--   1. TỶ LỆ ĐÚNG HẠN khác nhau rõ rệt giữa các nhân viên
--        Hoàng Văn Giang (9)  100%    Nguyễn Văn An (5)      100%
--        Ngô Thị Lan (12)     100%    Lê Thị Bình (6)        100%
--        Đinh Thị Nga (14)    100%    Phạm Minh Cường (7)     88%
--        Đỗ Thị Hoà (10)       83%    Vũ Đình Khánh (11)      60%
--        Trịnh Hồng Phúc (15)  60%    Trần Thị Dung (8)       50%
--        Bùi Nhật Minh (13)    40%
--
--   2. KHỐI LƯỢNG ĐANG GÁNH chênh lệch mạnh trong phòng Phát triển
--        Phạm Minh Cường (7) đang giữ 5 nhiệm vụ, trong đó 2 mức HIGH.
--        Trần Thị Dung (8) chỉ giữ 2 nhiệm vụ mức MEDIUM.
--      Cường giỏi hơn Dung về kỹ năng (C#/ASP.NET Core mức 4 so với mức 3) nhưng
--      đang quá tải. Đây chính là tình huống cần AI đẩy việc xuống người kế tiếp.
--
--   3. CÓ LỊCH SỬ LÀM LẠI: 5 nhiệm vụ từng bị trả về trước khi được duyệt,
--      để phân biệt người làm một lần xong với người phải sửa nhiều lần.
--
--   4. BỐN NHIỆM VỤ Ở TRẠNG THÁI MOI_TAO, chưa có người thực hiện và cố tình
--      để DEPARTMENT_ID rỗng. Đây là đầu vào để thử phần AI đoán phòng ban:
--        ID 54  giao diện thống kê      -> mong đợi phòng Phát triển, nhóm Frontend
--        ID 55  API bằng ASP.NET Core   -> mong đợi Cường giỏi nhất nhưng quá tải,
--                                          nên AI phải đẩy xuống Dung
--        ID 90  tối ưu truy vấn Oracle  -> mong đợi Giang, chuyên gia cơ sở dữ liệu
--        ID 89  đánh giá đầu tư công nghệ -> CỐ TÌNH MƠ HỒ. Vừa là chuyện tài chính
--                                          của phòng Hành chính, vừa là chuyện công
--                                          nghệ của phòng Phát triển. Dùng để thử
--                                          nhánh "không chắc chắn" của ngưỡng tin
--                                          cậy: AI phải giữ cả hai phòng và cảnh
--                                          báo, chứ không được chọn bừa một phòng.
--
-- Ý NGHĨA CỦA TASKS.DEPARTMENT_ID: PHÒNG THỰC THI, không phải phòng ra lệnh.
--   Đây là cột dùng để lọc ứng viên nên nó phải trỏ tới phòng có người làm việc đó.
--   Vì vậy các nhiệm vụ Giám đốc giao cho trưởng phòng (ID 84-88) mang mã phòng của
--   chính trưởng phòng đó, không mang mã Ban Giám đốc. Ban Giám đốc chỉ có một người
--   và người đó giao việc chứ không nhận việc, nên phòng này không có nhiệm vụ nào.
--
--   5. GIỮ LẠI CA THỬ SAI: ID 78 "Chuẩn bị tiệc tất niên" nằm ở phòng Hành chính.
--      Bản TF-IDF cũ từng chấm nhiệm vụ này khớp kỹ năng 0,109 với dân kỹ thuật do
--      trùng từ vụn. Giữ lại để so bản nhúng ngữ nghĩa có mắc lại lỗi đó không.
--
-- CHẠY LẠI ĐƯỢC: xoá sạch 4 bảng nhiệm vụ rồi nạp lại với ID cố định.
-- KHÔNG đụng tới USERS, DEPARTMENTS, USER_SKILLS, USER_QUALIFICATIONS.
-- SAU FILE NÀY PHẢI CHẠY LẠI 24_seed_v2.sql: xoá nhiệm vụ là mất luôn kỹ năng yêu cầu
-- (ON DELETE CASCADE), nhóm phụ trách, giờ công ước lượng và điểm đánh giá.
-- CẢNH BÁO: xoá toàn bộ dữ liệu nhiệm vụ. Chỉ chạy trên máy phát triển.
--------------------------------------------------------------------------------

SET DEFINE OFF
SET SERVEROUTPUT ON

--==============================================================================
-- BƯỚC 1: Xoá dữ liệu nhiệm vụ cũ theo đúng thứ tự phụ thuộc khoá ngoại
--==============================================================================
DELETE FROM TASK_ATTACHMENTS;
DELETE FROM TASK_REPORTS;
DELETE FROM TASK_PROGRESS;
DELETE FROM TASKS;
COMMIT;

--==============================================================================
-- BƯỚC 2: Nạp 90 nhiệm vụ
--
-- Dùng thủ tục cục bộ nv() cho gọn. Ngày tạo lấy theo ngày bắt đầu để trục thời
-- gian nhất quán — phần đánh giá mô hình sau này cần tách theo mốc thời gian,
-- chấm nhiệm vụ ngày X thì chỉ được dùng dữ liệu có trước ngày X.
--==============================================================================
DECLARE
  PROCEDURE nv(p_id NUMBER, p_title VARCHAR2, p_desc VARCHAR2,
               p_creator NUMBER, p_assignee NUMBER, p_pri VARCHAR2,
               p_status VARCHAR2, p_start DATE, p_due DATE, p_dept NUMBER) IS
  BEGIN
    INSERT INTO TASKS (ID, TITLE, DESCRIPTION, CREATOR_ID, ASSIGNEE_ID, PRIORITY,
                       STATUS_CODE, START_DATE, DUE_DATE, DEPARTMENT_ID,
                       CREATED_AT, UPDATED_AT)
    VALUES (p_id, p_title, p_desc, p_creator, p_assignee, p_pri, p_status,
            p_start, p_due, p_dept,
            CAST(p_start AS TIMESTAMP), CAST(p_due AS TIMESTAMP));
  END;
BEGIN

------------------------------------------------------------------------------
-- PHÒNG PHÁT TRIỂN (2) — nhóm Backend, đã hoàn thành
------------------------------------------------------------------------------
nv(1, 'Xây dựng API đăng nhập và cấp phát mã thông báo JWT',
   'Thiết kế điểm cuối xác thực bằng tài khoản nội bộ. Sinh access token hạn 30 phút kèm refresh token xoay vòng. Mật khẩu băm bằng BCrypt hệ số 11. Trả về mã lỗi rõ ràng khi sai tài khoản hoặc tài khoản bị khoá.',
   5, 7, 'HIGH', 'HOAN_THANH', DATE '2026-03-16', DATE '2026-03-27', 2);

nv(2, 'Tối ưu câu truy vấn danh sách nhiệm vụ bị chậm',
   'Trang danh sách mất hơn bốn giây khi bảng vượt mười nghìn dòng. Phân tích kế hoạch thực thi, tìm chỗ quét toàn bảng, bổ sung chỉ mục phù hợp và viết lại câu lệnh phân trang theo cú pháp OFFSET FETCH của Oracle.',
   5, 9, 'HIGH', 'HOAN_THANH', DATE '2026-03-18', DATE '2026-04-03', 2);

nv(3, 'Viết thủ tục sao lưu cơ sở dữ liệu Oracle hằng đêm',
   'Lập lịch sao lưu toàn phần mỗi tuần và sao lưu tăng dần hằng đêm bằng RMAN. Ghi nhật ký kết quả và gửi cảnh báo khi thất bại. Kiểm chứng khôi phục được từ bản sao lưu.',
   5, 9, 'MEDIUM', 'HOAN_THANH', DATE '2026-04-06', DATE '2026-04-17', 2);

nv(4, 'Sửa lỗi tạo trùng bản ghi khi gọi API hai lần liên tiếp',
   'Người dùng bấm nút lưu hai lần thì hệ thống tạo hai nhiệm vụ giống hệt nhau. Bổ sung khoá chống trùng theo mã yêu cầu và ràng buộc duy nhất ở tầng cơ sở dữ liệu.',
   5, 7, 'HIGH', 'HOAN_THANH', DATE '2026-04-08', DATE '2026-04-20', 2);

nv(5, 'Bổ sung phân trang cho API danh sách người dùng',
   'API đang trả về toàn bộ người dùng trong một lần gọi. Thêm tham số trang và kích thước trang, trả kèm tổng số dòng và tổng số trang. Đặt giới hạn tối đa cho kích thước trang.',
   5, 8, 'MEDIUM', 'HOAN_THANH', DATE '2026-04-13', DATE '2026-04-24', 2);

nv(6, 'Thiết kế bảng lưu lịch sử thay đổi trạng thái nhiệm vụ',
   'Cần truy vết được ai đổi trạng thái nào, lúc nào và vì sao. Thiết kế bảng lịch sử ghi trạng thái trước, trạng thái sau, người thực hiện và thời điểm. Bổ sung chỉ mục theo mã nhiệm vụ.',
   5, 9, 'MEDIUM', 'HOAN_THANH', DATE '2026-04-27', DATE '2026-05-08', 2);

nv(7, 'Tích hợp gửi thư điện tử thông báo khi được giao việc',
   'Khi một nhiệm vụ được giao, gửi thư cho người thực hiện kèm tiêu đề, hạn hoàn thành và đường dẫn tới nhiệm vụ. Dùng mẫu thư tách riêng để đổi nội dung không phải sửa mã nguồn.',
   5, 7, 'MEDIUM', 'HOAN_THANH', DATE '2026-05-04', DATE '2026-05-15', 2);

nv(8, 'Khắc phục lỗi tràn bộ nhớ khi xuất báo cáo Excel dung lượng lớn',
   'Xuất báo cáo trên năm mươi nghìn dòng làm tiến trình chiếm hết bộ nhớ rồi dừng. Chuyển sang ghi theo luồng và xử lý từng lô thay vì dựng toàn bộ bảng trong bộ nhớ.',
   5, 7, 'HIGH', 'HOAN_THANH', DATE '2026-05-11', DATE '2026-05-22', 2);

nv(9, 'Chuẩn hoá mã lỗi trả về của toàn bộ API',
   'Mỗi điểm cuối đang trả về một dạng lỗi khác nhau nên giao diện khó xử lý. Định nghĩa bộ mã lỗi thống nhất, gói trong một lớp phản hồi chung và áp dụng qua middleware bắt lỗi tập trung.',
   5, 8, 'LOW', 'HOAN_THANH', DATE '2026-05-18', DATE '2026-05-29', 2);

nv(10, 'Viết kiểm thử đơn vị cho tầng dịch vụ nhiệm vụ',
   'Phủ các nhánh nghiệp vụ chính của dịch vụ nhiệm vụ: tạo, giao, tiếp nhận, cập nhật tiến độ và chuyển trạng thái. Dùng cơ sở dữ liệu trong bộ nhớ để kiểm thử chạy độc lập.',
   5, 8, 'MEDIUM', 'HOAN_THANH', DATE '2026-05-25', DATE '2026-06-05', 2);

nv(11, 'Thiết kế kiến trúc phân hệ báo cáo thống kê',
   'Xác định các chỉ số cần thống kê theo phòng ban, theo người và theo khoảng thời gian. Chọn giữa tính trực tiếp và bảng tổng hợp dựng sẵn. Mô tả luồng dữ liệu và định dạng phản hồi.',
   2, 5, 'HIGH', 'HOAN_THANH', DATE '2026-06-01', DATE '2026-06-12', 2);

nv(12, 'Di trú dữ liệu từ hệ thống cũ sang Oracle',
   'Ánh xạ lược đồ cũ sang lược đồ mới, viết kịch bản chuyển đổi và đối chiếu số dòng cùng tổng kiểm tra sau khi chuyển. Lập phương án quay lui nếu đối chiếu không khớp.',
   5, 9, 'MEDIUM', 'HOAN_THANH', DATE '2026-06-08', DATE '2026-06-19', 2);

nv(13, 'Xây dựng API thống kê khối lượng công việc theo phòng ban',
   'Trả về số nhiệm vụ đang mở, đã hoàn thành và quá hạn của từng phòng ban trong khoảng thời gian tuỳ chọn. Cho phép lọc theo mức ưu tiên.',
   5, 7, 'MEDIUM', 'HOAN_THANH', DATE '2026-06-15', DATE '2026-06-26', 2);

nv(14, 'Rà soát lỗ hổng bảo mật tầng API',
   'Kiểm tra chèn câu lệnh SQL, lộ dữ liệu qua thông báo lỗi, thiếu kiểm quyền ở từng điểm cuối và cấu hình chia sẻ tài nguyên giữa các nguồn. Lập danh sách rủi ro kèm mức độ và cách khắc phục.',
   2, 5, 'HIGH', 'HOAN_THANH', DATE '2026-06-22', DATE '2026-07-03', 2);

nv(15, 'Ghi nhật ký thao tác người dùng phục vụ truy vết',
   'Ghi lại các thao tác thay đổi dữ liệu kèm người thực hiện, thời điểm và địa chỉ mạng. Tách nhật ký nghiệp vụ khỏi nhật ký kỹ thuật. Đặt chính sách xoá nhật ký cũ hơn một năm.',
   5, 8, 'LOW', 'HOAN_THANH', DATE '2026-06-29', DATE '2026-07-10', 2);

nv(16, 'Xử lý bất đồng bộ cho hàng đợi gửi thông báo',
   'Việc gửi thư đang làm chậm phản hồi của API giao việc. Tách sang hàng đợi nền, có cơ chế thử lại khi thất bại và giới hạn số lần thử.',
   5, 7, 'MEDIUM', 'HOAN_THANH', DATE '2026-07-06', DATE '2026-07-17', 2);

nv(17, 'Khắc phục hiện tượng khoá bảng khi nhiều người cập nhật đồng thời',
   'Hai người cùng cập nhật một nhiệm vụ thì phiên sau bị treo cho tới khi hết thời gian chờ. Áp dụng khoá lạc quan bằng cột phiên bản và trả lỗi xung đột rõ ràng cho giao diện.',
   5, 9, 'HIGH', 'HOAN_THANH', DATE '2026-07-13', DATE '2026-07-24', 2);

nv(18, 'Bổ sung kiểm tra hợp lệ dữ liệu đầu vào cho API',
   'Kiểm tra độ dài chuỗi, khoảng giá trị số, định dạng ngày và quan hệ giữa ngày bắt đầu với hạn hoàn thành. Trả về danh sách lỗi theo từng trường thay vì dừng ở lỗi đầu tiên.',
   5, 8, 'MEDIUM', 'HOAN_THANH', DATE '2026-07-20', DATE '2026-07-31', 2);

nv(19, 'Xây dựng cơ chế làm mới mã thông báo an toàn',
   'Refresh token phải xoay vòng sau mỗi lần dùng và thu hồi được khi phát hiện dùng lại. Lưu trữ theo phiên và cho phép đăng xuất khỏi mọi thiết bị.',
   5, 7, 'HIGH', 'HOAN_THANH', DATE '2026-07-27', DATE '2026-08-07', 2);

nv(20, 'Phân tích và bổ sung chỉ mục cho các bảng nghiệp vụ',
   'Rà soát toàn bộ khoá ngoại chưa có chỉ mục và các cột dùng trong mệnh đề lọc thường xuyên. Cân nhắc chi phí ghi khi thêm chỉ mục. Đo lại thời gian truy vấn trước và sau.',
   5, 9, 'MEDIUM', 'HOAN_THANH', DATE '2026-08-03', DATE '2026-08-14', 2);

nv(21, 'Chuẩn hoá quy ước đặt tên trong mã nguồn Backend',
   'Thống nhất cách đặt tên lớp, phương thức, biến và tên bảng ánh xạ. Viết tài liệu quy ước và bổ sung kiểm tra tự động trong quy trình tích hợp.',
   2, 5, 'MEDIUM', 'HOAN_THANH', DATE '2026-08-10', DATE '2026-08-21', 2);

nv(22, 'Viết tài liệu mô tả API bằng Swagger',
   'Bổ sung chú thích cho từng điểm cuối gồm tham số, mã trạng thái trả về và ví dụ. Bật nút xác thực trên giao diện Swagger để thử được API cần đăng nhập.',
   5, 8, 'MEDIUM', 'HOAN_THANH', DATE '2026-08-17', DATE '2026-08-28', 2);

nv(23, 'Tách tầng truy cập dữ liệu khỏi tầng nghiệp vụ',
   'Mã truy vấn đang nằm lẫn trong lớp dịch vụ khiến khó kiểm thử. Đưa về các lớp truy cập riêng, để lớp dịch vụ chỉ giữ luật nghiệp vụ.',
   5, 7, 'MEDIUM', 'HOAN_THANH', DATE '2026-08-24', DATE '2026-09-04', 2);

nv(24, 'Lựa chọn công nghệ nền cho hệ thống mới',
   'So sánh các lựa chọn về khung ứng dụng phía máy chủ, thư viện truy cập dữ liệu và cơ sở dữ liệu. Đánh giá theo mức độ phù hợp với đội ngũ hiện có, tài liệu và khả năng bảo trì lâu dài.',
   2, 5, 'HIGH', 'HOAN_THANH', DATE '2026-04-20', DATE '2026-05-01', 2);

------------------------------------------------------------------------------
-- PHÒNG PHÁT TRIỂN (2) — nhóm Backend, đang mở
-- Cường (7) giữ 5 việc: đây là ca quá tải để thử cơ chế đẩy xuống người kế tiếp
------------------------------------------------------------------------------
nv(25, 'Xây dựng API gợi ý người thực hiện phù hợp',
   'Nhận mã nhiệm vụ, trả về danh sách ứng viên kèm điểm tổng và điểm thành phần. Có tham số giới hạn số ứng viên trả về. Kèm lý do gợi ý cho từng người.',
   5, 7, 'HIGH', 'DANG_THUC_HIEN', DATE '2026-09-01', DATE '2026-09-18', 2);

nv(26, 'Bổ sung bộ nhớ đệm cho các bảng danh mục',
   'Bảng trạng thái, phòng ban và kỹ năng gần như không đổi nhưng đang bị truy vấn ở mọi yêu cầu. Nạp vào bộ nhớ khi khởi động và làm mới khi có thay đổi.',
   5, 7, 'MEDIUM', 'DANG_THUC_HIEN', DATE '2026-09-02', DATE '2026-09-22', 2);

nv(27, 'Chuyển đổi cách lưu trữ tệp đính kèm',
   'Tệp đang lưu trực tiếp trong thư mục ứng dụng nên khó sao lưu và không mở rộng được. Chuyển sang lưu ngoài, chỉ giữ đường dẫn và siêu dữ liệu trong cơ sở dữ liệu.',
   5, 7, 'MEDIUM', 'DA_GIAO', DATE '2026-09-03', DATE '2026-09-25', 2);

nv(28, 'Xây dựng dịch vụ nhúng véc-tơ ngữ nghĩa cho văn bản tiếng Việt',
   'Dựng dịch vụ nhận văn bản và trả về véc-tơ nhúng. Dùng mô hình đa ngữ đã huấn luyện sẵn, không tự huấn luyện. Có bộ nhớ đệm để không tính lại véc-tơ của hồ sơ không đổi.',
   2, 7, 'HIGH', 'DA_GIAO', DATE '2026-09-07', DATE '2026-09-30', 2);

nv(29, 'Thiết kế lược đồ lưu kỹ năng chuẩn hoá',
   'Kỹ năng đang lưu dạng chuỗi tự do nên cùng một kỹ năng có nhiều cách viết khác nhau. Tách thành bảng danh mục có mã chuẩn và bảng nối với người dùng.',
   5, 7, 'MEDIUM', 'DA_GIAO', DATE '2026-09-08', DATE '2026-10-02', 2);

nv(30, 'Kiểm thử hiệu năng API dưới tải cao',
   'Dựng kịch bản mô phỏng hai trăm người dùng đồng thời. Đo thời gian phản hồi trung vị và phân vị 95. Xác định điểm nghẽn ở tầng ứng dụng hay tầng cơ sở dữ liệu.',
   5, 8, 'MEDIUM', 'DANG_THUC_HIEN', DATE '2026-09-01', DATE '2026-09-20', 2);

nv(31, 'Viết kiểm thử tích hợp cho luồng duyệt kết quả',
   'Phủ trọn vòng nghiệp vụ: nộp báo cáo, quản lý trả về yêu cầu bổ sung, nộp lại, được duyệt. Kiểm tra trạng thái nhiệm vụ đổi đúng ở từng bước.',
   5, 8, 'MEDIUM', 'YEU_CAU_BO_SUNG', DATE '2026-09-05', DATE '2026-09-24', 2);

nv(32, 'Thiết kế mô hình dữ liệu cho hồ sơ năng lực nhân sự',
   'Mô hình hoá bằng cấp, chuyên ngành, kỹ năng kèm mức thành thạo và số năm kinh nghiệm. Cân nhắc giữa gộp vào bảng người dùng và tách bảng riêng.',
   2, 9, 'HIGH', 'DANG_THUC_HIEN', DATE '2026-08-31', DATE '2026-09-16', 2);

nv(33, 'Viết kịch bản khôi phục cơ sở dữ liệu',
   'Soạn quy trình khôi phục từ bản sao lưu gần nhất kèm thời gian dự kiến. Diễn tập trên môi trường thử và ghi lại từng bước cùng thời gian thực tế.',
   5, 9, 'MEDIUM', 'CHO_XAC_NHAN', DATE '2026-09-04', DATE '2026-09-21', 2);

nv(34, 'Rà soát mã nguồn của nhóm Backend',
   'Rà soát các thay đổi trong tháng: kiểm tra xử lý ngoại lệ, rò rỉ tài nguyên, truy vấn lồng trong vòng lặp và chỗ thiếu kiểm quyền. Ghi nhận xét theo từng tệp.',
   2, 5, 'HIGH', 'DANG_THUC_HIEN', DATE '2026-09-02', DATE '2026-09-19', 2);

------------------------------------------------------------------------------
-- PHÒNG PHÁT TRIỂN (2) — nhóm Frontend, đã hoàn thành
------------------------------------------------------------------------------
nv(35, 'Dựng màn hình danh sách nhiệm vụ bằng Angular',
   'Dựng bảng danh sách có phân trang, lọc theo trạng thái và mức ưu tiên, sắp xếp theo hạn hoàn thành. Dùng thành phần độc lập và tín hiệu để quản lý trạng thái.',
   6, 10, 'HIGH', 'HOAN_THANH', DATE '2026-05-04', DATE '2026-05-15', 2);

nv(36, 'Xây dựng màn hình chi tiết và cập nhật tiến độ',
   'Hiển thị đầy đủ thông tin nhiệm vụ, dòng thời gian tiến độ và các báo cáo đã nộp. Cho phép người thực hiện cập nhật phần trăm hoàn thành kèm ghi chú.',
   6, 10, 'MEDIUM', 'HOAN_THANH', DATE '2026-05-18', DATE '2026-05-29', 2);

nv(37, 'Thiết kế bộ thành phần giao diện dùng chung',
   'Xây dựng nút bấm, ô nhập, hộp thoại, nhãn trạng thái và bảng dữ liệu dùng lại được. Thống nhất bảng màu, khoảng cách và kiểu chữ trên toàn ứng dụng.',
   2, 6, 'HIGH', 'HOAN_THANH', DATE '2026-06-01', DATE '2026-06-12', 2);

nv(38, 'Hiển thị biểu đồ đóng góp điểm của gợi ý',
   'Vẽ thanh nhiều màu thể hiện từng thành phần điểm đóng góp bao nhiêu vào điểm tổng. Chú giải rõ tên thành phần và giá trị. Người xem phải hiểu được vì sao ứng viên này xếp trên.',
   6, 10, 'MEDIUM', 'HOAN_THANH', DATE '2026-06-15', DATE '2026-06-26', 2);

nv(39, 'Kiểm thử giao diện trên trình duyệt di động',
   'Kiểm tra bố cục trên màn hình hẹp, thao tác chạm, bàn phím ảo che ô nhập và tốc độ tải qua mạng di động. Lập danh sách lỗi kèm ảnh chụp màn hình.',
   6, 11, 'MEDIUM', 'HOAN_THANH', DATE '2026-06-22', DATE '2026-07-03', 2);

nv(40, 'Tối ưu thời gian tải trang đầu tiên',
   'Gói tài nguyên đang nặng làm trang đầu tải chậm. Tách gói theo tuyến đường, nạp trễ các màn hình ít dùng và nén tài nguyên tĩnh. Đo lại bằng công cụ đo hiệu năng.',
   2, 6, 'MEDIUM', 'HOAN_THANH', DATE '2026-07-06', DATE '2026-07-17', 2);

nv(41, 'Viết kịch bản kiểm thử hồi quy',
   'Soạn bộ kịch bản phủ các luồng chính để chạy lại trước mỗi lần phát hành. Ghi rõ bước thực hiện, dữ liệu đầu vào và kết quả mong đợi.',
   6, 11, 'LOW', 'HOAN_THANH', DATE '2026-07-13', DATE '2026-07-24', 2);

nv(42, 'Xây dựng biểu mẫu tạo nhiệm vụ có gợi ý trực tiếp',
   'Khi người dùng nhập xong tiêu đề và mô tả, gọi API gợi ý và hiển thị danh sách ứng viên ngay bên cạnh biểu mẫu. Cho chọn nhanh một ứng viên để điền vào ô người thực hiện.',
   6, 10, 'MEDIUM', 'HOAN_THANH', DATE '2026-07-20', DATE '2026-07-31', 2);

nv(43, 'Kiểm thử luồng giao việc từ đầu đến cuối',
   'Chạy trọn kịch bản từ lúc quản lý tạo nhiệm vụ, giao việc, nhân viên tiếp nhận, cập nhật tiến độ, nộp báo cáo cho tới khi được duyệt. Kiểm tra cả nhánh bị trả về.',
   6, 11, 'MEDIUM', 'HOAN_THANH', DATE '2026-08-03', DATE '2026-08-14', 2);

nv(44, 'Áp dụng thiết kế đáp ứng cho màn hình danh sách',
   'Bảng dữ liệu đang tràn ngang trên màn hình hẹp. Chuyển sang dạng thẻ khi màn hình dưới sáu trăm điểm ảnh, giữ nguyên bảng ở màn hình rộng.',
   6, 10, 'MEDIUM', 'HOAN_THANH', DATE '2026-08-10', DATE '2026-08-21', 2);

nv(45, 'Chuyển sang cơ chế phát hiện thay đổi không dùng Zone',
   'Bỏ zone.js và chuyển toàn bộ trạng thái sang tín hiệu. Rà soát các chỗ cập nhật giao diện ngoài vòng phát hiện thay đổi để không bị mất cập nhật.',
   2, 6, 'MEDIUM', 'HOAN_THANH', DATE '2026-08-17', DATE '2026-08-28', 2);

nv(46, 'Xử lý hiển thị thông báo lỗi thống nhất toàn ứng dụng',
   'Gom việc hiển thị lỗi về một chỗ qua bộ chặn HTTP. Phân biệt lỗi mạng, lỗi xác thực và lỗi nghiệp vụ để hiện thông điệp phù hợp thay vì báo lỗi kỹ thuật.',
   6, 10, 'MEDIUM', 'HOAN_THANH', DATE '2026-08-24', DATE '2026-09-04', 2);

nv(47, 'Kiểm thử chức năng đăng nhập và phân quyền',
   'Kiểm tra đăng nhập đúng và sai, hết hạn phiên, làm mới mã thông báo, và việc mỗi vai trò chỉ thấy được phần chức năng của mình. Thử cả trường hợp gọi thẳng API không qua giao diện.',
   6, 11, 'MEDIUM', 'HOAN_THANH', DATE '2026-05-11', DATE '2026-05-22', 2);

nv(48, 'Rà soát khả năng truy cập của giao diện',
   'Kiểm tra độ tương phản màu, thao tác được bằng bàn phím, nhãn cho trình đọc màn hình và thứ tự tiêu điểm. Lập danh sách điểm chưa đạt kèm mức ưu tiên khắc phục.',
   6, 11, 'LOW', 'HOAN_THANH', DATE '2026-06-08', DATE '2026-06-19', 2);

nv(49, 'Lựa chọn khung giao diện cho ứng dụng web',
   'So sánh các khung giao diện phổ biến theo tiêu chí đường cong học tập, hệ sinh thái thư viện, hiệu năng và khả năng tuyển được người biết dùng. Đề xuất lựa chọn kèm lý do.',
   2, 6, 'HIGH', 'HOAN_THANH', DATE '2026-04-27', DATE '2026-05-08', 2);

------------------------------------------------------------------------------
-- PHÒNG PHÁT TRIỂN (2) — nhóm Frontend, đang mở
------------------------------------------------------------------------------
nv(50, 'Xây dựng màn hình quản lý hồ sơ năng lực nhân sự',
   'Cho phép xem và sửa bằng cấp, chuyên ngành, kỹ năng kèm mức thành thạo của từng người. Quản lý sửa được người trong phạm vi mình phụ trách, nhân viên chỉ xem được hồ sơ của mình.',
   6, 10, 'MEDIUM', 'DANG_THUC_HIEN', DATE '2026-09-01', DATE '2026-09-19', 2);

nv(51, 'Kiểm thử đầu cuối luồng gợi ý người thực hiện',
   'Kiểm tra danh sách gợi ý hiển thị đúng thứ tự điểm, biểu đồ đóng góp khớp với số liệu API trả về, và cảnh báo hiện ra khi hệ thống không xác định được phòng ban phù hợp.',
   6, 11, 'MEDIUM', 'DANG_THUC_HIEN', DATE '2026-09-03', DATE '2026-09-23', 2);

nv(52, 'Kiểm thử khả năng chịu tải của giao diện danh sách',
   'Nạp thử năm nghìn dòng để xem trình duyệt có bị treo không. Đánh giá xem cần cuộn ảo hay chỉ cần phân trang phía máy chủ là đủ.',
   6, 11, 'MEDIUM', 'YEU_CAU_BO_SUNG', DATE '2026-09-06', DATE '2026-09-26', 2);

nv(53, 'Nâng cấp Angular lên phiên bản mới nhất',
   'Rà soát các thay đổi phá vỡ tương thích, cập nhật thư viện phụ thuộc và chạy lại toàn bộ kiểm thử. Ghi lại các chỗ phải sửa để làm tài liệu cho lần nâng cấp sau.',
   2, 6, 'MEDIUM', 'DA_GIAO', DATE '2026-09-04', DATE '2026-09-28', 2);

------------------------------------------------------------------------------
-- CHƯA GIAO — đầu vào để thử phần AI đoán phòng ban. DEPARTMENT_ID để rỗng.
------------------------------------------------------------------------------
nv(54, 'Xây dựng màn hình thống kê tiến độ theo phòng ban',
   'Dựng trang biểu đồ thể hiện số nhiệm vụ đang mở, đã hoàn thành và quá hạn của từng phòng ban. Có bộ lọc theo khoảng thời gian và xuất được ra tệp ảnh. Giao diện phải hiển thị tốt trên cả màn hình rộng lẫn màn hình hẹp.',
   2, NULL, 'HIGH', 'MOI_TAO', DATE '2026-09-10', DATE '2026-09-30', NULL);

nv(55, 'Xây dựng API quản lý danh mục kỹ năng bằng ASP.NET Core',
   'Viết các điểm cuối thêm, sửa, xoá và tra cứu danh mục kỹ năng. Dùng C# với Entity Framework Core trên Oracle. Có phân trang, tìm kiếm theo tên và kiểm tra trùng mã kỹ năng trước khi lưu.',
   5, NULL, 'HIGH', 'MOI_TAO', DATE '2026-09-10', DATE '2026-10-02', NULL);

------------------------------------------------------------------------------
-- PHÒNG NHÂN SỰ (3)
------------------------------------------------------------------------------
nv(56, 'Tổ chức đợt tuyển dụng lập trình viên quý II',
   'Đăng tin tuyển dụng, sàng lọc hồ sơ, sắp lịch phỏng vấn và tổng hợp kết quả. Mục tiêu tuyển đủ ba vị trí lập trình viên cho phòng Phát triển.',
   3, 12, 'HIGH', 'HOAN_THANH', DATE '2026-03-23', DATE '2026-04-10', 3);

nv(57, 'Sàng lọc hồ sơ ứng viên vị trí kỹ sư kiểm thử',
   'Đối chiếu hồ sơ với bản mô tả công việc, chấm điểm theo khung tiêu chí và lập danh sách ngắn để mời phỏng vấn.',
   3, 12, 'MEDIUM', 'HOAN_THANH', DATE '2026-04-13', DATE '2026-04-24', 3);

nv(58, 'Xây dựng chương trình đào tạo hội nhập cho nhân viên mới',
   'Soạn nội dung giới thiệu công ty, quy trình làm việc, công cụ nội bộ và quy định bảo mật. Thiết kế lộ trình hai tuần đầu kèm người kèm cặp cho từng nhân viên mới.',
   3, 13, 'MEDIUM', 'HOAN_THANH', DATE '2026-04-27', DATE '2026-05-15', 3);

nv(59, 'Tổ chức khoá huấn luyện an toàn thông tin',
   'Mời chuyên gia trình bày về quản lý mật khẩu, nhận biết thư lừa đảo và quy định xử lý dữ liệu nhạy cảm. Tổ chức bài kiểm tra cuối khoá và ghi nhận kết quả từng người.',
   3, 13, 'MEDIUM', 'HOAN_THANH', DATE '2026-05-18', DATE '2026-05-29', 3);

nv(60, 'Cập nhật quy trình thử việc và đánh giá sau thử việc',
   'Rà soát lại các mốc đánh giá, biểu mẫu nhận xét và tiêu chí quyết định ký hợp đồng chính thức. Lấy ý kiến các trưởng phòng trước khi ban hành.',
   3, 12, 'LOW', 'HOAN_THANH', DATE '2026-06-01', DATE '2026-06-12', 3);

nv(61, 'Đánh giá kết quả đào tạo nửa đầu năm',
   'Tổng hợp số khoá đã tổ chức, số lượt tham gia và kết quả kiểm tra. So sánh với kế hoạch đầu năm và đề xuất điều chỉnh cho nửa cuối năm.',
   3, 13, 'MEDIUM', 'HOAN_THANH', DATE '2026-06-15', DATE '2026-06-26', 3);

nv(62, 'Hoàn thiện hợp đồng lao động đợt tuyển tháng 6',
   'Soạn hợp đồng cho các ứng viên đã nhận việc, thu thập hồ sơ cá nhân, đăng ký bảo hiểm và mã số thuế. Kiểm tra tính đầy đủ trước khi trình ký.',
   3, 12, 'HIGH', 'HOAN_THANH', DATE '2026-06-22', DATE '2026-07-03', 3);

nv(63, 'Xây dựng khung năng lực cho khối kỹ thuật',
   'Định nghĩa các nhóm năng lực và mức độ từ một tới năm cho từng vị trí kỹ thuật. Mô tả biểu hiện cụ thể của từng mức để người đánh giá chấm nhất quán.',
   1, 3, 'MEDIUM', 'HOAN_THANH', DATE '2026-07-06', DATE '2026-07-24', 3);

nv(64, 'Tổ chức phỏng vấn vòng hai cho ứng viên Frontend',
   'Sắp lịch với trưởng nhóm Frontend, chuẩn bị bài tập thực hành và tổng hợp nhận xét của hội đồng. Phản hồi kết quả cho ứng viên trong vòng ba ngày.',
   3, 12, 'MEDIUM', 'HOAN_THANH', DATE '2026-07-27', DATE '2026-08-07', 3);

nv(65, 'Biên soạn tài liệu đào tạo nội bộ về quy trình làm việc',
   'Viết tài liệu mô tả quy trình giao việc, báo cáo tiến độ và duyệt kết quả. Kèm ví dụ minh hoạ và các lỗi thường gặp.',
   3, 13, 'MEDIUM', 'HOAN_THANH', DATE '2026-08-10', DATE '2026-08-21', 3);

nv(66, 'Thống kê nhu cầu đào tạo của các phòng ban',
   'Gửi phiếu khảo sát tới từng phòng, tổng hợp nhu cầu theo chủ đề và mức độ cấp thiết. Đề xuất danh mục khoá đào tạo cho quý sau.',
   3, 13, 'LOW', 'HOAN_THANH', DATE '2026-08-17', DATE '2026-08-28', 3);

nv(67, 'Tuyển dụng vị trí kỹ sư dữ liệu cho phòng Phát triển',
   'Xây dựng bản mô tả công việc, đăng tin trên các kênh tuyển dụng và sàng lọc ứng viên. Phối hợp với trưởng phòng Phát triển để thống nhất tiêu chí chuyên môn.',
   3, 12, 'HIGH', 'DANG_THUC_HIEN', DATE '2026-09-01', DATE '2026-09-25', 3);

nv(68, 'Khảo sát mức độ hài lòng của nhân viên',
   'Thiết kế phiếu khảo sát ẩn danh về môi trường làm việc, cơ hội phát triển và chế độ đãi ngộ. Tổng hợp kết quả theo phòng ban và đề xuất cải thiện.',
   3, 13, 'MEDIUM', 'CHO_XAC_NHAN', DATE '2026-09-03', DATE '2026-09-22', 3);

nv(69, 'Cập nhật sổ tay nhân viên năm 2026',
   'Rà soát và cập nhật các quy định về giờ làm việc, nghỉ phép, chế độ phúc lợi và quy tắc ứng xử. Ban hành bản mới tới toàn thể nhân viên.',
   3, 12, 'LOW', 'DA_GIAO', DATE '2026-09-07', DATE '2026-09-30', 3);

------------------------------------------------------------------------------
-- PHÒNG HÀNH CHÍNH - KẾ TOÁN (4)
------------------------------------------------------------------------------
nv(70, 'Quyết toán thuế thu nhập doanh nghiệp quý I',
   'Tập hợp chứng từ, đối chiếu doanh thu và chi phí, lập tờ khai quyết toán và nộp đúng hạn quy định. Lưu hồ sơ đầy đủ phục vụ thanh tra sau này.',
   4, 14, 'HIGH', 'HOAN_THANH', DATE '2026-03-30', DATE '2026-04-17', 4);

nv(71, 'Lập bảng lương và bảo hiểm tháng 4',
   'Tính lương theo bảng chấm công, các khoản phụ cấp, khấu trừ thuế thu nhập cá nhân và đóng bảo hiểm. Đối chiếu với phòng Nhân sự trước khi chuyển khoản.',
   4, 14, 'HIGH', 'HOAN_THANH', DATE '2026-04-27', DATE '2026-05-08', 4);

nv(72, 'Đối chiếu công nợ với nhà cung cấp',
   'Rà soát các khoản phải trả, đối chiếu số dư với từng nhà cung cấp và lập biên bản xác nhận. Xử lý các khoản chênh lệch nếu có.',
   4, 14, 'MEDIUM', 'HOAN_THANH', DATE '2026-05-18', DATE '2026-05-29', 4);

nv(73, 'Số hoá hồ sơ lưu trữ năm 2025',
   'Quét toàn bộ công văn đi và đến của năm 2025, đặt tên tệp theo quy ước và lập danh mục tra cứu. Kiểm tra chất lượng bản quét trước khi lưu.',
   4, 15, 'MEDIUM', 'HOAN_THANH', DATE '2026-05-25', DATE '2026-06-12', 4);

nv(74, 'Quản lý cấp phát văn phòng phẩm quý II',
   'Tổng hợp nhu cầu của các phòng, lập đơn mua, nhận hàng và cấp phát theo phiếu. Theo dõi tồn kho và báo cáo chi phí cuối quý.',
   4, 15, 'LOW', 'HOAN_THANH', DATE '2026-06-15', DATE '2026-06-26', 4);

nv(75, 'Lập báo cáo tài chính giữa niên độ',
   'Lập bảng cân đối kế toán, báo cáo kết quả kinh doanh và báo cáo lưu chuyển tiền tệ cho sáu tháng đầu năm. Thuyết minh các khoản mục biến động lớn.',
   4, 14, 'MEDIUM', 'HOAN_THANH', DATE '2026-06-29', DATE '2026-07-17', 4);

nv(76, 'Tổ chức hội nghị khách hàng thường niên',
   'Đặt địa điểm, gửi thư mời, chuẩn bị tài liệu và quà tặng, điều phối chương trình trong ngày diễn ra. Tổng hợp chi phí và phản hồi của khách mời sau sự kiện.',
   4, 15, 'MEDIUM', 'HOAN_THANH', DATE '2026-07-20', DATE '2026-07-31', 4);

nv(77, 'Kiểm kê tài sản cố định toàn công ty',
   'Đối chiếu sổ sách với thực tế tại từng phòng ban, dán lại nhãn tài sản và lập biên bản kiểm kê. Đề xuất thanh lý các tài sản đã hết khấu hao và không còn dùng.',
   4, 14, 'MEDIUM', 'HOAN_THANH', DATE '2026-08-03', DATE '2026-08-14', 4);

nv(78, 'Chuẩn bị tiệc tất niên cho toàn công ty',
   'Khảo sát và đặt nhà hàng, lên thực đơn, chuẩn bị chương trình văn nghệ và bốc thăm trúng thưởng. Tổng hợp danh sách tham dự và dự trù kinh phí.',
   4, 15, 'LOW', 'HOAN_THANH', DATE '2026-08-10', DATE '2026-08-21', 4);

nv(79, 'Lập bảng lương và bảo hiểm tháng 8',
   'Tính lương theo bảng chấm công tháng 8, xử lý các trường hợp nghỉ không lương và làm thêm giờ. Đối chiếu với phòng Nhân sự trước khi chuyển khoản.',
   4, 14, 'HIGH', 'HOAN_THANH', DATE '2026-08-17', DATE '2026-08-28', 4);

nv(80, 'Sắp xếp và lập danh mục kho lưu trữ công văn',
   'Phân loại hồ sơ theo năm và theo loại văn bản, đánh số hộp lưu trữ và lập bảng tra cứu. Loại bỏ các tài liệu đã hết thời hạn lưu theo quy định.',
   4, 15, 'MEDIUM', 'HOAN_THANH', DATE '2026-08-24', DATE '2026-09-04', 4);

nv(81, 'Lập bảng lương và bảo hiểm tháng 9',
   'Tính lương theo bảng chấm công tháng 9, cập nhật mức đóng bảo hiểm mới và đối chiếu với phòng Nhân sự trước khi chuyển khoản.',
   4, 14, 'HIGH', 'DANG_THUC_HIEN', DATE '2026-09-01', DATE '2026-09-20', 4);

nv(82, 'Đối chiếu chi phí công tác tháng 8',
   'Rà soát chứng từ công tác phí, đối chiếu với quy định định mức và lập bảng tổng hợp trình duyệt. Trả lại các chứng từ không hợp lệ để bổ sung.',
   4, 14, 'MEDIUM', 'CHO_XAC_NHAN', DATE '2026-09-04', DATE '2026-09-23', 4);

nv(83, 'Rà soát hợp đồng thuê văn phòng sắp hết hạn',
   'Kiểm tra thời hạn, điều khoản gia hạn và mức giá thuê hiện tại. Khảo sát giá thị trường để có căn cứ đàm phán. Đề xuất phương án gia hạn hoặc chuyển địa điểm.',
   4, 15, 'LOW', 'YEU_CAU_BO_SUNG', DATE '2026-09-06', DATE '2026-09-26', 4);

------------------------------------------------------------------------------
-- GIÁM ĐỐC GIAO XUỐNG CHO TRƯỞNG PHÒNG
--
-- Người giao là Giám đốc (1) nhưng DEPARTMENT_ID mang mã phòng của người nhận,
-- vì cột đó chỉ phòng thực thi. Xem ghi chú ở đầu file.
--
-- LƯU Ý CHO PHẦN CODE: các nhiệm vụ này giao cho DEPT_HEAD, trong khi
-- VaiTro.CoTheNhanViec hiện chỉ cho TEAM_LEAD và EMPLOYEE nhận việc. Quy tắc
-- đúng phải là "ai ở dưới người giao thì nhận được việc". Cần thống nhất lại
-- trước khi viết phần lọc ứng viên.
------------------------------------------------------------------------------
nv(84, 'Lập kế hoạch phát triển sản phẩm năm 2026',
   'Xác định các sản phẩm trọng tâm, mốc phát hành theo quý và nguồn lực cần thiết cho từng giai đoạn. Đánh giá rủi ro kỹ thuật và phương án dự phòng.',
   1, 2, 'HIGH', 'HOAN_THANH', DATE '2026-03-16', DATE '2026-04-10', 2);

nv(85, 'Xây dựng chiến lược nhân sự năm 2026',
   'Dự báo nhu cầu nhân lực theo kế hoạch kinh doanh, xác định các vị trí cần tuyển và ngân sách nhân sự. Đề xuất chính sách đào tạo và phát triển đội ngũ.',
   1, 3, 'HIGH', 'HOAN_THANH', DATE '2026-03-23', DATE '2026-04-17', 3);

nv(86, 'Rà soát ngân sách vận hành nửa cuối năm',
   'Đối chiếu chi phí thực tế nửa đầu năm với dự toán, phân tích các khoản vượt và đề xuất điều chỉnh ngân sách cho sáu tháng còn lại.',
   1, 4, 'MEDIUM', 'HOAN_THANH', DATE '2026-06-01', DATE '2026-06-26', 4);

nv(87, 'Triển khai phân hệ giao nhiệm vụ tích hợp AI',
   'Chỉ đạo triển khai phân hệ giao việc có tích hợp gợi ý người thực hiện bằng trí tuệ nhân tạo. Theo dõi tiến độ, phê duyệt các quyết định kiến trúc và nghiệm thu kết quả từng giai đoạn.',
   1, 2, 'HIGH', 'DANG_THUC_HIEN', DATE '2026-08-03', DATE '2026-10-16', 2);

nv(88, 'Đề xuất chính sách giữ chân nhân sự chủ chốt',
   'Xác định các vị trí then chốt, đánh giá rủi ro nghỉ việc và đề xuất gói chính sách gồm lộ trình thăng tiến, đãi ngộ và cơ hội đào tạo.',
   1, 3, 'MEDIUM', 'DA_GIAO', DATE '2026-09-07', DATE '2026-09-30', 3);

nv(89, 'Đánh giá hiệu quả đầu tư công nghệ năm 2026',
   'Tổng hợp toàn bộ chi phí đầu tư cho hạ tầng, phần mềm và nhân lực công nghệ trong năm. Đối chiếu với lợi ích thu được và đề xuất định hướng đầu tư cho năm sau.',
   1, NULL, 'HIGH', 'MOI_TAO', DATE '2026-09-10', DATE '2026-10-10', NULL);

nv(90, 'Tối ưu truy vấn thống kê tổng hợp trên Oracle',
   'Báo cáo tổng hợp cuối tháng chạy mất hơn ba phút do phải quét nhiều bảng lớn. Phân tích kế hoạch thực thi, cân nhắc dùng khung nhìn cụ thể hoá hoặc bảng tổng hợp dựng sẵn, và thiết kế lại chỉ mục cho phù hợp.',
   2, NULL, 'HIGH', 'MOI_TAO', DATE '2026-09-10', DATE '2026-10-05', NULL);

END;
/
COMMIT;

--==============================================================================
-- BƯỚC 3: Sinh tiến độ và báo cáo
--
-- Ngày duyệt tính từ hạn hoàn thành:
--   đúng hạn -> trước hạn 1 ngày;  trễ hạn -> sau hạn 4 ngày.
-- Danh sách v_tre và v_lam_lai là nơi duy nhất quyết định ai có hồ sơ tốt hay
-- xấu. Muốn đổi phân bố hiệu suất thì sửa hai danh sách này, không sửa chỗ khác.
--==============================================================================
DECLARE
  TYPE t_ids IS TABLE OF NUMBER;

  -- Nhiệm vụ hoàn thành muộn hơn hạn
  v_tre     t_ids := t_ids(5, 7, 9, 15, 36, 39, 43, 59, 61, 66, 74, 78);

  -- Nhiệm vụ từng bị trả về một lần trước khi được duyệt
  v_lam_lai t_ids := t_ids(10, 18, 41, 61, 73);

  v_ngay_xong DATE;
  v_id_bc     NUMBER;

  FUNCTION co_trong(p_id NUMBER, p_ds t_ids) RETURN BOOLEAN IS
  BEGIN
    FOR i IN 1 .. p_ds.COUNT LOOP
      IF p_ds(i) = p_id THEN RETURN TRUE; END IF;
    END LOOP;
    RETURN FALSE;
  END;

  PROCEDURE td(p_task NUMBER, p_user NUMBER, p_pct NUMBER,
               p_noi_dung VARCHAR2, p_luc DATE) IS
  BEGIN
    INSERT INTO TASK_PROGRESS (TASK_ID, USER_ID, PROGRESS_PERCENT, CONTENT, CREATED_AT)
    VALUES (p_task, p_user, p_pct, p_noi_dung, CAST(p_luc AS TIMESTAMP));
  END;

BEGIN
  FOR t IN (SELECT ID, CREATOR_ID, ASSIGNEE_ID, STATUS_CODE, START_DATE, DUE_DATE
            FROM   TASKS
            WHERE  ASSIGNEE_ID IS NOT NULL
            ORDER  BY ID) LOOP

    ----------------------------------------------------------------------------
    -- Nhiệm vụ đã khép lại: đủ ba mốc tiến độ
    ----------------------------------------------------------------------------
    IF t.STATUS_CODE IN ('HOAN_THANH', 'CHO_XAC_NHAN', 'YEU_CAU_BO_SUNG') THEN

      IF co_trong(t.ID, v_tre) THEN
        v_ngay_xong := t.DUE_DATE + 4;
      ELSE
        v_ngay_xong := t.DUE_DATE - 1;
      END IF;

      td(t.ID, t.ASSIGNEE_ID, 30,
         'Đã nắm yêu cầu và bắt tay vào phần đầu tiên.',
         t.START_DATE + 2);
      td(t.ID, t.ASSIGNEE_ID, 70,
         'Hoàn thành phần chính, còn lại khâu rà soát và hoàn thiện.',
         t.START_DATE + TRUNC((t.DUE_DATE - t.START_DATE) * 0.6));
      td(t.ID, t.ASSIGNEE_ID, 100,
         'Đã xong toàn bộ nội dung, chuyển sang bước báo cáo kết quả.',
         v_ngay_xong - 1);

      --------------------------------------------------------------------------
      -- Báo cáo
      --------------------------------------------------------------------------
      IF t.STATUS_CODE = 'HOAN_THANH' THEN

        -- Nhiệm vụ từng bị trả về: một báo cáo bị từ chối trước
        IF co_trong(t.ID, v_lam_lai) THEN
          INSERT INTO TASK_REPORTS (TASK_ID, REPORTER_ID, CONTENT, REPORT_STATUS,
                                    REVIEWER_ID, REVIEW_NOTE, CREATED_AT, REVIEWED_AT)
          VALUES (t.ID, t.ASSIGNEE_ID,
                  'Báo cáo kết quả lần đầu, kèm mô tả các phần đã thực hiện.',
                  'TU_CHOI', t.CREATOR_ID,
                  'Kết quả chưa đạt yêu cầu: còn thiếu phần kiểm chứng và chưa xử lý hết các trường hợp biên. Đề nghị bổ sung rồi nộp lại.',
                  CAST(v_ngay_xong - 6 AS TIMESTAMP),
                  CAST(v_ngay_xong - 5 AS TIMESTAMP));
        END IF;

        INSERT INTO TASK_REPORTS (TASK_ID, REPORTER_ID, CONTENT, REPORT_STATUS,
                                  REVIEWER_ID, REVIEW_NOTE, CREATED_AT, REVIEWED_AT)
        VALUES (t.ID, t.ASSIGNEE_ID,
                'Đã hoàn thành toàn bộ nội dung được giao. Kết quả đã tự kiểm tra và sẵn sàng bàn giao.',
                'DA_XAC_NHAN', t.CREATOR_ID,
                'Kết quả đạt yêu cầu. Đồng ý nghiệm thu.',
                CAST(v_ngay_xong AS TIMESTAMP),
                CAST(v_ngay_xong AS TIMESTAMP));

      ELSIF t.STATUS_CODE = 'CHO_XAC_NHAN' THEN

        INSERT INTO TASK_REPORTS (TASK_ID, REPORTER_ID, CONTENT, REPORT_STATUS,
                                  REVIEWER_ID, REVIEW_NOTE, CREATED_AT, REVIEWED_AT)
        VALUES (t.ID, t.ASSIGNEE_ID,
                'Đã hoàn thành nội dung được giao, kính trình quản lý xem xét nghiệm thu.',
                'CHO_XAC_NHAN', NULL, NULL,
                CAST(v_ngay_xong AS TIMESTAMP), NULL);

      ELSE -- YEU_CAU_BO_SUNG

        INSERT INTO TASK_REPORTS (TASK_ID, REPORTER_ID, CONTENT, REPORT_STATUS,
                                  REVIEWER_ID, REVIEW_NOTE, CREATED_AT, REVIEWED_AT)
        VALUES (t.ID, t.ASSIGNEE_ID,
                'Báo cáo kết quả thực hiện kèm các nội dung đã hoàn thành.',
                'TU_CHOI', t.CREATOR_ID,
                'Cần bổ sung thêm: phần kết quả chưa có số liệu kiểm chứng và chưa nêu rõ các hạn chế còn tồn tại.',
                CAST(v_ngay_xong AS TIMESTAMP),
                CAST(v_ngay_xong + 1 AS TIMESTAMP));

      END IF;

    ----------------------------------------------------------------------------
    -- Nhiệm vụ đang làm dở: chỉ có mốc tiến độ, chưa có báo cáo
    ----------------------------------------------------------------------------
    ELSIF t.STATUS_CODE = 'DANG_THUC_HIEN' THEN

      td(t.ID, t.ASSIGNEE_ID, 30,
         'Đã nắm yêu cầu và bắt tay vào phần đầu tiên.',
         t.START_DATE + 2);
      td(t.ID, t.ASSIGNEE_ID, 60,
         'Đang thực hiện phần chính, tiến độ bám sát kế hoạch.',
         t.START_DATE + 6);

    END IF;
    -- DA_GIAO và MOI_TAO: chưa có gì

  END LOOP;
END;
/
COMMIT;

--==============================================================================
-- BƯỚC 4: Một ít tệp đính kèm cho các nhiệm vụ đã nghiệm thu
--==============================================================================
INSERT INTO TASK_ATTACHMENTS (TASK_ID, REPORT_ID, FILE_NAME, FILE_PATH, FILE_TYPE, FILE_SIZE, UPLOADED_BY, UPLOADED_AT)
SELECT r.TASK_ID, r.ID,
       'bao-cao-nhiem-vu-' || r.TASK_ID || '.pdf',
       '/uploads/2026/bao-cao-nhiem-vu-' || r.TASK_ID || '.pdf',
       'application/pdf',
       120000 + MOD(r.TASK_ID * 7919, 480000),
       r.REPORTER_ID,
       r.CREATED_AT
FROM   TASK_REPORTS r
WHERE  r.REPORT_STATUS = 'DA_XAC_NHAN'
  AND  MOD(r.TASK_ID, 3) = 0;
COMMIT;

--==============================================================================
-- BƯỚC 5: Đặt lại bộ đếm IDENTITY
--
-- Script chèn ID tường minh cho TASKS nhưng bộ đếm không tự nhảy theo. Không đặt
-- lại thì INSERT đầu tiên từ ứng dụng sẽ xin ID = 1 và dính ORA-00001.
-- Ba bảng còn lại để IDENTITY tự sinh nên vẫn đặt lại cho chắc.
--==============================================================================
ALTER TABLE TASKS            MODIFY (ID GENERATED BY DEFAULT AS IDENTITY (START WITH LIMIT VALUE));
ALTER TABLE TASK_PROGRESS    MODIFY (ID GENERATED BY DEFAULT AS IDENTITY (START WITH LIMIT VALUE));
ALTER TABLE TASK_REPORTS     MODIFY (ID GENERATED BY DEFAULT AS IDENTITY (START WITH LIMIT VALUE));
ALTER TABLE TASK_ATTACHMENTS MODIFY (ID GENERATED BY DEFAULT AS IDENTITY (START WITH LIMIT VALUE));
COMMIT;

--==============================================================================
-- KIỂM CHỨNG
--==============================================================================
SET PAGESIZE 200 LINESIZE 200
SET FEEDBACK OFF

PROMPT
PROMPT === Số dòng từng bảng ===
SELECT 'TASKS' AS BANG, COUNT(*) AS SO_DONG FROM TASKS
UNION ALL SELECT 'TASK_PROGRESS',    COUNT(*) FROM TASK_PROGRESS
UNION ALL SELECT 'TASK_REPORTS',     COUNT(*) FROM TASK_REPORTS
UNION ALL SELECT 'TASK_ATTACHMENTS', COUNT(*) FROM TASK_ATTACHMENTS;

PROMPT
PROMPT === Nhiệm vụ theo phòng ban và trạng thái ===
SELECT NVL(d.CODE, '(chua ro)') AS PHONG,
       t.STATUS_CODE, COUNT(*) AS SO_LUONG
FROM   TASKS t LEFT JOIN DEPARTMENTS d ON d.ID = t.DEPARTMENT_ID
GROUP  BY NVL(d.CODE, '(chua ro)'), t.STATUS_CODE
ORDER  BY 1, 2;

PROMPT
PROMPT === Hiệu suất từng người: tỷ lệ đúng hạn và số việc đang gánh ===
SELECT u.ID,
       u.FULL_NAME,
       xong.SO_XONG                                   AS DA_XONG,
       xong.SO_DUNG_HAN                               AS DUNG_HAN,
       ROUND(xong.SO_DUNG_HAN / xong.SO_XONG * 100)   AS TY_LE,
       NVL(mo.SO_MO, 0)                               AS DANG_GANH,
       NVL(mo.TRONG_SO, 0)                            AS TAI_CO_TRONG_SO
FROM   USERS u
JOIN  (SELECT t.ASSIGNEE_ID,
              COUNT(*) AS SO_XONG,
              SUM(CASE WHEN r.REVIEWED_AT <= CAST(t.DUE_DATE + 1 AS TIMESTAMP)
                       THEN 1 ELSE 0 END) AS SO_DUNG_HAN
       FROM   TASKS t
       JOIN   TASK_REPORTS r ON r.TASK_ID = t.ID AND r.REPORT_STATUS = 'DA_XAC_NHAN'
       WHERE  t.STATUS_CODE = 'HOAN_THANH'
       GROUP  BY t.ASSIGNEE_ID) xong ON xong.ASSIGNEE_ID = u.ID
LEFT  JOIN (SELECT ASSIGNEE_ID,
                   COUNT(*) AS SO_MO,
                   SUM(CASE PRIORITY WHEN 'HIGH' THEN 3 WHEN 'MEDIUM' THEN 2 ELSE 1 END) AS TRONG_SO
            FROM   TASKS
            WHERE  STATUS_CODE IN ('DA_GIAO','DANG_THUC_HIEN','CHO_XAC_NHAN','YEU_CAU_BO_SUNG')
            GROUP  BY ASSIGNEE_ID) mo ON mo.ASSIGNEE_ID = u.ID
ORDER  BY 5 DESC, 6;

PROMPT
PROMPT === Bốn nhiệm vụ chưa giao, dùng để thử phần AI đoán phòng ban ===
SELECT ID, SUBSTR(TITLE, 1, 62) AS TIEU_DE, PRIORITY, DEPARTMENT_ID
FROM   TASKS WHERE STATUS_CODE = 'MOI_TAO' ORDER BY ID;

PROMPT
PROMPT === Kiểm tra tiếng Việt không bị hỏng mã (LENGTHB phai lon hon LENGTH) ===
SELECT ID, LENGTH(TITLE) AS SO_KY_TU, LENGTHB(TITLE) AS SO_BYTE,
       SUBSTR(TITLE, 1, 46) AS TIEU_DE
FROM   TASKS WHERE ID IN (1, 55, 78, 90) ORDER BY ID;
