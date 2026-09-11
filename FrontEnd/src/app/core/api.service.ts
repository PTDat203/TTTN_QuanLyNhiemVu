import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import {
  BaoCao, DangNhapRequest, DangNhapResponse, GoiYResponse, KetQuaPhanTrang,
  NguoiDung, NhiemVuChiTiet, NhiemVuLoc, NhiemVuTomTat, TaoNhiemVuRequest, TienDo,
  VAI_TRO_GIAO_VIEC,
} from './models';

/** Địa chỉ BackEnd. Khi chạy `ng serve`, proxy.conf.json chuyển tiếp /api sang cổng 5080. */
const API = '/api';

const KHOA_TOKEN = 'taskapp.token';
const KHOA_REFRESH = 'taskapp.refresh';
const KHOA_NGUOI_DUNG = 'taskapp.user';

/**
 * Quản lý phiên đăng nhập.
 *
 * Token lưu trong localStorage để tải lại trang không bị đăng xuất.
 * Đây là đánh đổi quen thuộc: tiện cho người dùng nhưng token đọc được bằng JavaScript
 * nên dính XSS là mất. Bản chạy thật nên dùng cookie HttpOnly.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  /** Người đang đăng nhập, null nếu chưa. Dùng signal để giao diện tự cập nhật. */
  readonly nguoiDung = signal<NguoiDung | null>(this.docNguoiDungDaLuu());

  readonly daDangNhap = computed(() => this.nguoiDung() !== null);

  /**
   * Được giao việc cho người khác: Giám đốc, trưởng phòng, trưởng nhóm.
   *
   * Chỉ để che bớt giao diện. Quyền thật — kể cả phạm vi phòng nào, nhóm nào — do backend
   * quyết định; frontend không biết và không cần biết quy tắc phạm vi.
   */
  readonly coTheGiaoViec = computed(() => {
    const vaiTro = this.nguoiDung()?.role;
    return !!vaiTro && VAI_TRO_GIAO_VIEC.includes(vaiTro);
  });

  get token(): string | null {
    return localStorage.getItem(KHOA_TOKEN);
  }

  dangNhap(yeuCau: DangNhapRequest): Observable<DangNhapResponse> {
    return this.http.post<DangNhapResponse>(`${API}/auth/login`, yeuCau).pipe(
      tap((kq) => {
        localStorage.setItem(KHOA_TOKEN, kq.accessToken);
        localStorage.setItem(KHOA_REFRESH, kq.refreshToken);
        localStorage.setItem(KHOA_NGUOI_DUNG, JSON.stringify(kq.nguoiDung));
        this.nguoiDung.set(kq.nguoiDung);
      })
    );
  }

  dangXuat(): void {
    const refresh = localStorage.getItem(KHOA_REFRESH);

    // Gọi API để thu hồi refresh token phía server. Không chờ kết quả: dù server có lỗi
    // thì phiên phía trình duyệt vẫn phải bị xoá.
    if (refresh) {
      this.http.post(`${API}/auth/logout`, { refreshToken: refresh }).subscribe({
        error: () => undefined,
      });
    }

    localStorage.removeItem(KHOA_TOKEN);
    localStorage.removeItem(KHOA_REFRESH);
    localStorage.removeItem(KHOA_NGUOI_DUNG);
    this.nguoiDung.set(null);
    this.router.navigate(['/dang-nhap']);
  }

  private docNguoiDungDaLuu(): NguoiDung | null {
    try {
      const s = localStorage.getItem(KHOA_NGUOI_DUNG);
      return s ? (JSON.parse(s) as NguoiDung) : null;
    } catch {
      // localStorage hỏng hoặc JSON sai định dạng thì coi như chưa đăng nhập,
      // không để một bản ghi lỗi làm chết cả ứng dụng lúc khởi động.
      return null;
    }
  }
}

/** Gọi các endpoint nhiệm vụ. */
@Injectable({ providedIn: 'root' })
export class NhiemVuService {
  private http = inject(HttpClient);

  danhSach(loc: NhiemVuLoc): Observable<KetQuaPhanTrang<NhiemVuTomTat>> {
    let p = new HttpParams();
    for (const [khoa, giaTri] of Object.entries(loc)) {
      // Bỏ qua giá trị rỗng để URL không đầy tham số vô nghĩa,
      // và để backend hiểu là "không lọc theo tiêu chí này".
      if (giaTri !== undefined && giaTri !== null && giaTri !== '') {
        p = p.set(khoa, String(giaTri));
      }
    }
    return this.http.get<KetQuaPhanTrang<NhiemVuTomTat>>(`${API}/nhiem-vu`, { params: p });
  }

  chiTiet(id: number): Observable<NhiemVuChiTiet> {
    return this.http.get<NhiemVuChiTiet>(`${API}/nhiem-vu/${id}`);
  }

  tao(yeuCau: TaoNhiemVuRequest): Observable<NhiemVuChiTiet> {
    return this.http.post<NhiemVuChiTiet>(`${API}/nhiem-vu`, yeuCau);
  }

  sua(id: number, yeuCau: TaoNhiemVuRequest): Observable<NhiemVuChiTiet> {
    return this.http.put<NhiemVuChiTiet>(`${API}/nhiem-vu/${id}`, yeuCau);
  }

  xoa(id: number): Observable<void> {
    return this.http.delete<void>(`${API}/nhiem-vu/${id}`);
  }

  giao(id: number, assigneeId: number): Observable<NhiemVuChiTiet> {
    return this.http.post<NhiemVuChiTiet>(`${API}/nhiem-vu/${id}/giao`, { assigneeId });
  }

  tiepNhan(id: number): Observable<NhiemVuChiTiet> {
    return this.http.post<NhiemVuChiTiet>(`${API}/nhiem-vu/${id}/tiep-nhan`, {});
  }

  capNhatTienDo(id: number, progressPercent: number, content?: string): Observable<TienDo> {
    return this.http.post<TienDo>(`${API}/nhiem-vu/${id}/tien-do`, { progressPercent, content });
  }

  guiBaoCao(id: number, content: string): Observable<BaoCao> {
    return this.http.post<BaoCao>(`${API}/nhiem-vu/${id}/bao-cao`, { content });
  }

  /**
   * Duyệt báo cáo. Điểm chất lượng và mức hoàn thành (1..5) là dữ liệu mà AI dùng để chấm
   * hiệu suất lịch sử — bỏ trống thì nhiệm vụ này không góp vào điểm đó.
   */
  duyetBaoCao(
    baoCaoId: number,
    xacNhan: boolean,
    reviewNote?: string,
    qualityScore?: number | null,
    completionScore?: number | null,
  ): Observable<BaoCao> {
    return this.http.post<BaoCao>(`${API}/bao-cao/${baoCaoId}/duyet`, {
      xacNhan,
      reviewNote,
      qualityScore,
      completionScore,
    });
  }

  choToiDuyet(): Observable<BaoCao[]> {
    return this.http.get<BaoCao[]>(`${API}/bao-cao/cho-toi-duyet`);
  }
}

/** Gọi endpoint AI gợi ý người thực hiện. */
@Injectable({ providedIn: 'root' })
export class GoiYService {
  private http = inject(HttpClient);

  /** Gợi ý theo nhiệm vụ đã lưu, hoặc theo nội dung đang gõ. */
  goiY(yeuCau: {
    taskId?: number;
    title?: string;
    description?: string;
    soLuong?: number;
  }): Observable<GoiYResponse> {
    return this.http.post<GoiYResponse>(`${API}/goi-y/nguoi-thuc-hien`, yeuCau);
  }
}
