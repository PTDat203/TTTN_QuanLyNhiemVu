import { HttpClient, HttpEvent, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { GIOI_HAN_TEP, Guid, TepDinhKem } from '../models';
import { duongDan } from './api.util';

/**
 * §5.7 G1-G3 - tep dinh kem.
 *
 * Luong dung: upload TRUOC (G1) de lay `id`, sau do gui mang `fileIds` kem
 * body cua C1 / C5 / D2 / D3 / D4 / F1. Backend gan tep vao ban ghi tuong ung.
 */
@Injectable({ providedIn: 'root' })
export class FileService {
  constructor(private readonly http: HttpClient) {}

  /**
   * G1 - POST /api/v1/files (multipart).
   * Whitelist: pdf, doc(x), xls(x), png, jpg. Toi da 20MB moi tep.
   * Ten truong form BAT BUOC la `files` (khop `[FromForm] List<IFormFile> files`).
   */
  taiLen(dsTep: File[]): Observable<TepDinhKem[]> {
    const form = new FormData();
    for (const tep of dsTep) form.append('files', tep, tep.name);
    return this.http.post<TepDinhKem[]>(duongDan('/files'), form);
  }

  /** G1 - ban co theo doi tien do (dung cho thanh progress khi tep lon). */
  taiLenCoTienDo(dsTep: File[]): Observable<HttpEvent<TepDinhKem[]>> {
    const form = new FormData();
    for (const tep of dsTep) form.append('files', tep, tep.name);

    const yeuCau = new HttpRequest<FormData>('POST', duongDan('/files'), form, {
      reportProgress: true
    });
    return this.http.request<TepDinhKem[]>(yeuCau);
  }

  /**
   * G2 - GET /api/v1/files/{id} - tai xuong.
   * Tra `Blob` de man hinh tu tao link tai (yeu cau header Authorization nen
   * KHONG dung the `<a href>` truc tiep duoc).
   */
  taiXuong(id: Guid): Observable<Blob> {
    return this.http.get(duongDan(`/files/${id}`), { responseType: 'blob' });
  }

  /** G3 - DELETE /api/v1/files/{id} - chi nguoi upload, khi ban ghi chua khoa. */
  xoa(id: Guid): Observable<void> {
    return this.http.delete<void>(duongDan(`/files/${id}`));
  }

  /* ------------------------------------------------------------ */
  /* Kiem tra phia FE truoc khi upload (BE van kiem lai)            */
  /* ------------------------------------------------------------ */

  /** Tra ve thong bao loi tieng Viet, hoac null neu tep hop le. */
  kiemTraTep(tep: File): string | null {
    if (tep.size > GIOI_HAN_TEP.kichThuocToiDa) {
      return `Tệp "${tep.name}" vượt quá 20MB.`;
    }
    const cham = tep.name.lastIndexOf('.');
    const duoi = cham >= 0 ? tep.name.substring(cham).toLowerCase() : '';
    if (!GIOI_HAN_TEP.duoiChoPhep.includes(duoi)) {
      return `Tệp "${tep.name}" có định dạng không được phép. Chỉ nhận: ${GIOI_HAN_TEP.duoiChoPhep.join(', ')}.`;
    }
    return null;
  }

  /** Dinh dang kich thuoc tep de hien thi (vi du "1,2 MB"). */
  static dinhDangKichThuoc(soByte: number): string {
    if (soByte < 1024) return `${soByte} B`;
    if (soByte < 1024 * 1024) return `${(soByte / 1024).toFixed(1).replace('.', ',')} KB`;
    return `${(soByte / (1024 * 1024)).toFixed(1).replace('.', ',')} MB`;
  }

  /** Kich hoat trinh duyet luu Blob vua tai ve. */
  static luuBlob(noiDung: Blob, tenTep: string): void {
    const url = URL.createObjectURL(noiDung);
    const a = document.createElement('a');
    a.href = url;
    a.download = tenTep;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }
}
