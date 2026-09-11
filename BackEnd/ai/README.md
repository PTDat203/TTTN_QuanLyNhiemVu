# Dịch vụ nhúng ngữ nghĩa — `BackEnd/ai/`

Dịch vụ Python nhỏ, chỉ làm một việc: **nhận văn bản, trả về véc-tơ**. ASP.NET Core gọi sang đây
để biết hai đoạn văn gần nghĩa nhau đến đâu — nội dung nhiệm vụ với hồ sơ nhân viên, với mô tả
phòng ban, với mô tả kỹ năng, với những nhiệm vụ đã làm.

Mọi logic nghiệp vụ — lọc ứng viên, công thức chấm điểm, lý do gợi ý — nằm ở phía .NET. Dịch vụ
này không biết nhiệm vụ hay nhân viên là gì.

**Backend vẫn chạy khi dịch vụ này tắt**: phần gợi ý tự lùi về TF-IDF và ghi rõ trong kết quả.

## Mô hình

`intfloat/multilingual-e5-small` — 118 triệu tham số, véc-tơ 384 chiều, nhận tối đa 512 token.
Dùng nguyên bản đã huấn luyện sẵn: **không tự huấn luyện, không fine-tune.**

## Cài đặt — một lần

```powershell
cd BackEnd\ai
py -m venv .venv
.venv\Scripts\python -m pip install -r requirements.txt
```

Lần chạy đầu tự tải mô hình về `BackEnd/ai/.cache/`, khoảng 470 MB. Thư mục này và `.venv/` đã
được `.gitignore` bỏ qua. Muốn xoá sạch thì xoá hai thư mục đó.

## Chạy

```powershell
cd BackEnd\ai
.venv\Scripts\python -m uvicorn app:app --port 8000
```

Thấy dòng `Đã nạp intfloat/multilingual-e5-small: 384 chiều` là sẵn sàng. Kiểm tra nhanh:

```powershell
curl http://localhost:8000/health
```

## API

`GET /health` → `{"ready": true, "model": "intfloat/multilingual-e5-small", "dim": 384}`

`POST /embed`

```json
{ "texts": ["Tối ưu truy vấn Oracle bị chậm"], "kind": "query" }
```

→ `{ "model": "...", "dim": 384, "vectors": [[0.0123, ...]], "ms": 21.4 }`

| `kind` | Dùng cho |
|---|---|
| `query` | Nội dung nhiệm vụ. Cũng dùng cho cả hai bên khi so nhiệm vụ với nhiệm vụ |
| `passage` | Hồ sơ nhân viên, mô tả phòng ban, nhóm, kỹ năng |

Dịch vụ tự thêm tiền tố `query: ` / `passage: ` mà E5 đòi hỏi. Quên tiền tố thì véc-tơ vẫn sinh
ra nhưng bị lệch — một lỗi âm thầm, không báo gì.

Véc-tơ trả về đã chuẩn hoá độ dài 1, nên độ tương đồng cosine chính là tích vô hướng.

## Vì sao không dùng `sentence-transformers`

Thư viện đó kéo theo scikit-learn, và trên máy phát triển Smart App Control của Windows 11 đã
chặn một tệp DLL của scikit-learn ở lần chạy đầu. Dịch vụ dùng thẳng `transformers` rồi tự lấy
trung bình véc-tơ token theo đúng hướng dẫn trên thẻ mô hình. Đã đối chiếu hai cách trên cùng câu
đầu vào: **lệch tối đa 5×10⁻⁸**, tức là trùng khớp.

## Lưu ý về thang điểm

Cosine của E5 dồn trong khoảng hẹp. Trên máy phát triển, nhiệm vụ "Tối ưu truy vấn Oracle bị
chậm" so với hồ sơ kỹ sư cơ sở dữ liệu được 0,842, so với hồ sơ kế toán vẫn được 0,799. Phía .NET
vì vậy không dùng thẳng con số thô mà quy về thang 0..1 bằng cặp ngưỡng hiệu chỉnh từ dữ liệu —
xem mục `GoiY` trong `appsettings.json`.
