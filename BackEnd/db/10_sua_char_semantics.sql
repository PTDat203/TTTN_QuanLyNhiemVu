--------------------------------------------------------------------------------
-- TRẠNG THÁI: ĐÃ ÁP DỤNG XONG. GIỮ LẠI CHỈ ĐỂ THAM KHẢO — KHÔNG CẦN CHẠY LẠI.
--
-- Người dùng đã tự chạy phần này trực tiếp trên Oracle. Đã kiểm chứng lại bằng
-- truy vấn ALL_TAB_COLUMNS: toàn bộ 19 cột VARCHAR2 của schema TASK_APP hiện đều
-- dùng ngữ nghĩa CHAR (CHAR_USED = 'C'), rộng hơn phạm vi mà script này đề xuất.
--
-- Chạy lại vẫn vô hại (ALTER về đúng trạng thái đang có), nhưng không còn cần thiết.
--------------------------------------------------------------------------------

-- =====================================================================
-- 10_sua_char_semantics.sql
-- ĐỀ XUẤT SỬA — đọc kỹ trước khi chạy. Chạy trong PDB XEPDB1, user TASK_APP.
-- =====================================================================
--
-- VẤN ĐỀ
-- Bộ ký tự database là AL32UTF8, nhưng mọi cột VARCHAR2 đang khai theo
-- ngữ nghĩa BYTE (mặc định của Oracle). Một ký tự tiếng Việt có dấu chiếm
-- tới 3 byte, nên sức chứa thật nhỏ hơn con số ghi trong cột rất nhiều:
--
--   TASKS.TITLE            VARCHAR2(200 BYTE)  -> chỉ ~66 ký tự tiếng Việt
--   TASK_PROGRESS.CONTENT  VARCHAR2(2000 BYTE) -> chỉ ~666 ký tự
--   USER_SKILLS.DESCRIPTION VARCHAR2(500 BYTE) -> chỉ ~166 ký tự
--   TASK_ATTACHMENTS.FILE_NAME VARCHAR2(255 BYTE) -> chỉ ~85 ký tự
--   USERS.FULL_NAME        VARCHAR2(100 BYTE)  -> chỉ ~33 ký tự
--
-- Vượt quá sẽ báo ORA-12899: value too large for column.
-- Tiêu đề nhiệm vụ tiếng Việt vượt 66 ký tự là chuyện rất bình thường.
--
-- Vấn đề thứ hai: phía C#, EF Core dùng HasMaxLength(200) nghĩa là 200 KÝ TỰ.
-- Nếu cột vẫn là BYTE thì EF cho nhập 200 ký tự rồi Oracle mới từ chối —
-- lỗi chỉ lộ ra lúc chạy, không lộ lúc build.
--
-- CÁCH SỬA
-- Đổi sang ngữ nghĩa CHAR cho các cột chứa văn bản tiếng Việt tự do.
-- Đây là thao tác NỚI RỘNG sức chứa:
--   - Không mất dữ liệu, không DROP gì.
--   - Các bảng hiện đang rỗng (trừ TASK_STATUS_LOOKUP có 6 dòng), nên rủi ro bằng 0.
--   - Chạy lại nhiều lần vẫn an toàn.
--
-- KHÔNG đổi các cột chỉ chứa mã ASCII: USERNAME, PASSWORD_HASH, EMAIL,
-- USER_ROLE, USER_STATUS, PRIORITY, STATUS_CODE, REPORT_STATUS, CODE, FILE_TYPE.
-- Giữ nguyên để không đụng vào cặp khóa chính / khóa ngoại CODE <- STATUS_CODE.
-- =====================================================================

ALTER SESSION SET CONTAINER = XEPDB1;

-- USERS -----------------------------------------------------------------
ALTER TABLE TASK_APP.USERS              MODIFY (FULL_NAME     VARCHAR2(100 CHAR));

-- USER_SKILLS -----------------------------------------------------------
ALTER TABLE TASK_APP.USER_SKILLS        MODIFY (SKILL_NAME    VARCHAR2(100 CHAR));
ALTER TABLE TASK_APP.USER_SKILLS        MODIFY (DESCRIPTION   VARCHAR2(500 CHAR));

-- TASK_STATUS_LOOKUP ----------------------------------------------------
ALTER TABLE TASK_APP.TASK_STATUS_LOOKUP MODIFY (NAME          VARCHAR2(100 CHAR));
ALTER TABLE TASK_APP.TASK_STATUS_LOOKUP MODIFY (DESCRIPTION   VARCHAR2(255 CHAR));

-- TASKS -----------------------------------------------------------------
ALTER TABLE TASK_APP.TASKS              MODIFY (TITLE         VARCHAR2(200 CHAR));

-- TASK_PROGRESS ---------------------------------------------------------
ALTER TABLE TASK_APP.TASK_PROGRESS      MODIFY (CONTENT       VARCHAR2(2000 CHAR));

-- TASK_ATTACHMENTS ------------------------------------------------------
ALTER TABLE TASK_APP.TASK_ATTACHMENTS   MODIFY (FILE_NAME     VARCHAR2(255 CHAR));
ALTER TABLE TASK_APP.TASK_ATTACHMENTS   MODIFY (FILE_PATH     VARCHAR2(500 CHAR));

COMMIT;

-- =====================================================================
-- KIỂM CHỨNG — chạy sau khi ALTER, cột CHAR_USED phải là 'C'
-- =====================================================================
SELECT TABLE_NAME, COLUMN_NAME, DATA_LENGTH, CHAR_USED
FROM   ALL_TAB_COLUMNS
WHERE  OWNER = 'TASK_APP'
  AND  COLUMN_NAME IN ('FULL_NAME','SKILL_NAME','DESCRIPTION','NAME',
                       'TITLE','CONTENT','FILE_NAME','FILE_PATH')
  AND  DATA_TYPE = 'VARCHAR2'
ORDER  BY TABLE_NAME, COLUMN_NAME;

-- Thử chèn một tiêu đề tiếng Việt dài, phải chạy được sau khi sửa.
-- (Chạy tay khi đã có ít nhất 1 người dùng để thoả khóa ngoại CREATOR_ID.)
--
-- INSERT INTO TASK_APP.TASKS (TITLE, CREATOR_ID, PRIORITY, STATUS_CODE)
-- VALUES ('Rà soát, đánh giá và báo cáo tình hình triển khai hệ thống quản lý '
--      || 'nhiệm vụ tại các đơn vị trực thuộc trong quý III năm 2026',
--         1, 'MEDIUM', 'MOI_TAO');
-- ROLLBACK;
