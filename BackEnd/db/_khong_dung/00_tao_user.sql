--------------------------------------------------------------------------------
-- 00_tao_user.sql
-- Muc dich : Tao schema/user TASK_APP trong PDB XEPDB1 va cap du quyen cho ung dung.
-- Chay bang : tai khoan quan tri (SYS AS SYSDBA hoac SYSTEM).
-- Cach chay  : sqlplus / as sysdba   @00_tao_user.sql
--              hoac mo tep nay trong DBeaver bang ket noi quan tri roi chay ca script.
--
-- LUU Y ENCODING: tep nay luu o UTF-8. Neu chay bang SQL*Plus tren Windows,
--                 dat bien moi truong truoc khi chay:  set NLS_LANG=.AL32UTF8
--
-- CANH BAO: doi <DAT_MAT_KHAU_O_DAY> thanh mat khau that cua may ban.
--           KHONG commit mat khau that len Git.
--------------------------------------------------------------------------------

-- 1) Chuyen phien lam viec vao dung PDB.
--    Oracle 21c XE co CDB (XE) va PDB (XEPDB1). User ung dung PHAI nam trong PDB,
--    neu tao nham trong CDB thi Oracle bat buoc ten user phai bat dau bang C##.
ALTER SESSION SET CONTAINER = XEPDB1;

-- 2) Tao user TASK_APP.
--    Boc trong khoi PL/SQL de chay lai nhieu lan khong bao loi:
--      ORA-01920 = user name conflicts with another user or role name (user da ton tai).
DECLARE
    v_mat_khau CONSTANT VARCHAR2(100) := '<DAT_MAT_KHAU_O_DAY>';
BEGIN
    EXECUTE IMMEDIATE 'CREATE USER TASK_APP IDENTIFIED BY "' || v_mat_khau || '"';
    DBMS_OUTPUT.PUT_LINE('Da tao user TASK_APP.');
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE = -1920 THEN
            DBMS_OUTPUT.PUT_LINE('User TASK_APP da ton tai - bo qua buoc CREATE USER.');
        ELSE
            RAISE;
        END IF;
END;
/

-- 3) Cap quyen he thong toi thieu ma ung dung can.
--    Khong cap DBA, khong cap RESOURCE (RESOURCE tu 12c khong con kem quota tablespace).
GRANT CREATE SESSION   TO TASK_APP;   -- dang nhap
GRANT CREATE TABLE     TO TASK_APP;   -- tao 7 bang nghiep vu
GRANT CREATE SEQUENCE  TO TASK_APP;   -- cot IDENTITY sinh sequence ngam ben duoi
GRANT CREATE VIEW      TO TASK_APP;   -- du phong cho view bao cao
GRANT CREATE PROCEDURE TO TASK_APP;   -- du phong cho package/procedure

-- 4) BAT BUOC: cap quota tablespace.
--    Thieu buoc nay thi CREATE TABLE van chay duoc nhung INSERT dau tien se bao
--    ORA-01950: no privileges on tablespace 'USERS'.
ALTER USER TASK_APP QUOTA UNLIMITED ON USERS;

-- 5) Mo khoa tai khoan phong truong hop bi khoa do go sai mat khau nhieu lan (ORA-28000).
ALTER USER TASK_APP ACCOUNT UNLOCK;

-- 6) Kiem tra bo ky tu cua database.
--    Can NLS_CHARACTERSET = AL32UTF8 thi tieng Viet co dau moi luu dung.
SELECT PARAMETER, VALUE
FROM   NLS_DATABASE_PARAMETERS
WHERE  PARAMETER IN ('NLS_CHARACTERSET', 'NLS_NCHAR_CHARACTERSET');

-- 7) Kiem tra user vua tao.
SELECT USERNAME, ACCOUNT_STATUS, DEFAULT_TABLESPACE
FROM   DBA_USERS
WHERE  USERNAME = 'TASK_APP';

--------------------------------------------------------------------------------
-- Sau khi chay xong tep nay, dang nhap lai bang chinh TASK_APP roi chay tiep:
--   01_tao_bang.sql
--   02_seed_danh_muc.sql
--   03_seed_du_lieu_mau.sql
--
-- Lenh dang nhap kiem chung:
--   sqlplus TASK_APP/<mat_khau>@127.0.0.1:1521/XEPDB1
--------------------------------------------------------------------------------
