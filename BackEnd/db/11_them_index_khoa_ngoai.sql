--------------------------------------------------------------------------------
-- TRẠNG THÁI: ĐÃ ÁP DỤNG XONG. GIỮ LẠI CHỈ ĐỂ THAM KHẢO — KHÔNG CẦN CHẠY LẠI.
--
-- Người dùng đã tự tạo index cho cả 12 cột khóa ngoại, với tiền tố IDX_ thay vì
-- IX_ như script này đặt. Đã kiểm chứng bằng DBA_IND_COLUMNS: không còn cột khóa
-- ngoại nào thiếu index.
--
-- Index thật đang có trong DB (tên khác script này):
--   IDX_USER_SKILLS_USER_ID          IDX_TASKS_CREATOR_ID
--   IDX_TASKS_ASSIGNEE_ID            IDX_TASKS_STATUS_CODE
--   IDX_TASK_PROGRESS_TASK_ID        IDX_TASK_PROGRESS_USER_ID
--   IDX_TASK_REPORTS_TASK_ID         IDX_TASK_REPORTS_REPORTER_ID
--   IDX_TASK_REPORTS_REVIEWER_ID     IDX_TASK_ATTACHMENTS_TASK_ID
--   IDX_TASK_ATTACHMENTS_REPORT_ID   IDX_TASK_ATTACHMENTS_UPLOADED_BY
--
-- KHÔNG chạy lại: tên index trong script khác tên thật, chạy sẽ tạo index TRÙNG
-- CỘT. Oracle chặn bằng ORA-01408 và script đã bắt mã lỗi đó, nên không hỏng gì —
-- nhưng vẫn là thao tác thừa, không có lý do để chạy.
--------------------------------------------------------------------------------

-- =====================================================================
-- 11_them_index_khoa_ngoai.sql
-- ĐỀ XUẤT THÊM — chạy trong PDB XEPDB1.
-- =====================================================================
--
-- VẤN ĐỀ
-- Oracle tự tạo index cho khóa chính và ràng buộc UNIQUE, nhưng KHÔNG tự
-- tạo index cho cột khóa ngoại. Schema hiện có 12 khóa ngoại, không cột nào
-- được đánh index. Hai hệ quả:
--
--   1. Truy vấn chậm. Mọi màn hình đều lọc theo các cột này:
--      "nhiệm vụ của tôi"      -> TASKS.ASSIGNEE_ID
--      "nhiệm vụ tôi tạo"      -> TASKS.CREATOR_ID
--      "lọc theo trạng thái"   -> TASKS.STATUS_CODE
--      "lịch sử tiến độ"       -> TASK_PROGRESS.TASK_ID
--      "báo cáo của nhiệm vụ"  -> TASK_REPORTS.TASK_ID
--
--   2. Khóa bảng khi xoá/sửa bảng cha. Khóa ngoại không có index khiến Oracle
--      phải khoá toàn bảng con khi cập nhật khóa chính bảng cha.
--
-- Chạy lại nhiều lần an toàn: bọc trong khối bắt ORA-00955 (đã tồn tại).
-- =====================================================================

ALTER SESSION SET CONTAINER = XEPDB1;

DECLARE
    PROCEDURE tao_index(p_lenh VARCHAR2) IS
    BEGIN
        EXECUTE IMMEDIATE p_lenh;
    EXCEPTION
        WHEN OTHERS THEN
            -- ORA-00955: tên đã được một đối tượng đang có sử dụng
            -- ORA-01408: danh sách cột này đã được đánh index
            IF SQLCODE NOT IN (-955, -1408) THEN
                RAISE;
            END IF;
    END;
BEGIN
    -- TASKS: ba khóa ngoại, đều là cột lọc chính của ứng dụng
    tao_index('CREATE INDEX TASK_APP.IX_TASKS_ASSIGNEE  ON TASK_APP.TASKS (ASSIGNEE_ID)');
    tao_index('CREATE INDEX TASK_APP.IX_TASKS_CREATOR   ON TASK_APP.TASKS (CREATOR_ID)');
    tao_index('CREATE INDEX TASK_APP.IX_TASKS_STATUS    ON TASK_APP.TASKS (STATUS_CODE)');
    -- Lọc "việc sắp đến hạn" và sắp xếp theo hạn
    tao_index('CREATE INDEX TASK_APP.IX_TASKS_DUE_DATE  ON TASK_APP.TASKS (DUE_DATE)');
    -- Màn "nhiệm vụ của tôi" lọc đồng thời người thực hiện + trạng thái
    tao_index('CREATE INDEX TASK_APP.IX_TASKS_ASG_STT   ON TASK_APP.TASKS (ASSIGNEE_ID, STATUS_CODE)');

    -- USER_SKILLS: AI gợi ý luôn quét theo người dùng
    tao_index('CREATE INDEX TASK_APP.IX_SKILLS_USER     ON TASK_APP.USER_SKILLS (USER_ID)');

    -- TASK_PROGRESS
    tao_index('CREATE INDEX TASK_APP.IX_PROGRESS_TASK   ON TASK_APP.TASK_PROGRESS (TASK_ID)');
    tao_index('CREATE INDEX TASK_APP.IX_PROGRESS_USER   ON TASK_APP.TASK_PROGRESS (USER_ID)');

    -- TASK_REPORTS
    tao_index('CREATE INDEX TASK_APP.IX_REPORTS_TASK    ON TASK_APP.TASK_REPORTS (TASK_ID)');
    tao_index('CREATE INDEX TASK_APP.IX_REPORTS_REPTER  ON TASK_APP.TASK_REPORTS (REPORTER_ID)');
    tao_index('CREATE INDEX TASK_APP.IX_REPORTS_REVWER  ON TASK_APP.TASK_REPORTS (REVIEWER_ID)');

    -- TASK_ATTACHMENTS
    tao_index('CREATE INDEX TASK_APP.IX_ATTACH_TASK     ON TASK_APP.TASK_ATTACHMENTS (TASK_ID)');
    tao_index('CREATE INDEX TASK_APP.IX_ATTACH_REPORT   ON TASK_APP.TASK_ATTACHMENTS (REPORT_ID)');
    tao_index('CREATE INDEX TASK_APP.IX_ATTACH_USER     ON TASK_APP.TASK_ATTACHMENTS (UPLOADED_BY)');
END;
/

-- =====================================================================
-- KIỂM CHỨNG
-- =====================================================================
SELECT INDEX_NAME, TABLE_NAME
FROM   ALL_INDEXES
WHERE  OWNER = 'TASK_APP' AND INDEX_NAME LIKE 'IX\_%' ESCAPE '\'
ORDER  BY TABLE_NAME, INDEX_NAME;
