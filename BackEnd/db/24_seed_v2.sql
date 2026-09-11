--------------------------------------------------------------------------------
-- 24_seed_v2.sql
-- Nạp dữ liệu cho phần lược đồ V2: danh mục kỹ năng, nhóm, hồ sơ năng lực, kỹ năng
-- yêu cầu của từng nhiệm vụ, điểm đánh giá kết quả.
-- Chạy bằng tài khoản TASK_APP, SAU 23_nang_cap_v2.sql và 22_seed_nhiem_vu.sql.
--
-- THỨ TỰ DỰNG LẠI TOÀN BỘ DỮ LIỆU DEMO:  21 -> 22 -> 24
--   21 xoá và nạp lại người dùng, 22 xoá và nạp lại nhiệm vụ. Cả hai đều làm mất phần
--   dữ liệu V2 gắn với chúng, nên sau đó luôn chạy lại file này.
--
-- KHÔNG xoá người dùng hay nhiệm vụ nào. Chỉ thêm và cập nhật. Chạy lại được.
--
-- NHỮNG GÌ FILE NÀY NẠP
--   1. SKILLS: 23 kỹ năng chuẩn hoá. Có một kỹ năng không ai sở hữu (Học máy) để thử
--      trường hợp nhiệm vụ đòi hỏi thứ cả phòng chưa ai biết.
--   2. TEAMS: phòng Phát triển chia hai nhóm Backend và Frontend. Hai phòng còn lại ít
--      người nên không chia nhóm, nhân viên báo cáo thẳng lên trưởng phòng.
--   3. Nhân viên mới Lý Minh Tuấn (ID 16) vào làm từ 17/08/2026, chưa có nhiệm vụ nào.
--      Đây là ca KHỞI ĐẦU LẠNH: không có lịch sử để chấm, hệ thống phải dùng giá trị
--      mặc định trung tính chứ không được cho 0 điểm.
--   4. Ngày vào làm từng người, khớp với năm tốt nghiệp trong USER_QUALIFICATIONS.
--   5. Chuyển USER_SKILLS sang mã chuẩn. Tên "Văn thư" quy về mã RECORDS — đúng việc
--      chuẩn hoá mà bảng SKILLS sinh ra để làm.
--   6. Kỹ năng yêu cầu cho cả 90 nhiệm vụ, nguồn MANUAL. Đây là NHÃN ĐÚNG để đo xem
--      AI tự trích kỹ năng từ mô tả nhiệm vụ có chính xác không.
--   7. Điểm chất lượng và mức hoàn thành cho mọi báo cáo đã duyệt hoặc bị trả về,
--      thiết kế khớp với hồ sơ đúng hạn đã dựng ở script 22.
--------------------------------------------------------------------------------

SET DEFINE OFF
SET SERVEROUTPUT ON

--==============================================================================
-- BƯỚC 1: SKILLS — danh mục kỹ năng
--==============================================================================
DECLARE
  PROCEDURE kn(p_id NUMBER, p_code VARCHAR2, p_name VARCHAR2,
               p_cat VARCHAR2, p_desc VARCHAR2) IS
  BEGIN
    MERGE INTO SKILLS s
    USING (SELECT p_id AS ID FROM DUAL) x ON (s.ID = x.ID)
    WHEN MATCHED THEN UPDATE SET
         s.CODE = p_code, s.NAME = p_name, s.CATEGORY = p_cat,
         s.DESCRIPTION = p_desc, s.UPDATED_AT = CURRENT_TIMESTAMP
    WHEN NOT MATCHED THEN INSERT (ID, CODE, NAME, CATEGORY, DESCRIPTION)
         VALUES (p_id, p_code, p_name, p_cat, p_desc);
  END;
BEGIN
  -- Backend
  kn( 1, 'CSHARP', 'C#', 'BACKEND',
      'Lập trình C# trên nền .NET: hướng đối tượng, xử lý bất đồng bộ, LINQ, xử lý ngoại lệ và quản lý bộ nhớ.');
  kn( 2, 'ASPNET_CORE', 'ASP.NET Core', 'BACKEND',
      'Xây dựng Web API phía máy chủ bằng ASP.NET Core: định tuyến, middleware, xác thực JWT, Entity Framework Core, tài liệu Swagger.');
  kn( 3, 'SECURITY', 'Bảo mật ứng dụng', 'BACKEND',
      'Bảo mật ứng dụng web: chống chèn câu lệnh SQL, kiểm soát truy cập, quản lý phiên và mã thông báo, rà soát lỗ hổng.');
  -- Dữ liệu
  kn( 4, 'ORACLE', 'Oracle', 'DATA',
      'Quản trị và phát triển trên Oracle Database: PL/SQL, sao lưu và khôi phục bằng RMAN, đọc kế hoạch thực thi, khoá và giao dịch.');
  kn( 5, 'SQL', 'SQL', 'DATA',
      'Viết và tối ưu câu truy vấn SQL, thiết kế lược đồ quan hệ, chỉ mục, ràng buộc toàn vẹn và di trú dữ liệu.');
  kn( 6, 'DATA_ANALYSIS', 'Phân tích dữ liệu', 'DATA',
      'Tổng hợp, thống kê và trực quan hoá dữ liệu bằng biểu đồ để phục vụ báo cáo và ra quyết định.');
  kn( 7, 'MACHINE_LEARNING', 'Học máy', 'DATA',
      'Xử lý ngôn ngữ tự nhiên, véc-tơ nhúng ngữ nghĩa, huấn luyện và đánh giá mô hình học máy bằng Python.');
  -- Frontend
  kn( 8, 'ANGULAR', 'Angular', 'FRONTEND',
      'Xây dựng ứng dụng web một trang bằng Angular: thành phần độc lập, tín hiệu, định tuyến, biểu mẫu và gọi API.');
  kn( 9, 'TYPESCRIPT', 'TypeScript', 'FRONTEND',
      'Lập trình TypeScript cho giao diện web: kiểu tĩnh, lập trình bất đồng bộ, RxJS.');
  kn(10, 'CSS', 'CSS', 'FRONTEND',
      'Trình bày giao diện bằng CSS: bố cục lưới và flexbox, thiết kế đáp ứng cho màn hình hẹp, hiệu ứng chuyển động.');
  kn(11, 'UI_DESIGN', 'Thiết kế giao diện', 'FRONTEND',
      'Thiết kế trải nghiệm và giao diện người dùng, khả năng truy cập, bộ thành phần giao diện dùng chung.');
  -- Kiểm thử
  kn(12, 'TESTING', 'Kiểm thử', 'TESTING',
      'Thiết kế ca kiểm thử, kiểm thử chức năng, hồi quy, đầu cuối, kiểm thử hiệu năng và kiểm thử tự động.');
  -- Quản lý
  kn(13, 'BUSINESS_ANALYSIS', 'Phân tích nghiệp vụ', 'MANAGEMENT',
      'Thu thập và đặc tả yêu cầu, mô hình hoá quy trình nghiệp vụ, làm cầu nối giữa người dùng và đội phát triển.');
  kn(14, 'PROJECT_MGMT', 'Quản lý dự án', 'MANAGEMENT',
      'Lập kế hoạch, phân bổ nguồn lực, theo dõi tiến độ, quản lý rủi ro và nghiệm thu dự án.');
  -- Nhân sự
  kn(15, 'HR_MGMT', 'Quản trị nhân sự', 'HR',
      'Xây dựng chính sách nhân sự, khung năng lực, hợp đồng lao động, đánh giá hiệu suất và chế độ đãi ngộ.');
  kn(16, 'RECRUITMENT', 'Tuyển dụng', 'HR',
      'Đăng tin tuyển dụng, sàng lọc hồ sơ ứng viên, tổ chức phỏng vấn và tiếp nhận nhân sự mới.');
  kn(17, 'TRAINING', 'Đào tạo', 'HR',
      'Xây dựng chương trình, tổ chức khoá đào tạo nội bộ, biên soạn tài liệu và đánh giá hiệu quả đào tạo.');
  -- Kế toán
  kn(18, 'ACCOUNTING', 'Kế toán', 'ACCOUNTING',
      'Kế toán tổng hợp, lập báo cáo tài chính, đối chiếu công nợ, kiểm kê tài sản và kiểm soát chi phí.');
  kn(19, 'TAX', 'Thuế', 'ACCOUNTING',
      'Kê khai và quyết toán thuế thu nhập doanh nghiệp, thuế giá trị gia tăng và thuế thu nhập cá nhân.');
  kn(20, 'PAYROLL', 'Tiền lương và bảo hiểm', 'ACCOUNTING',
      'Tính lương theo bảng chấm công, phụ cấp, khấu trừ thuế thu nhập cá nhân và đóng bảo hiểm xã hội.');
  -- Hành chính
  kn(21, 'RECORDS', 'Văn thư lưu trữ', 'ADMIN',
      'Quản lý công văn đi và đến, số hoá, phân loại và lưu trữ hồ sơ theo quy định.');
  kn(22, 'EVENTS', 'Tổ chức sự kiện', 'ADMIN',
      'Lên kế hoạch, điều phối hậu cần, đặt địa điểm và tổ chức hội nghị, sự kiện nội bộ.');
  kn(23, 'PROCUREMENT', 'Hành chính mua sắm', 'ADMIN',
      'Mua sắm và cấp phát văn phòng phẩm, quản lý hợp đồng thuê và hợp đồng dịch vụ.');
END;
/
COMMIT;

--==============================================================================
-- BƯỚC 2: TEAMS
--
-- DESCRIPTION là phần AI đọc để đoán nhiệm vụ thuộc nhóm nào, nên liệt kê cụ thể
-- nhóm làm những việc gì.
--==============================================================================
MERGE INTO TEAMS t
USING (SELECT 1 AS ID FROM DUAL) x ON (t.ID = x.ID)
WHEN MATCHED THEN UPDATE SET
     t.DEPARTMENT_ID = 2, t.CODE = 'BACKEND', t.NAME = 'Nhóm Backend',
     t.LEADER_USER_ID = 5, t.UPDATED_AT = CURRENT_TIMESTAMP,
     t.DESCRIPTION = 'Xây dựng API và dịch vụ phía máy chủ bằng C# và ASP.NET Core. Thiết kế, tối ưu và vận hành cơ sở dữ liệu Oracle: truy vấn, chỉ mục, sao lưu, di trú dữ liệu. Bảo mật, xác thực và hiệu năng hệ thống.'
WHEN NOT MATCHED THEN INSERT (ID, DEPARTMENT_ID, CODE, NAME, LEADER_USER_ID, DESCRIPTION)
     VALUES (1, 2, 'BACKEND', 'Nhóm Backend', 5,
             'Xây dựng API và dịch vụ phía máy chủ bằng C# và ASP.NET Core. Thiết kế, tối ưu và vận hành cơ sở dữ liệu Oracle: truy vấn, chỉ mục, sao lưu, di trú dữ liệu. Bảo mật, xác thực và hiệu năng hệ thống.');

MERGE INTO TEAMS t
USING (SELECT 2 AS ID FROM DUAL) x ON (t.ID = x.ID)
WHEN MATCHED THEN UPDATE SET
     t.DEPARTMENT_ID = 2, t.CODE = 'FRONTEND', t.NAME = 'Nhóm Frontend',
     t.LEADER_USER_ID = 6, t.UPDATED_AT = CURRENT_TIMESTAMP,
     t.DESCRIPTION = 'Xây dựng giao diện web bằng Angular và TypeScript. Thiết kế giao diện và trải nghiệm người dùng, bố cục đáp ứng cho nhiều kích thước màn hình. Kiểm thử giao diện, kiểm thử hồi quy và kiểm thử đầu cuối.'
WHEN NOT MATCHED THEN INSERT (ID, DEPARTMENT_ID, CODE, NAME, LEADER_USER_ID, DESCRIPTION)
     VALUES (2, 2, 'FRONTEND', 'Nhóm Frontend', 6,
             'Xây dựng giao diện web bằng Angular và TypeScript. Thiết kế giao diện và trải nghiệm người dùng, bố cục đáp ứng cho nhiều kích thước màn hình. Kiểm thử giao diện, kiểm thử hồi quy và kiểm thử đầu cuối.');
COMMIT;

--==============================================================================
-- BƯỚC 3: Nhân viên mới — ca khởi đầu lạnh
--==============================================================================
MERGE INTO USERS u
USING (SELECT 16 AS ID FROM DUAL) x ON (u.ID = x.ID)
WHEN MATCHED THEN UPDATE SET
     u.USERNAME = 'nv.tuan', u.FULL_NAME = 'Lý Minh Tuấn', u.EMAIL = 'tuan.lm@congty.vn',
     u.USER_ROLE = 'EMPLOYEE', u.USER_STATUS = 'ACTIVE', u.DEPARTMENT_ID = 2,
     u.JOB_TITLE = 'Lập trình viên Backend', u.MANAGER_ID = 5,
     u.UPDATED_AT = CURRENT_TIMESTAMP
WHEN NOT MATCHED THEN INSERT
     (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, USER_ROLE, USER_STATUS,
      DEPARTMENT_ID, JOB_TITLE, MANAGER_ID)
     VALUES (16, 'nv.tuan', '$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi',
             'Lý Minh Tuấn', 'tuan.lm@congty.vn', 'EMPLOYEE', 'ACTIVE',
             2, 'Lập trình viên Backend', 5);

DELETE FROM USER_QUALIFICATIONS WHERE USER_ID = 16;
INSERT INTO USER_QUALIFICATIONS (USER_ID, DEGREE_NAME, MAJOR, DEGREE_LEVEL, SCHOOL, GRAD_YEAR)
VALUES (16, 'Kỹ sư Công nghệ thông tin', 'Kỹ thuật phần mềm', 'DAI_HOC', 'Đại học Xây dựng Hà Nội', 2026);
COMMIT;

--==============================================================================
-- BƯỚC 4: Nhóm, ngày vào làm, trưởng phòng
--
-- Ngày vào làm luôn sau năm tốt nghiệp ghi trong USER_QUALIFICATIONS.
--==============================================================================
DECLARE
  PROCEDURE hs(p_id NUMBER, p_team NUMBER, p_ngay DATE) IS
  BEGIN
    UPDATE USERS SET TEAM_ID = p_team, HIRED_DATE = p_ngay WHERE ID = p_id;
    IF SQL%ROWCOUNT = 0 THEN
      RAISE_APPLICATION_ERROR(-20002, 'Không có người dùng #' || p_id);
    END IF;
  END;
BEGIN
  hs( 1, NULL, DATE '2014-01-02');  -- Giám đốc
  hs( 2, NULL, DATE '2016-10-03');  -- Trưởng phòng Phát triển
  hs( 3, NULL, DATE '2016-03-01');  -- Trưởng phòng Nhân sự
  hs( 4, NULL, DATE '2015-05-04');  -- Trưởng phòng Hành chính
  hs( 5,    1, DATE '2018-09-04');  -- Trưởng nhóm Backend
  hs( 6,    2, DATE '2019-09-03');  -- Trưởng nhóm Frontend
  hs( 7,    1, DATE '2021-07-01');  -- Cường
  hs( 8,    1, DATE '2023-08-01');  -- Dung
  hs( 9,    1, DATE '2020-09-01');  -- Giang
  hs(16,    1, DATE '2026-08-17');  -- Tuấn, nhân viên mới
  hs(10,    2, DATE '2022-08-01');  -- Hoà
  hs(11,    2, DATE '2021-08-02');  -- Khánh
  hs(12, NULL, DATE '2021-09-06');  -- Lan
  hs(13, NULL, DATE '2022-09-05');  -- Minh
  hs(14, NULL, DATE '2021-10-04');  -- Nga
  hs(15, NULL, DATE '2022-09-12');  -- Phúc
END;
/

UPDATE DEPARTMENTS SET HEAD_USER_ID = ID WHERE ID IN (1, 2, 3, 4);
COMMIT;

--==============================================================================
-- BƯỚC 5: USER_SKILLS — chuyển sang mã chuẩn, bổ sung kỹ năng mới, số năm kinh nghiệm
--==============================================================================

-- 5a. Tên khai trùng tên chuẩn: gắn thẳng
UPDATE USER_SKILLS us
SET    SKILL_ID = (SELECT s.ID FROM SKILLS s WHERE s.NAME = us.SKILL_NAME)
WHERE  EXISTS (SELECT 1 FROM SKILLS s WHERE s.NAME = us.SKILL_NAME);

-- 5b. Tên biến thể: quy về mã chuẩn. Muốn thêm biến thể thì thêm dòng ở đây.
UPDATE USER_SKILLS
SET    SKILL_ID = (SELECT ID FROM SKILLS WHERE CODE = 'RECORDS')
WHERE  SKILL_NAME = 'Văn thư';

-- 5c. Kỹ năng thuộc danh mục mới mà script 21 chưa có
DECLARE
  PROCEDURE ky(p_user NUMBER, p_code VARCHAR2, p_level NUMBER, p_desc VARCHAR2) IS
    v_id   NUMBER;
    v_name VARCHAR2(150 CHAR);
  BEGIN
    SELECT ID, NAME INTO v_id, v_name FROM SKILLS WHERE CODE = p_code;
    MERGE INTO USER_SKILLS us
    USING (SELECT p_user AS USER_ID, v_name AS SKILL_NAME FROM DUAL) x
    ON (us.USER_ID = x.USER_ID AND us.SKILL_NAME = x.SKILL_NAME)
    WHEN MATCHED THEN UPDATE SET
         us.SKILL_ID = v_id, us.SKILL_LEVEL = p_level,
         us.DESCRIPTION = p_desc, us.UPDATED_AT = CURRENT_TIMESTAMP
    WHEN NOT MATCHED THEN INSERT (USER_ID, SKILL_NAME, SKILL_ID, SKILL_LEVEL, DESCRIPTION)
         VALUES (p_user, v_name, v_id, p_level, p_desc);
  END;
BEGIN
  ky( 5, 'SECURITY',    4, 'Xác thực JWT, phân quyền và rà soát bảo mật API');
  ky( 7, 'SECURITY',    3, 'Làm mới mã thông báo và chống dùng lại');
  ky( 6, 'UI_DESIGN',   4, 'Dựng bộ thành phần giao diện dùng chung');
  ky(10, 'UI_DESIGN',   3, 'Thiết kế màn hình theo bộ thành phần chung');
  ky( 3, 'RECRUITMENT', 4, 'Điều phối các đợt tuyển dụng lớn');
  ky( 4, 'TAX',         5, 'Quyết toán thuế doanh nghiệp');
  ky( 4, 'PAYROLL',     4, 'Duyệt bảng lương toàn công ty');
  ky(14, 'TAX',         4, 'Kê khai thuế giá trị gia tăng và thu nhập cá nhân');
  ky(14, 'PAYROLL',     4, 'Lập bảng lương và bảo hiểm hằng tháng');
  ky(15, 'EVENTS',      3, 'Tổ chức hội nghị và sự kiện nội bộ');
  ky(15, 'PROCUREMENT', 3, 'Mua sắm văn phòng phẩm và hợp đồng dịch vụ');
  -- Nhân viên mới: kỹ năng đúng chuyên môn nhưng mức còn thấp
  ky(16, 'CSHARP',      2, 'Đã làm đồ án tốt nghiệp bằng C#');
  ky(16, 'ASPNET_CORE', 2, 'Viết được Web API cơ bản');
  ky(16, 'SQL',         2, 'Truy vấn cơ bản');
END;
/

-- 5d. Không được sót dòng nào chưa có mã chuẩn
DECLARE
  v_sot VARCHAR2(2000);
BEGIN
  SELECT LISTAGG(DISTINCT SKILL_NAME, ', ') WITHIN GROUP (ORDER BY SKILL_NAME)
  INTO   v_sot FROM USER_SKILLS WHERE SKILL_ID IS NULL;

  IF v_sot IS NOT NULL THEN
    RAISE_APPLICATION_ERROR(-20003,
      'Còn kỹ năng chưa quy được về mã chuẩn: ' || v_sot ||
      '. Thêm vào danh mục SKILLS hoặc khai báo biến thể ở bước 5b.');
  END IF;
END;
/

-- 5e. Số năm kinh nghiệm theo từng kỹ năng
--   Lấy thâm niên tính tới 01/09/2026, nhưng không vượt quá 1,5 năm cho mỗi bậc
--   thành thạo — người vào làm lâu mà kỹ năng mới ở mức 2 thì không thể có 8 năm
--   kinh nghiệm với kỹ năng đó. Dữ liệu demo nên dùng công thức cho nhất quán.
UPDATE USER_SKILLS us
SET    YEARS_EXPERIENCE = (
         SELECT ROUND(LEAST(GREATEST(MONTHS_BETWEEN(DATE '2026-09-01', u.HIRED_DATE) / 12, 0),
                            NVL(us.SKILL_LEVEL, 1) * 1.5), 1)
         FROM   USERS u WHERE u.ID = us.USER_ID);
COMMIT;

--==============================================================================
-- BƯỚC 6: TASKS — nhóm phụ trách và ước lượng công sức
--
-- Nhóm phụ trách lấy theo nhóm của người thực hiện. Nhiệm vụ giao cho trưởng phòng,
-- hoặc cho người ở phòng không chia nhóm, thì để trống.
-- Nhiệm vụ MOI_TAO chưa có người nên cũng để trống — đó là việc của AI.
--==============================================================================
UPDATE TASKS t
SET    TEAM_ID = (SELECT u.TEAM_ID FROM USERS u
                  WHERE  u.ID = t.ASSIGNEE_ID AND u.DEPARTMENT_ID = t.DEPARTMENT_ID)
WHERE  t.ASSIGNEE_ID IS NOT NULL;

-- Giờ công: mức nền theo ưu tiên, cộng dao động cố định theo ID cho khỏi đều tăm tắp
UPDATE TASKS
SET    ESTIMATED_EFFORT = CASE PRIORITY WHEN 'HIGH' THEN 32 WHEN 'MEDIUM' THEN 16 ELSE 8 END
                        + MOD(ID * 7, 5) * 2;
COMMIT;

--==============================================================================
-- BƯỚC 7: TASK_REQUIRED_SKILLS — nhãn đúng cho 90 nhiệm vụ
--
-- Cú pháp mỗi dòng: 'MA_KY_NANG:muc MA_KY_NANG:muc'. Mức là mức tối thiểu mong muốn.
-- Chỉ xoá dòng MANUAL của 90 nhiệm vụ mẫu, không đụng tới dòng do AI sinh.
--==============================================================================
DELETE FROM TASK_REQUIRED_SKILLS WHERE SOURCE = 'MANUAL' AND TASK_ID BETWEEN 1 AND 90;

DECLARE
  PROCEDURE rs(p_task NUMBER, p_ds VARCHAR2) IS
    v_ds   VARCHAR2(400) := p_ds || ' ';
    v_muc  VARCHAR2(60);
    v_code VARCHAR2(50);
    v_pos  PLS_INTEGER;
  BEGIN
    LOOP
      v_ds := LTRIM(v_ds);
      EXIT WHEN v_ds IS NULL;
      v_pos  := INSTR(v_ds, ' ');
      v_muc  := SUBSTR(v_ds, 1, v_pos - 1);
      v_ds   := SUBSTR(v_ds, v_pos + 1);
      v_code := SUBSTR(v_muc, 1, INSTR(v_muc, ':') - 1);

      INSERT INTO TASK_REQUIRED_SKILLS (TASK_ID, SKILL_ID, REQUIRED_LEVEL, SOURCE)
      SELECT p_task, s.ID, TO_NUMBER(SUBSTR(v_muc, INSTR(v_muc, ':') + 1)), 'MANUAL'
      FROM   SKILLS s WHERE s.CODE = v_code;

      IF SQL%ROWCOUNT = 0 THEN
        RAISE_APPLICATION_ERROR(-20004,
          'Nhiệm vụ ' || p_task || ' đòi mã kỹ năng không có trong danh mục: ' || v_code);
      END IF;
    END LOOP;
  END;
BEGIN
  -- Backend ---------------------------------------------------------------------
  rs( 1, 'ASPNET_CORE:4 SECURITY:3 CSHARP:3');
  rs( 2, 'SQL:4 ORACLE:4');
  rs( 3, 'ORACLE:4');
  rs( 4, 'ASPNET_CORE:3 SQL:3');
  rs( 5, 'ASPNET_CORE:3 CSHARP:3');
  rs( 6, 'SQL:4 ORACLE:3');
  rs( 7, 'ASPNET_CORE:3 CSHARP:3');
  rs( 8, 'CSHARP:4 ASPNET_CORE:3');
  rs( 9, 'ASPNET_CORE:3');
  rs(10, 'CSHARP:3 TESTING:3');
  rs(11, 'ASPNET_CORE:4 DATA_ANALYSIS:3 BUSINESS_ANALYSIS:3');
  rs(12, 'ORACLE:4 SQL:4');
  rs(13, 'ASPNET_CORE:3 SQL:3');
  rs(14, 'SECURITY:4 ASPNET_CORE:4');
  rs(15, 'ASPNET_CORE:3 SQL:2');
  rs(16, 'CSHARP:4 ASPNET_CORE:3');
  rs(17, 'ORACLE:4 SQL:4 ASPNET_CORE:3');
  rs(18, 'ASPNET_CORE:3 CSHARP:3');
  rs(19, 'SECURITY:4 ASPNET_CORE:4');
  rs(20, 'ORACLE:4 SQL:4');
  rs(21, 'CSHARP:4');
  rs(22, 'ASPNET_CORE:3');
  rs(23, 'CSHARP:4 ASPNET_CORE:4');
  rs(24, 'ASPNET_CORE:4 ORACLE:3 PROJECT_MGMT:3');
  rs(25, 'ASPNET_CORE:4 CSHARP:4 DATA_ANALYSIS:3');
  rs(26, 'ASPNET_CORE:3 CSHARP:3');
  rs(27, 'ASPNET_CORE:3 SQL:2');
  rs(28, 'MACHINE_LEARNING:3 ASPNET_CORE:3');
  rs(29, 'SQL:4 ORACLE:3');
  rs(30, 'TESTING:4 ASPNET_CORE:3');
  rs(31, 'TESTING:3 CSHARP:3');
  rs(32, 'SQL:4 ORACLE:4 BUSINESS_ANALYSIS:3');
  rs(33, 'ORACLE:4');
  rs(34, 'CSHARP:5 ASPNET_CORE:4 SECURITY:3');
  -- Frontend --------------------------------------------------------------------
  rs(35, 'ANGULAR:4 TYPESCRIPT:3');
  rs(36, 'ANGULAR:3 TYPESCRIPT:3');
  rs(37, 'ANGULAR:4 CSS:4 UI_DESIGN:4');
  rs(38, 'ANGULAR:3 CSS:3 DATA_ANALYSIS:2');
  rs(39, 'TESTING:4 CSS:2');
  rs(40, 'ANGULAR:4 TYPESCRIPT:4');
  rs(41, 'TESTING:3');
  rs(42, 'ANGULAR:4 TYPESCRIPT:3');
  rs(43, 'TESTING:4');
  rs(44, 'CSS:4 ANGULAR:3');
  rs(45, 'ANGULAR:5 TYPESCRIPT:4');
  rs(46, 'ANGULAR:3 TYPESCRIPT:3');
  rs(47, 'TESTING:4 SECURITY:2');
  rs(48, 'UI_DESIGN:3 TESTING:3');
  rs(49, 'ANGULAR:4 PROJECT_MGMT:3');
  rs(50, 'ANGULAR:4 TYPESCRIPT:3');
  rs(51, 'TESTING:4');
  rs(52, 'TESTING:3 ANGULAR:3');
  rs(53, 'ANGULAR:5 TYPESCRIPT:4');
  -- Chưa giao -------------------------------------------------------------------
  rs(54, 'ANGULAR:4 CSS:3 DATA_ANALYSIS:3');
  rs(55, 'ASPNET_CORE:3 CSHARP:3 SQL:2');
  -- Nhân sự ---------------------------------------------------------------------
  rs(56, 'RECRUITMENT:4');
  rs(57, 'RECRUITMENT:3');
  rs(58, 'TRAINING:4');
  rs(59, 'TRAINING:3 SECURITY:2');
  rs(60, 'HR_MGMT:3 RECRUITMENT:3');
  rs(61, 'TRAINING:4 DATA_ANALYSIS:2');
  rs(62, 'HR_MGMT:4');
  rs(63, 'HR_MGMT:5 BUSINESS_ANALYSIS:3');
  rs(64, 'RECRUITMENT:4');
  rs(65, 'TRAINING:3');
  rs(66, 'TRAINING:3 DATA_ANALYSIS:2');
  rs(67, 'RECRUITMENT:4');
  rs(68, 'HR_MGMT:3 DATA_ANALYSIS:3');
  rs(69, 'HR_MGMT:3');
  -- Hành chính - Kế toán --------------------------------------------------------
  rs(70, 'TAX:4 ACCOUNTING:4');
  rs(71, 'PAYROLL:4');
  rs(72, 'ACCOUNTING:4');
  rs(73, 'RECORDS:4');
  rs(74, 'PROCUREMENT:3');
  rs(75, 'ACCOUNTING:5');
  rs(76, 'EVENTS:4');
  rs(77, 'ACCOUNTING:3');
  rs(78, 'EVENTS:3');
  rs(79, 'PAYROLL:4');
  rs(80, 'RECORDS:3');
  rs(81, 'PAYROLL:4');
  rs(82, 'ACCOUNTING:3');
  rs(83, 'PROCUREMENT:3');
  -- Giám đốc giao cho trưởng phòng ---------------------------------------------
  rs(84, 'PROJECT_MGMT:5 BUSINESS_ANALYSIS:4');
  rs(85, 'HR_MGMT:5');
  rs(86, 'ACCOUNTING:5');
  rs(87, 'PROJECT_MGMT:5');
  rs(88, 'HR_MGMT:5');
  -- Chưa giao -------------------------------------------------------------------
  rs(89, 'ACCOUNTING:4 PROJECT_MGMT:3 DATA_ANALYSIS:3');
  rs(90, 'ORACLE:4 SQL:4');
END;
/
COMMIT;

--==============================================================================
-- BƯỚC 8: Điểm đánh giá cho báo cáo đã duyệt hoặc bị trả về
--
-- Mỗi người có một mức chất lượng nền, khớp với hồ sơ đã dựng ở script 22. Chất
-- lượng và đúng hạn là hai trục KHÁC NHAU: Khánh hay trễ nhưng làm kỹ, Lan vừa
-- đúng hạn vừa ổn. Nhờ vậy HistoricalPerformance và OnTimeScore không trùng thông tin.
--
--   5  Giang, Cường, An, Nga
--   4  Bình, Hoà, Lan, Khánh, ba trưởng phòng
--   3  Phúc, Dung, Minh
--
-- Báo cáo bị trả về luôn 2 điểm chất lượng. Nhiệm vụ từng bị trả về thì lần nộp lại
-- đạt nhưng bị trừ một bậc.
--==============================================================================
DECLARE
  v_q       NUMBER;
  v_c       NUMBER;
  v_lam_lai NUMBER;

  FUNCTION nen(p_user NUMBER) RETURN NUMBER IS
  BEGIN
    RETURN CASE
             WHEN p_user IN (9, 7, 5, 14)              THEN 5
             WHEN p_user IN (6, 10, 12, 11, 2, 3, 4)   THEN 4
             ELSE 3
           END;
  END;
BEGIN
  UPDATE TASK_REPORTS SET QUALITY_SCORE = NULL, COMPLETION_SCORE = NULL;

  FOR r IN (SELECT rp.ID, rp.TASK_ID, rp.REPORT_STATUS, t.ASSIGNEE_ID, t.STATUS_CODE
            FROM   TASK_REPORTS rp JOIN TASKS t ON t.ID = rp.TASK_ID
            WHERE  rp.REPORT_STATUS IN ('DA_XAC_NHAN', 'TU_CHOI')) LOOP

    IF r.REPORT_STATUS = 'TU_CHOI' THEN
      v_q := 2;
      -- Đang chờ bổ sung thì đã làm được kha khá; bị trả về rồi mới làm lại thì thiếu nhiều
      v_c := CASE WHEN r.STATUS_CODE = 'YEU_CAU_BO_SUNG' THEN 3 ELSE 2 END;
    ELSE
      v_q := nen(r.ASSIGNEE_ID);

      -- Dao động cố định theo ID để không ai toàn điểm tuyệt đối
      IF v_q >= 4 AND MOD(r.TASK_ID, 4) = 0 THEN v_q := v_q - 1; END IF;
      IF v_q <= 3 AND MOD(r.TASK_ID, 5) = 0 THEN v_q := v_q + 1; END IF;

      SELECT COUNT(*) INTO v_lam_lai FROM TASK_REPORTS x
      WHERE  x.TASK_ID = r.TASK_ID AND x.REPORT_STATUS = 'TU_CHOI';
      IF v_lam_lai > 0 THEN v_q := GREATEST(2, v_q - 1); END IF;

      v_c := v_q;
      IF MOD(r.TASK_ID, 3) = 1 AND v_c < 5 THEN v_c := v_c + 1; END IF;
    END IF;

    UPDATE TASK_REPORTS SET QUALITY_SCORE = v_q, COMPLETION_SCORE = v_c WHERE ID = r.ID;
  END LOOP;
END;
/
COMMIT;

--==============================================================================
-- BƯỚC 9: Đặt lại bộ đếm IDENTITY
--==============================================================================
ALTER TABLE SKILLS               MODIFY (ID GENERATED BY DEFAULT AS IDENTITY (START WITH LIMIT VALUE));
ALTER TABLE TEAMS                MODIFY (ID GENERATED BY DEFAULT AS IDENTITY (START WITH LIMIT VALUE));
ALTER TABLE TASK_REQUIRED_SKILLS MODIFY (ID GENERATED BY DEFAULT AS IDENTITY (START WITH LIMIT VALUE));
ALTER TABLE USERS                MODIFY (ID GENERATED BY DEFAULT AS IDENTITY (START WITH LIMIT VALUE));
ALTER TABLE USER_SKILLS          MODIFY (ID GENERATED BY DEFAULT AS IDENTITY (START WITH LIMIT VALUE));
ALTER TABLE USER_QUALIFICATIONS  MODIFY (ID GENERATED BY DEFAULT AS IDENTITY (START WITH LIMIT VALUE));
COMMIT;

--==============================================================================
-- KIỂM CHỨNG
--==============================================================================
SET PAGESIZE 200 LINESIZE 200
SET FEEDBACK OFF
COLUMN NHOM FORMAT A10
COLUMN FULL_NAME FORMAT A22
COLUMN USER_ROLE FORMAT A10
COLUMN CATEGORY FORMAT A12

PROMPT
PROMPT === Số dòng ===
SELECT 'SKILLS=' || (SELECT COUNT(*) FROM SKILLS) ||
       '  TEAMS=' || (SELECT COUNT(*) FROM TEAMS) ||
       '  USERS=' || (SELECT COUNT(*) FROM USERS) ||
       '  USER_SKILLS=' || (SELECT COUNT(*) FROM USER_SKILLS) ||
       '  TASK_REQUIRED_SKILLS=' || (SELECT COUNT(*) FROM TASK_REQUIRED_SKILLS) AS SO_DONG
FROM DUAL;

PROMPT
PROMPT === Thành viên từng nhóm ===
SELECT t.CODE AS NHOM, u.ID, u.FULL_NAME, u.USER_ROLE,
       TO_CHAR(u.HIRED_DATE, 'DD/MM/YYYY') AS VAO_LAM
FROM   USERS u JOIN TEAMS t ON t.ID = u.TEAM_ID
ORDER  BY t.CODE, DECODE(u.USER_ROLE, 'TEAM_LEAD', 1, 2), u.ID;

PROMPT
PROMPT === Kỹ năng chưa quy về mã chuẩn (phải là 0) ===
SELECT COUNT(*) AS CHUA_CHUAN_HOA FROM USER_SKILLS WHERE SKILL_ID IS NULL;

PROMPT
PROMPT === Danh mục kỹ năng: bao nhiêu người có, bao nhiêu nhiệm vụ cần ===
SELECT s.CATEGORY, s.CODE,
       (SELECT COUNT(*) FROM USER_SKILLS us WHERE us.SKILL_ID = s.ID) AS SO_NGUOI_CO,
       (SELECT COUNT(*) FROM TASK_REQUIRED_SKILLS r WHERE r.SKILL_ID = s.ID) AS SO_VIEC_CAN
FROM   SKILLS s ORDER BY s.CATEGORY, s.CODE;

PROMPT
PROMPT === Nhiệm vụ chưa có kỹ năng yêu cầu (phải là 0) ===
SELECT COUNT(*) AS THIEU_KY_NANG FROM TASKS t
WHERE  NOT EXISTS (SELECT 1 FROM TASK_REQUIRED_SKILLS r WHERE r.TASK_ID = t.ID);

PROMPT
PROMPT === Nhiệm vụ theo nhóm phụ trách ===
SELECT NVL(tm.CODE, '(khong nhom)') AS NHOM, COUNT(*) AS SO_VIEC
FROM   TASKS t LEFT JOIN TEAMS tm ON tm.ID = t.TEAM_ID
GROUP  BY NVL(tm.CODE, '(khong nhom)') ORDER BY 1;

PROMPT
PROMPT === Chất lượng trung bình từng người (báo cáo đã duyệt) ===
SELECT u.ID, u.FULL_NAME,
       COUNT(*)                         AS SO_BAO_CAO,
       ROUND(AVG(r.QUALITY_SCORE), 2)   AS CHAT_LUONG,
       ROUND(AVG(r.COMPLETION_SCORE), 2) AS HOAN_THANH
FROM   TASK_REPORTS r
JOIN   TASKS t ON t.ID = r.TASK_ID
JOIN   USERS u ON u.ID = t.ASSIGNEE_ID
WHERE  r.REPORT_STATUS = 'DA_XAC_NHAN'
GROUP  BY u.ID, u.FULL_NAME
ORDER  BY 4 DESC, 1;

PROMPT
PROMPT === Báo cáo chờ duyệt không được có điểm (phải là 0) ===
SELECT COUNT(*) AS VI_PHAM FROM TASK_REPORTS
WHERE  REPORT_STATUS = 'CHO_XAC_NHAN'
  AND  (QUALITY_SCORE IS NOT NULL OR COMPLETION_SCORE IS NOT NULL);
