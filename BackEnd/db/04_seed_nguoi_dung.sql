--------------------------------------------------------------------------------
-- 04_seed_nguoi_dung.sql
-- Nap NGUOI DUNG va KY NANG. Chay bang tai khoan TASK_APP tren PDB XEPDB1.
--
-- MAT KHAU DEMO cua moi tai khoan: 123456
--   Cot PASSWORD_HASH la hash BCrypt ($2a$, work factor 11), khong phai mat khau ro.
--   Da kiem chung bang BCrypt.Net-Next 4.0.3: Verify("123456", hash) == true.
--   Khi ung dung tao tai khoan moi thi tu sinh hash, KHONG dung lai hash o day.
--   Day la du lieu demo — khong dung mat khau nay o moi truong that.
--
-- USER_SKILLS la nguon du lieu BAT BUOC cho AI goi y nguoi thuc hien.
--   Khong co bang nay thi SkillSimilarity bang 0 voi tat ca moi nguoi.
--
-- TEN COT bam DUNG schema Oracle dang chay: USER_ROLE, USER_STATUS
--   (ban cu dung ROLE / STATUS — sai, se bao ORA-00904).
--
-- CHAY LAI DUOC: xoa sach du lieu 6 bang nghiep vu theo dung thu tu khoa ngoai
-- roi nap lai voi ID co dinh, cuoi cung dat lai bo dem IDENTITY.
--
-- CANH BAO: cac lenh DELETE duoi day xoa TOAN BO du lieu nghiep vu.
--           Chi chay tren may phat trien.
--------------------------------------------------------------------------------

--==============================================================================
-- BUOC 1: Xoa du lieu cu theo dung thu tu phu thuoc khoa ngoai
--==============================================================================
DELETE FROM TASK_ATTACHMENTS;
DELETE FROM TASK_REPORTS;
DELETE FROM TASK_PROGRESS;
DELETE FROM TASKS;
DELETE FROM USER_SKILLS;
DELETE FROM USERS;
COMMIT;

--==============================================================================
-- BUOC 2: USERS - 2 MANAGER (nguoi giao) va 8 EMPLOYEE (nguoi thuc hien)
--         Mat khau demo cua tat ca: 123456
--==============================================================================
INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS, CREATED_AT, UPDATED_AT)
VALUES (1, 'manager1', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi', 'Trần Quốc Hưng', 'hung.tq@congty.vn', 'MANAGER', 'ACTIVE', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS, CREATED_AT, UPDATED_AT)
VALUES (2, 'manager2', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi', 'Phạm Thị Bích Ngọc', 'ngoc.ptb@congty.vn', 'MANAGER', 'ACTIVE', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS, CREATED_AT, UPDATED_AT)
VALUES (3, 'nv.an', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi', 'Nguyễn Văn An', 'an.nv@congty.vn', 'EMPLOYEE', 'ACTIVE', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS, CREATED_AT, UPDATED_AT)
VALUES (4, 'nv.binh', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi', 'Lê Thị Bình', 'binh.lt@congty.vn', 'EMPLOYEE', 'ACTIVE', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS, CREATED_AT, UPDATED_AT)
VALUES (5, 'nv.cuong', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi', 'Phạm Minh Cường', 'cuong.pm@congty.vn', 'EMPLOYEE', 'ACTIVE', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS, CREATED_AT, UPDATED_AT)
VALUES (6, 'nv.dung', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi', 'Trần Thị Dung', 'dung.tt@congty.vn', 'EMPLOYEE', 'ACTIVE', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS, CREATED_AT, UPDATED_AT)
VALUES (7, 'nv.giang', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi', 'Hoàng Văn Giang', 'giang.hv@congty.vn', 'EMPLOYEE', 'ACTIVE', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS, CREATED_AT, UPDATED_AT)
VALUES (8, 'nv.hoa', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi', 'Đỗ Thị Hòa', 'hoa.dt@congty.vn', 'EMPLOYEE', 'ACTIVE', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS, CREATED_AT, UPDATED_AT)
VALUES (9, 'nv.khanh', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi', 'Vũ Đình Khánh', 'khanh.vd@congty.vn', 'EMPLOYEE', 'ACTIVE', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS, CREATED_AT, UPDATED_AT)
VALUES (10, 'nv.lan', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi', 'Ngô Thị Lan', 'lan.nt@congty.vn', 'EMPLOYEE', 'ACTIVE', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
COMMIT;

--==============================================================================
-- BUOC 3: USER_SKILLS - moi nhan vien 2..4 ky nang, SKILL_LEVEL 1..5
--         Day la du lieu dau vao chinh cho chuc nang AI goi y nguoi thuc hien.
--==============================================================================
INSERT INTO USER_SKILLS (ID, USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION, CREATED_AT, UPDATED_AT)
VALUES (1, 3, 'C#', 5, 'Viết dịch vụ nghiệp vụ và API bằng C# 12', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USER_SKILLS (ID, USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION, CREATED_AT, UPDATED_AT)
VALUES (2, 3, 'ASP.NET Core', 4, 'Xây dựng Web API, middleware, phân quyền', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USER_SKILLS (ID, USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION, CREATED_AT, UPDATED_AT)
VALUES (3, 3, 'Oracle', 4, 'Viết PL/SQL, tối ưu câu truy vấn', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USER_SKILLS (ID, USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION, CREATED_AT, UPDATED_AT)
VALUES (4, 3, 'SQL', 4, 'Truy vấn tổng hợp, hàm phân tích', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USER_SKILLS (ID, USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION, CREATED_AT, UPDATED_AT)
VALUES (5, 4, 'Angular', 4, 'Dựng giao diện quản trị, form động', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USER_SKILLS (ID, USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION, CREATED_AT, UPDATED_AT)
VALUES (6, 4, 'TypeScript', 4, 'Viết service và mô hình dữ liệu phía giao diện', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USER_SKILLS (ID, USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION, CREATED_AT, UPDATED_AT)
VALUES (7, 4, 'HTML/CSS', 3, 'Dựng giao diện đáp ứng nhiều kích thước màn hình', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USER_SKILLS (ID, USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION, CREATED_AT, UPDATED_AT)
VALUES (8, 5, 'Java', 5, 'Phát triển dịch vụ nền tảng bằng Spring Boot', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USER_SKILLS (ID, USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION, CREATED_AT, UPDATED_AT)
VALUES (9, 5, 'SQL', 4, 'Chuẩn hóa dữ liệu và viết báo cáo truy vấn', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USER_SKILLS (ID, USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION, CREATED_AT, UPDATED_AT)
VALUES (10, 5, 'Oracle', 3, 'Quản trị bảng, chỉ mục và sao lưu', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USER_SKILLS (ID, USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION, CREATED_AT, UPDATED_AT)
VALUES (11, 6, 'Báo cáo', 5, 'Tổng hợp và trình bày báo cáo định kỳ', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USER_SKILLS (ID, USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION, CREATED_AT, UPDATED_AT)
VALUES (12, 6, 'Văn thư', 4, 'Soạn thảo, lưu trữ và phát hành văn bản', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USER_SKILLS (ID, USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION, CREATED_AT, UPDATED_AT)
VALUES (13, 7, 'Python', 4, 'Viết script xử lý dữ liệu và tự động hóa', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USER_SKILLS (ID, USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION, CREATED_AT, UPDATED_AT)
VALUES (14, 7, 'Phân tích dữ liệu', 3, 'Làm sạch dữ liệu và dựng biểu đồ thống kê', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USER_SKILLS (ID, USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION, CREATED_AT, UPDATED_AT)
VALUES (15, 7, 'SQL', 3, 'Truy vấn dữ liệu phục vụ phân tích', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USER_SKILLS (ID, USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION, CREATED_AT, UPDATED_AT)
VALUES (16, 8, 'Kiểm thử', 4, 'Viết kịch bản kiểm thử và ghi nhận lỗi', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USER_SKILLS (ID, USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION, CREATED_AT, UPDATED_AT)
VALUES (17, 8, 'Văn thư', 3, 'Kiểm tra thể thức văn bản trước khi phát hành', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USER_SKILLS (ID, USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION, CREATED_AT, UPDATED_AT)
VALUES (18, 8, 'Báo cáo', 3, 'Soạn báo cáo kết quả kiểm thử', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USER_SKILLS (ID, USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION, CREATED_AT, UPDATED_AT)
VALUES (19, 9, 'C#', 3, 'Bảo trì và sửa lỗi mã nguồn nghiệp vụ', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USER_SKILLS (ID, USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION, CREATED_AT, UPDATED_AT)
VALUES (20, 9, 'Angular', 3, 'Sửa lỗi giao diện và bổ sung màn hình nhỏ', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USER_SKILLS (ID, USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION, CREATED_AT, UPDATED_AT)
VALUES (21, 9, 'Oracle', 2, 'Đọc hiểu lược đồ và viết truy vấn cơ bản', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USER_SKILLS (ID, USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION, CREATED_AT, UPDATED_AT)
VALUES (22, 9, 'Kiểm thử', 4, 'Kiểm thử hồi quy trước mỗi lần phát hành', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USER_SKILLS (ID, USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION, CREATED_AT, UPDATED_AT)
VALUES (23, 10, 'Python', 5, 'Xây dựng mô hình chấm điểm và xử lý ngôn ngữ', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USER_SKILLS (ID, USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION, CREATED_AT, UPDATED_AT)
VALUES (24, 10, 'Báo cáo', 4, 'Trình bày kết quả phân tích cho lãnh đạo', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
INSERT INTO USER_SKILLS (ID, USER_ID, SKILL_NAME, SKILL_LEVEL, DESCRIPTION, CREATED_AT, UPDATED_AT)
VALUES (25, 10, 'Phân tích dữ liệu', 4, 'Thống kê tỷ lệ đúng hạn và khối lượng công việc', TIMESTAMP '2026-01-02 08:00:00', TIMESTAMP '2026-01-02 08:00:00');
COMMIT;

--==============================================================================
-- Dat lai bo dem IDENTITY cua 2 bang vua nap
--
-- VI SAO CAN: script chen ID tuong minh (1..10). Cot ID khai
-- GENERATED BY DEFAULT AS IDENTITY nen cho phep chen tay, NHUNG bo dem ben trong
-- KHONG tu nhay theo. Khong dat lai thi INSERT dau tien tu ung dung se xin ID = 1
-- va dinh ORA-00001 (trung khoa chinh).
-- START WITH LIMIT VALUE = dat bo dem bang MAX(ID) hien co + 1.
--==============================================================================
ALTER TABLE USERS       MODIFY (ID GENERATED BY DEFAULT AS IDENTITY (START WITH LIMIT VALUE));
ALTER TABLE USER_SKILLS MODIFY (ID GENERATED BY DEFAULT AS IDENTITY (START WITH LIMIT VALUE));
COMMIT;

--==============================================================================
-- Kiem chung
--==============================================================================
SELECT 'USERS       = ' || COUNT(*) FROM USERS;
SELECT 'USER_SKILLS = ' || COUNT(*) FROM USER_SKILLS;
SELECT USER_ROLE || ' : ' || COUNT(*) FROM USERS GROUP BY USER_ROLE ORDER BY 1;

-- Tieng Viet phai con nguyen: SO_BYTE lon hon SO_KY_TU
SELECT FULL_NAME, LENGTH(FULL_NAME) AS SO_KY_TU, LENGTHB(FULL_NAME) AS SO_BYTE
FROM   USERS ORDER BY ID;
