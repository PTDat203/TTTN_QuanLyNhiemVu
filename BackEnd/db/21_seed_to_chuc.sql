--------------------------------------------------------------------------------
-- 21_seed_to_chuc.sql
-- Dựng lại dữ liệu mẫu theo cơ cấu tổ chức bốn cấp.
-- Chạy bằng tài khoản TASK_APP, SAU KHI đã chạy 20_them_co_cau_to_chuc.sql.
--
-- CƠ CẤU
--   Giám đốc
--     ├── Trưởng phòng Phát triển phần mềm
--     │     ├── Trưởng nhóm Backend  ── 3 nhân viên
--     │     └── Trưởng nhóm Frontend ── 2 nhân viên
--     ├── Trưởng phòng Nhân sự       ── 2 nhân viên
--     └── Trưởng phòng Hành chính    ── 2 nhân viên
--
-- Tổng 15 người. Phòng Phát triển đông nhất (8 người) để phần AI có đủ ứng viên
-- mà so sánh, và để thấy được cơ chế "người giỏi nhưng quá tải thì đẩy xuống người kế".
--
-- MẬT KHẨU DEMO của mọi tài khoản: 123456
--   Hash BCrypt $2a$11$ dưới đây đã kiểm chứng bằng BCrypt.Net-Next 4.0.3.
--
-- BẰNG CẤP CHỈ LÀ TÍN HIỆU HỖ TRỢ, KHÔNG PHẢI ĐIỀU KIỆN LỌC.
--   Điều kiện lọc ứng viên là PHÒNG BAN HIỆN TẠI của người đó.
--   Bằng cấp, kỹ năng, kinh nghiệm, lịch sử, hiệu suất và khối lượng chỉ dùng để
--   XẾP HẠNG bên trong phòng.
--
--   Phần lớn người phòng Phát triển có bằng CNTT, nhưng dữ liệu cố tình có một ca
--   ngược: Phạm Minh Cường (ID 7) học Kinh tế, vẫn đang là lập trình viên Backend
--   với kỹ năng C#/ASP.NET Core mức 4. Nếu lấy bằng cấp làm điều kiện cứng thì người
--   này bị loại oan — đó chính là lý do không được làm vậy.
--
-- CHẠY LẠI ĐƯỢC: xoá sạch dữ liệu nghiệp vụ rồi nạp lại với ID cố định.
-- CẢNH BÁO: xoá toàn bộ dữ liệu 8 bảng. Chỉ chạy trên máy phát triển.
--------------------------------------------------------------------------------

SET DEFINE OFF

--==============================================================================
-- BƯỚC 1: Xoá dữ liệu cũ theo đúng thứ tự phụ thuộc khoá ngoại
--==============================================================================
DELETE FROM TASK_ATTACHMENTS;
DELETE FROM TASK_REPORTS;
DELETE FROM TASK_PROGRESS;
DELETE FROM TASKS;
DELETE FROM USER_QUALIFICATIONS;
DELETE FROM USER_SKILLS;
-- Gỡ liên kết tự tham chiếu trước khi xoá, nếu không sẽ vướng FK_USERS_MANAGER
UPDATE USERS SET MANAGER_ID = NULL;
DELETE FROM USERS;
DELETE FROM DEPARTMENTS;
COMMIT;

--==============================================================================
-- BƯỚC 2: DEPARTMENTS — bốn phòng ban
--==============================================================================
INSERT INTO DEPARTMENTS (ID, CODE, NAME, DESCRIPTION) VALUES
  (1, 'BAN_GIAM_DOC', 'Ban Giám đốc', 'Điều hành chung toàn công ty');
INSERT INTO DEPARTMENTS (ID, CODE, NAME, DESCRIPTION) VALUES
  (2, 'PHAT_TRIEN', 'Phòng Phát triển phần mềm',
   'Phân tích, lập trình, kiểm thử và vận hành các sản phẩm phần mềm');
INSERT INTO DEPARTMENTS (ID, CODE, NAME, DESCRIPTION) VALUES
  (3, 'NHAN_SU', 'Phòng Nhân sự',
   'Tuyển dụng, đào tạo, chế độ chính sách và quản lý hồ sơ nhân sự');
INSERT INTO DEPARTMENTS (ID, CODE, NAME, DESCRIPTION) VALUES
  (4, 'HANH_CHINH', 'Phòng Hành chính - Kế toán',
   'Văn thư, tài sản, thu chi và báo cáo tài chính');
COMMIT;

--==============================================================================
-- BƯỚC 3: USERS — 15 người, bốn cấp
--
-- Chèn MANAGER_ID = NULL trước rồi cập nhật sau, để không phụ thuộc thứ tự chèn.
--==============================================================================

-- Cấp 1: Giám đốc -------------------------------------------------------------
INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS, DEPARTMENT_ID, JOB_TITLE)
VALUES (1, 'giamdoc', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi',
        'Trần Quốc Hưng', 'hung.tq@congty.vn', 'DIRECTOR', 'ACTIVE', 1, 'Giám đốc');

-- Cấp 2: Trưởng phòng ---------------------------------------------------------
INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS, DEPARTMENT_ID, JOB_TITLE)
VALUES (2, 'tp.phattrien', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi',
        'Phạm Thị Bích Ngọc', 'ngoc.ptb@congty.vn', 'DEPT_HEAD', 'ACTIVE', 2, 'Trưởng phòng Phát triển phần mềm');

INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS, DEPARTMENT_ID, JOB_TITLE)
VALUES (3, 'tp.nhansu', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi',
        'Nguyễn Thu Trang', 'trang.nt@congty.vn', 'DEPT_HEAD', 'ACTIVE', 3, 'Trưởng phòng Nhân sự');

INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS, DEPARTMENT_ID, JOB_TITLE)
VALUES (4, 'tp.hanhchinh', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi',
        'Lê Văn Thắng', 'thang.lv@congty.vn', 'DEPT_HEAD', 'ACTIVE', 4, 'Trưởng phòng Hành chính - Kế toán');

-- Cấp 3: Trưởng nhóm (chỉ phòng Phát triển) ------------------------------------
INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS, DEPARTMENT_ID, JOB_TITLE)
VALUES (5, 'leader.be', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi',
        'Nguyễn Văn An', 'an.nv@congty.vn', 'TEAM_LEAD', 'ACTIVE', 2, 'Trưởng nhóm Backend');

INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS, DEPARTMENT_ID, JOB_TITLE)
VALUES (6, 'leader.fe', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi',
        'Lê Thị Bình', 'binh.lt@congty.vn', 'TEAM_LEAD', 'ACTIVE', 2, 'Trưởng nhóm Frontend');

-- Cấp 4: Nhân viên phòng Phát triển -------------------------------------------
INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS, DEPARTMENT_ID, JOB_TITLE)
VALUES (7, 'nv.cuong', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi',
        'Phạm Minh Cường', 'cuong.pm@congty.vn', 'EMPLOYEE', 'ACTIVE', 2, 'Lập trình viên Backend');

INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS, DEPARTMENT_ID, JOB_TITLE)
VALUES (8, 'nv.dung', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi',
        'Trần Thị Dung', 'dung.tt@congty.vn', 'EMPLOYEE', 'ACTIVE', 2, 'Lập trình viên Backend');

INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS, DEPARTMENT_ID, JOB_TITLE)
VALUES (9, 'nv.giang', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi',
        'Hoàng Văn Giang', 'giang.hv@congty.vn', 'EMPLOYEE', 'ACTIVE', 2, 'Kỹ sư cơ sở dữ liệu');

INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS, DEPARTMENT_ID, JOB_TITLE)
VALUES (10, 'nv.hoa', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi',
        'Đỗ Thị Hòa', 'hoa.dt@congty.vn', 'EMPLOYEE', 'ACTIVE', 2, 'Lập trình viên Frontend');

INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS, DEPARTMENT_ID, JOB_TITLE)
VALUES (11, 'nv.khanh', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi',
        'Vũ Đình Khánh', 'khanh.vd@congty.vn', 'EMPLOYEE', 'ACTIVE', 2, 'Kỹ sư kiểm thử');

-- Cấp 4: Nhân viên phòng Nhân sự ----------------------------------------------
INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS, DEPARTMENT_ID, JOB_TITLE)
VALUES (12, 'nv.lan', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi',
        'Ngô Thị Lan', 'lan.nt@congty.vn', 'EMPLOYEE', 'ACTIVE', 3, 'Chuyên viên tuyển dụng');

INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS, DEPARTMENT_ID, JOB_TITLE)
VALUES (13, 'nv.minh', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi',
        'Bùi Nhật Minh', 'minh.bn@congty.vn', 'EMPLOYEE', 'ACTIVE', 3, 'Chuyên viên đào tạo');

-- Cấp 4: Nhân viên phòng Hành chính --------------------------------------------
INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS, DEPARTMENT_ID, JOB_TITLE)
VALUES (14, 'nv.nga', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi',
        'Đinh Thị Nga', 'nga.dt@congty.vn', 'EMPLOYEE', 'ACTIVE', 4, 'Kế toán viên');

INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS, DEPARTMENT_ID, JOB_TITLE)
VALUES (15, 'nv.phuc', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi',
        'Trịnh Hồng Phúc', 'phuc.th@congty.vn', 'EMPLOYEE', 'ACTIVE', 4, 'Nhân viên văn thư');

COMMIT;

--==============================================================================
-- BƯỚC 4: Đường báo cáo — dựng cây tổ chức
--==============================================================================
-- Trưởng phòng  <- Giám đốc
UPDATE USERS SET MANAGER_ID = 1 WHERE ID IN (2, 3, 4);
-- Trưởng nhóm   <- Trưởng phòng Phát triển
UPDATE USERS SET MANAGER_ID = 2 WHERE ID IN (5, 6);
-- Nhóm Backend  <- Trưởng nhóm Backend
UPDATE USERS SET MANAGER_ID = 5 WHERE ID IN (7, 8, 9);
-- Nhóm Frontend <- Trưởng nhóm Frontend
UPDATE USERS SET MANAGER_ID = 6 WHERE ID IN (10, 11);
-- Nhân viên Nhân sự    <- Trưởng phòng Nhân sự
UPDATE USERS SET MANAGER_ID = 3 WHERE ID IN (12, 13);
-- Nhân viên Hành chính <- Trưởng phòng Hành chính
UPDATE USERS SET MANAGER_ID = 4 WHERE ID IN (14, 15);
COMMIT;

--==============================================================================
-- BƯỚC 5: USER_QUALIFICATIONS — bằng cấp khớp phòng ban
--
-- Đây là dữ liệu chứng minh nguyên tắc "học CNTT thì vào phòng Phát triển,
-- học Kế toán thì vào phòng Hành chính".
--==============================================================================

-- Phòng Phát triển: bằng CNTT ---------------------------------------------------
INSERT INTO USER_QUALIFICATIONS (USER_ID, DEGREE_NAME, MAJOR, DEGREE_LEVEL, SCHOOL, GRAD_YEAR)
VALUES (2, 'Thạc sĩ Khoa học máy tính', 'Công nghệ thông tin', 'THAC_SI', 'Đại học Bách khoa Hà Nội', 2016);
INSERT INTO USER_QUALIFICATIONS (USER_ID, DEGREE_NAME, MAJOR, DEGREE_LEVEL, SCHOOL, GRAD_YEAR)
VALUES (5, 'Kỹ sư Công nghệ thông tin', 'Kỹ thuật phần mềm', 'DAI_HOC', 'Đại học Xây dựng Hà Nội', 2018);
INSERT INTO USER_QUALIFICATIONS (USER_ID, DEGREE_NAME, MAJOR, DEGREE_LEVEL, SCHOOL, GRAD_YEAR)
VALUES (6, 'Kỹ sư Công nghệ thông tin', 'Công nghệ phần mềm', 'DAI_HOC', 'Đại học Công nghệ - ĐHQGHN', 2019);
-- CA NGƯỢC CÓ CHỦ ĐÍCH: Phạm Minh Cường học Kinh tế nhưng đang làm Backend
-- ở phòng Phát triển, kỹ năng C#/ASP.NET Core mức 4 và có lịch sử hoàn thành tốt.
-- Dùng để chứng minh: bằng cấp chỉ là TÍN HIỆU HỖ TRỢ. Nếu để bằng cấp làm điều kiện
-- cứng thì người này bị loại oan, trong khi thực tế anh ta thừa sức làm việc backend.
INSERT INTO USER_QUALIFICATIONS (USER_ID, DEGREE_NAME, MAJOR, DEGREE_LEVEL, SCHOOL, GRAD_YEAR)
VALUES (7, 'Cử nhân Kinh tế', 'Kinh tế đầu tư', 'DAI_HOC', 'Đại học Kinh tế Quốc dân', 2019);
INSERT INTO USER_QUALIFICATIONS (USER_ID, DEGREE_NAME, MAJOR, DEGREE_LEVEL, SCHOOL, GRAD_YEAR)
VALUES (7, 'Chứng chỉ Lập trình viên quốc tế', 'Công nghệ thông tin', 'CHUNG_CHI', 'Aptech', 2020);
INSERT INTO USER_QUALIFICATIONS (USER_ID, DEGREE_NAME, MAJOR, DEGREE_LEVEL, SCHOOL, GRAD_YEAR)
VALUES (8, 'Cử nhân Công nghệ thông tin', 'Khoa học máy tính', 'DAI_HOC', 'Đại học Bách khoa Hà Nội', 2022);
INSERT INTO USER_QUALIFICATIONS (USER_ID, DEGREE_NAME, MAJOR, DEGREE_LEVEL, SCHOOL, GRAD_YEAR)
VALUES (9, 'Kỹ sư Công nghệ thông tin', 'Hệ thống thông tin', 'DAI_HOC', 'Học viện Kỹ thuật Mật mã', 2020);
INSERT INTO USER_QUALIFICATIONS (USER_ID, DEGREE_NAME, MAJOR, DEGREE_LEVEL, SCHOOL, GRAD_YEAR)
VALUES (10, 'Cử nhân Công nghệ thông tin', 'Công nghệ phần mềm', 'DAI_HOC', 'Đại học FPT', 2022);
INSERT INTO USER_QUALIFICATIONS (USER_ID, DEGREE_NAME, MAJOR, DEGREE_LEVEL, SCHOOL, GRAD_YEAR)
VALUES (11, 'Kỹ sư Công nghệ thông tin', 'Kỹ thuật phần mềm', 'DAI_HOC', 'Đại học Xây dựng Hà Nội', 2021);

-- Phòng Nhân sự -----------------------------------------------------------------
INSERT INTO USER_QUALIFICATIONS (USER_ID, DEGREE_NAME, MAJOR, DEGREE_LEVEL, SCHOOL, GRAD_YEAR)
VALUES (3, 'Cử nhân Quản trị nhân lực', 'Quản trị nhân lực', 'DAI_HOC', 'Đại học Kinh tế Quốc dân', 2015);
INSERT INTO USER_QUALIFICATIONS (USER_ID, DEGREE_NAME, MAJOR, DEGREE_LEVEL, SCHOOL, GRAD_YEAR)
VALUES (12, 'Cử nhân Quản trị nhân lực', 'Quản trị nhân lực', 'DAI_HOC', 'Đại học Lao động - Xã hội', 2021);
INSERT INTO USER_QUALIFICATIONS (USER_ID, DEGREE_NAME, MAJOR, DEGREE_LEVEL, SCHOOL, GRAD_YEAR)
VALUES (13, 'Cử nhân Tâm lý học', 'Tâm lý học tổ chức', 'DAI_HOC', 'Đại học Khoa học Xã hội và Nhân văn', 2020);

-- Phòng Hành chính --------------------------------------------------------------
INSERT INTO USER_QUALIFICATIONS (USER_ID, DEGREE_NAME, MAJOR, DEGREE_LEVEL, SCHOOL, GRAD_YEAR)
VALUES (4, 'Cử nhân Kế toán', 'Kế toán - Kiểm toán', 'DAI_HOC', 'Học viện Tài chính', 2014);
INSERT INTO USER_QUALIFICATIONS (USER_ID, DEGREE_NAME, MAJOR, DEGREE_LEVEL, SCHOOL, GRAD_YEAR)
VALUES (14, 'Cử nhân Kế toán', 'Kế toán doanh nghiệp', 'DAI_HOC', 'Học viện Tài chính', 2021);
INSERT INTO USER_QUALIFICATIONS (USER_ID, DEGREE_NAME, MAJOR, DEGREE_LEVEL, SCHOOL, GRAD_YEAR)
VALUES (15, 'Cử nhân Quản trị văn phòng', 'Quản trị văn phòng', 'DAI_HOC', 'Đại học Nội vụ Hà Nội', 2022);

-- Giám đốc ----------------------------------------------------------------------
INSERT INTO USER_QUALIFICATIONS (USER_ID, DEGREE_NAME, MAJOR, DEGREE_LEVEL, SCHOOL, GRAD_YEAR)
VALUES (1, 'Thạc sĩ Quản trị kinh doanh', 'Quản trị kinh doanh', 'THAC_SI', 'Đại học Kinh tế Quốc dân', 2012);

COMMIT;

--==============================================================================
-- BƯỚC 6: USER_SKILLS — kỹ năng khớp chuyên môn từng phòng
--==============================================================================

-- Nhóm Backend (5 An, 7 Cường, 8 Dung, 9 Giang) ---------------------------------
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (5, 'C#', 5, 'Lập trình backend với ASP.NET Core');
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (5, 'ASP.NET Core', 5, 'Xây dựng Web API, xác thực JWT');
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (5, 'Oracle', 4, 'Thiết kế cơ sở dữ liệu, tối ưu truy vấn');
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (5, 'SQL', 5, 'Truy vấn phức tạp, thủ tục lưu trữ');

INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (7, 'C#', 4, 'Lập trình backend');
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (7, 'ASP.NET Core', 4, 'Web API, Entity Framework Core');
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (7, 'Oracle', 3, 'Truy vấn và xử lý dữ liệu');

INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (8, 'C#', 3, 'Lập trình backend');
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (8, 'ASP.NET Core', 3, 'Xây dựng Web API');
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (8, 'Kiểm thử', 3, 'Viết unit test');

INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (9, 'Oracle', 5, 'Quản trị cơ sở dữ liệu, tối ưu chỉ mục');
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (9, 'SQL', 5, 'Tối ưu truy vấn, phân tích execution plan');
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (9, 'Phân tích dữ liệu', 4, 'Báo cáo và thống kê');

-- Nhóm Frontend (6 Bình, 10 Hòa, 11 Khánh) --------------------------------------
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (6, 'Angular', 5, 'Xây dựng giao diện web');
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (6, 'TypeScript', 5, 'Lập trình frontend');
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (6, 'CSS', 4, 'Thiết kế giao diện đáp ứng');

INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (10, 'Angular', 4, 'Xây dựng component và service');
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (10, 'TypeScript', 4, 'Lập trình frontend');
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (10, 'CSS', 3, 'Dựng giao diện');

INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (11, 'Kiểm thử', 5, 'Kiểm thử tự động và thủ công');
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (11, 'Angular', 3, 'Đọc hiểu mã frontend');
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (11, 'SQL', 3, 'Kiểm tra dữ liệu');

-- Trưởng phòng Phát triển (2 Ngọc) ----------------------------------------------
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (2, 'Quản lý dự án', 5, 'Điều phối nhóm phát triển');
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (2, 'Phân tích nghiệp vụ', 4, 'Khảo sát và đặc tả yêu cầu');

-- Phòng Nhân sự (3 Trang, 12 Lan, 13 Minh) --------------------------------------
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (3, 'Quản trị nhân sự', 5, 'Chế độ chính sách, hồ sơ lao động');
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (12, 'Tuyển dụng', 4, 'Sàng lọc hồ sơ và phỏng vấn');
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (12, 'Quản trị nhân sự', 3, 'Quản lý hồ sơ nhân viên');
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (13, 'Đào tạo', 4, 'Tổ chức khoá đào tạo nội bộ');

-- Phòng Hành chính (4 Thắng, 14 Nga, 15 Phúc) ------------------------------------
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (4, 'Kế toán', 5, 'Kế toán tổng hợp, báo cáo tài chính');
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (14, 'Kế toán', 4, 'Kế toán thu chi và công nợ');
INSERT INTO USER_SKILLS (USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION) VALUES (15, 'Văn thư', 4, 'Quản lý công văn và lưu trữ');

COMMIT;

--==============================================================================
-- BƯỚC 7: Đặt lại bộ đếm IDENTITY
--
-- Script chèn ID tường minh nhưng bộ đếm không tự nhảy theo. Không đặt lại thì
-- INSERT đầu tiên từ ứng dụng sẽ xin ID = 1 và dính ORA-00001.
--==============================================================================
ALTER TABLE DEPARTMENTS         MODIFY (ID GENERATED BY DEFAULT AS IDENTITY (START WITH LIMIT VALUE));
ALTER TABLE USERS               MODIFY (ID GENERATED BY DEFAULT AS IDENTITY (START WITH LIMIT VALUE));
ALTER TABLE USER_SKILLS         MODIFY (ID GENERATED BY DEFAULT AS IDENTITY (START WITH LIMIT VALUE));
ALTER TABLE USER_QUALIFICATIONS MODIFY (ID GENERATED BY DEFAULT AS IDENTITY (START WITH LIMIT VALUE));
COMMIT;

--==============================================================================
-- KIỂM CHỨNG
--==============================================================================
SET PAGESIZE 100 LINESIZE 220

PROMPT === Cây tổ chức ===
SELECT LPAD(' ', (LEVEL - 1) * 3) || u.FULL_NAME AS NHAN_SU,
       u.USER_ROLE, d.NAME AS PHONG_BAN
FROM   USERS u LEFT JOIN DEPARTMENTS d ON d.ID = u.DEPARTMENT_ID
START  WITH u.MANAGER_ID IS NULL
CONNECT BY PRIOR u.ID = u.MANAGER_ID
ORDER  SIBLINGS BY u.ID;

PROMPT === Số người mỗi phòng ===
SELECT d.NAME, COUNT(u.ID) AS SO_NGUOI
FROM   DEPARTMENTS d LEFT JOIN USERS u ON u.DEPARTMENT_ID = d.ID
GROUP  BY d.NAME ORDER BY d.NAME;

PROMPT === Bằng cấp theo phòng ===
SELECT d.NAME AS PHONG, q.MAJOR, COUNT(*) AS SO_NGUOI
FROM   USER_QUALIFICATIONS q
       JOIN USERS u ON u.ID = q.USER_ID
       JOIN DEPARTMENTS d ON d.ID = u.DEPARTMENT_ID
GROUP  BY d.NAME, q.MAJOR ORDER BY d.NAME, q.MAJOR;
