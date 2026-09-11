import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService, NhiemVuService } from '../core/api.service';
import { MAU_TRANG_THAI, NguoiDung, NhiemVuChiTiet } from '../core/models';

@Component({
  selector: 'app-chi-tiet',
  standalone: true,
  imports: [FormsModule, DatePipe],
  template: `
    @if (dangTai()) {
      <p class="trong">Đang tải…</p>
    } @else if (!nv()) {
      <p class="trong">Không tìm thấy nhiệm vụ.</p>
    } @else {
      <div class="dau-trang">
        <div>
          <span class="the-trang-thai" [style.background]="mau()">{{ nv()!.tenTrangThai }}</span>
          <h1>{{ nv()!.title }}</h1>
        </div>
        <button class="phu-nhat" (click)="quayLai()">← Quay lại</button>
      </div>

      @if (loi()) {
        <div class="bao-loi">{{ loi() }}</div>
      }
      @if (thanhCong()) {
        <div class="bao-ok">{{ thanhCong() }}</div>
      }

      <div class="hai-cot">
        <div class="trai">
          <section class="the">
            <h2>Thông tin</h2>
            @if (nv()!.description) {
              <p class="mo-ta">{{ nv()!.description }}</p>
            }
            <dl>
              <dt>Người giao</dt><dd>{{ nv()!.tenNguoiTao }}</dd>
              <dt>Người thực hiện</dt>
              <dd>{{ nv()!.tenNguoiThucHien ?? '— chưa giao' }}</dd>
              <dt>Phòng thực thi</dt>
              <dd>
                {{ nv()!.tenPhongBan ?? '— chưa xác định' }}{{ nv()!.tenNhom ? ' · ' + nv()!.tenNhom : '' }}
              </dd>
              <dt>Mức ưu tiên</dt><dd>{{ nv()!.tenUuTien }}</dd>
              <dt>Hạn hoàn thành</dt>
              <dd [class.qua-han]="nv()!.quaHan">
                {{ nv()!.dueDate ? (nv()!.dueDate | date: 'dd/MM/yyyy') : '— chưa đặt' }}
              </dd>
            </dl>
          </section>

          <!-- ------------------------------ hành động theo trạng thái -->
          <section class="the">
            <h2>Hành động</h2>

            @if (coTheGiao()) {
              <div class="hanh-dong">
                <select [(ngModel)]="nguoiNhanId">
                  <option [ngValue]="null">— Chọn người thực hiện —</option>
                  @for (n of nhanVien(); track n.id) {
                    <option [ngValue]="n.id">
                      {{ n.fullName }}{{ n.jobTitle ? ' — ' + n.jobTitle : '' }}
                    </option>
                  }
                </select>
                <button (click)="giao()" [disabled]="!nguoiNhanId">Giao nhiệm vụ</button>
              </div>
            }

            @if (coTheTiepNhan()) {
              <button class="nut-chinh" (click)="tiepNhan()">Tiếp nhận nhiệm vụ</button>
            }

            @if (coTheCapNhatTienDo()) {
              <div class="hanh-dong doc">
                <label>
                  Tiến độ: {{ phanTram }}%
                  <input type="range" min="0" max="100" [(ngModel)]="phanTram" />
                </label>
                <textarea
                  [(ngModel)]="noiDungTienDo"
                  rows="2"
                  placeholder="Mô tả công việc đã làm…"
                ></textarea>
                <button (click)="capNhatTienDo()">Cập nhật tiến độ</button>
              </div>
            }

            @if (coTheBaoCao()) {
              <div class="hanh-dong doc">
                <textarea
                  [(ngModel)]="noiDungBaoCao"
                  rows="3"
                  placeholder="Nội dung báo cáo kết quả…"
                ></textarea>
                <button class="nut-chinh" (click)="guiBaoCao()">Gửi báo cáo kết quả</button>
              </div>
            }

            @if (coTheDuyet()) {
              <div class="hanh-dong doc">
                <p class="mo">Có báo cáo đang chờ bạn duyệt.</p>
                <textarea
                  [(ngModel)]="yKien"
                  rows="2"
                  placeholder="Ý kiến (bắt buộc khi từ chối)…"
                ></textarea>
                <div class="hang cham-diem">
                  <label>
                    Chất lượng
                    <select [(ngModel)]="diemChatLuong">
                      <option [ngValue]="null">— chấm —</option>
                      @for (d of thangDiem; track d) {
                        <option [ngValue]="d">{{ d }} / 5</option>
                      }
                    </select>
                  </label>
                  <label>
                    Mức hoàn thành
                    <select [(ngModel)]="diemHoanThanh">
                      <option [ngValue]="null">— chấm —</option>
                      @for (d of thangDiem; track d) {
                        <option [ngValue]="d">{{ d }} / 5</option>
                      }
                    </select>
                  </label>
                </div>
                <small class="mo">
                  Điểm chất lượng là dữ liệu AI dùng để đánh giá hiệu suất người thực hiện.
                </small>
                <div class="hang">
                  <button class="nut-dat" (click)="duyet(true)">✓ Xác nhận hoàn thành</button>
                  <button class="nut-tu-choi" (click)="duyet(false)">✕ Yêu cầu bổ sung</button>
                </div>
              </div>
            }

            @if (!coTheGiao() && !coTheTiepNhan() && !coTheCapNhatTienDo()
                 && !coTheBaoCao() && !coTheDuyet()) {
              <p class="mo">
                @if (nv()!.statusCode === 'HOAN_THANH') {
                  Nhiệm vụ đã hoàn thành.
                } @else {
                  Hiện không có hành động nào dành cho bạn ở trạng thái này.
                }
              </p>
            }
          </section>
        </div>

        <div class="phai">
          <section class="the">
            <h2>Lịch sử tiến độ ({{ nv()!.lichSuTienDo.length }})</h2>
            @if (nv()!.lichSuTienDo.length === 0) {
              <p class="mo">Chưa có cập nhật nào.</p>
            } @else {
              @for (td of nv()!.lichSuTienDo; track td.id) {
                <div class="muc">
                  <div class="hang-muc">
                    <strong>{{ td.progressPercent }}%</strong>
                    <span class="mo">{{ td.tenNguoiCapNhat }}</span>
                    <span class="mo nho">{{ td.createdAt | date: 'dd/MM HH:mm' }}</span>
                  </div>
                  @if (td.content) {
                    <p>{{ td.content }}</p>
                  }
                </div>
              }
            }
          </section>

          <section class="the">
            <h2>Báo cáo ({{ nv()!.danhSachBaoCao.length }})</h2>
            @if (nv()!.danhSachBaoCao.length === 0) {
              <p class="mo">Chưa có báo cáo nào.</p>
            } @else {
              @for (bc of nv()!.danhSachBaoCao; track bc.id) {
                <div class="muc">
                  <div class="hang-muc">
                    <span class="nhan" [class.dat]="bc.reportStatus === 'DA_XAC_NHAN'"
                          [class.tu-choi]="bc.reportStatus === 'TU_CHOI'">
                      {{ bc.tenTrangThaiBaoCao }}
                    </span>
                    <span class="mo nho">{{ bc.createdAt | date: 'dd/MM HH:mm' }}</span>
                  </div>
                  <p>{{ bc.content }}</p>
                  @if (bc.qualityScore || bc.completionScore) {
                    <p class="diem-bc">
                      Chất lượng {{ bc.qualityScore ?? '—' }}/5 · Mức hoàn thành {{ bc.completionScore ?? '—' }}/5
                    </p>
                  }
                  @if (bc.reviewNote) {
                    <p class="y-kien">
                      <strong>{{ bc.tenNguoiDuyet }}:</strong> {{ bc.reviewNote }}
                    </p>
                  }
                </div>
              }
            }
          </section>
        </div>
      </div>
    }
  `,
  styles: [
    `
      .dau-trang {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        margin-bottom: 16px;
        gap: 16px;
      }
      h1 {
        margin: 8px 0 0;
        font-size: 21px;
      }
      h2 {
        margin: 0 0 10px;
        font-size: 15px;
        color: #334155;
      }
      .the-trang-thai {
        color: #fff;
        padding: 3px 10px;
        border-radius: 20px;
        font-size: 12px;
      }
      .hai-cot {
        display: grid;
        grid-template-columns: 1fr 400px;
        gap: 16px;
        align-items: start;
      }
      @media (max-width: 980px) {
        .hai-cot {
          grid-template-columns: 1fr;
        }
      }
      .trai,
      .phai {
        display: grid;
        gap: 16px;
      }
      .the {
        background: #fff;
        padding: 18px;
        border-radius: 10px;
        box-shadow: 0 1px 3px rgb(0 0 0 / 8%);
      }
      .mo-ta {
        margin: 0 0 14px;
        color: #334155;
        line-height: 1.6;
        white-space: pre-wrap;
      }
      dl {
        display: grid;
        grid-template-columns: 130px 1fr;
        gap: 8px 12px;
        margin: 0;
        font-size: 14px;
      }
      dt {
        color: #64748b;
      }
      dd {
        margin: 0;
        color: #0f172a;
      }
      .qua-han {
        color: #dc2626;
        font-weight: 500;
      }
      .hanh-dong {
        display: flex;
        gap: 8px;
        align-items: center;
      }
      .hanh-dong.doc {
        display: grid;
        gap: 8px;
      }
      .hang {
        display: flex;
        gap: 8px;
      }
      .hang button {
        flex: 1;
      }
      select,
      textarea,
      input[type='range'] {
        padding: 8px 10px;
        border: 1px solid #cbd5e1;
        border-radius: 7px;
        font-size: 14px;
        font-family: inherit;
        width: 100%;
      }
      input[type='range'] {
        padding: 0;
      }
      label {
        display: grid;
        gap: 6px;
        font-size: 14px;
        color: #334155;
      }
      button {
        padding: 9px 14px;
        border: 1px solid #cbd5e1;
        background: #fff;
        border-radius: 7px;
        cursor: pointer;
        font-size: 14px;
      }
      .nut-chinh {
        background: #2563eb;
        color: #fff;
        border-color: #2563eb;
      }
      .nut-dat {
        background: #16a34a;
        color: #fff;
        border-color: #16a34a;
      }
      .nut-tu-choi {
        background: #fff;
        color: #dc2626;
        border-color: #dc2626;
      }
      .phu-nhat {
        white-space: nowrap;
      }
      button:disabled {
        opacity: 0.5;
        cursor: default;
      }
      .muc {
        border-top: 1px solid #f1f5f9;
        padding: 10px 0;
      }
      .muc:first-of-type {
        border-top: 0;
      }
      .hang-muc {
        display: flex;
        gap: 8px;
        align-items: baseline;
        flex-wrap: wrap;
      }
      .muc p {
        margin: 5px 0 0;
        font-size: 13.5px;
        color: #334155;
        line-height: 1.55;
      }
      .cham-diem label {
        flex: 1;
      }
      .diem-bc {
        font-size: 12.5px !important;
        color: #0f172a !important;
        font-weight: 500;
      }
      .y-kien {
        background: #f8fafc;
        padding: 8px 10px;
        border-radius: 6px;
        border-left: 3px solid #cbd5e1;
      }
      .nhan {
        background: #fef3c7;
        color: #92400e;
        padding: 2px 8px;
        border-radius: 12px;
        font-size: 11.5px;
      }
      .nhan.dat {
        background: #dcfce7;
        color: #166534;
      }
      .nhan.tu-choi {
        background: #fee2e2;
        color: #991b1b;
      }
      .mo {
        color: #64748b;
        font-size: 13.5px;
        margin: 0;
      }
      .nho {
        font-size: 12px;
        margin-left: auto;
      }
      .bao-loi {
        background: #fef2f2;
        color: #b91c1c;
        padding: 11px 14px;
        border-radius: 8px;
        margin-bottom: 14px;
        font-size: 14px;
      }
      .bao-ok {
        background: #f0fdf4;
        color: #166534;
        padding: 11px 14px;
        border-radius: 8px;
        margin-bottom: 14px;
        font-size: 14px;
      }
      .trong {
        text-align: center;
        color: #64748b;
        padding: 40px;
      }
    `,
  ],
})
export class ChiTietComponent implements OnInit {
  private api = inject(NhiemVuService);
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  readonly auth = inject(AuthService);

  readonly nv = signal<NhiemVuChiTiet | null>(null);
  readonly nhanVien = signal<NguoiDung[]>([]);
  readonly dangTai = signal(true);
  readonly loi = signal('');
  readonly thanhCong = signal('');

  id = 0;
  nguoiNhanId: number | null = null;
  phanTram = 50;
  noiDungTienDo = '';
  noiDungBaoCao = '';
  yKien = '';
  diemChatLuong: number | null = null;
  diemHoanThanh: number | null = null;
  readonly thangDiem = [1, 2, 3, 4, 5];

  ngOnInit(): void {
    this.id = Number(this.route.snapshot.paramMap.get('id'));
    this.tai();

    if (this.auth.coTheGiaoViec()) {
      this.http
        .get<NguoiDung[]>('/api/nguoi-dung/nhan-vien')
        .subscribe({ next: (ds) => this.nhanVien.set(ds), error: () => undefined });
    }
  }

  tai(): void {
    this.dangTai.set(true);
    this.api.chiTiet(this.id).subscribe({
      next: (nv) => {
        this.nv.set(nv);
        this.phanTram = nv.tienDoPhanTram ?? 50;
        this.dangTai.set(false);
      },
      error: () => this.dangTai.set(false),
    });
  }

  mau(): string {
    const nv = this.nv();
    return nv ? (MAU_TRANG_THAI[nv.statusCode] ?? '#64748b') : '#64748b';
  }

  // --- Điều kiện hiện nút. Chỉ để che bớt giao diện; quyền thật do backend quyết định. ---

  coTheGiao(): boolean {
    const nv = this.nv();
    return (
      !!nv &&
      this.auth.coTheGiaoViec() &&
      nv.creatorId === this.auth.nguoiDung()?.id &&
      (nv.statusCode === 'MOI_TAO' || nv.statusCode === 'DA_GIAO')
    );
  }

  coTheTiepNhan(): boolean {
    const nv = this.nv();
    return !!nv && nv.assigneeId === this.auth.nguoiDung()?.id && nv.statusCode === 'DA_GIAO';
  }

  coTheCapNhatTienDo(): boolean {
    const nv = this.nv();
    return (
      !!nv &&
      nv.assigneeId === this.auth.nguoiDung()?.id &&
      (nv.statusCode === 'DANG_THUC_HIEN' || nv.statusCode === 'YEU_CAU_BO_SUNG')
    );
  }

  coTheBaoCao(): boolean {
    const nv = this.nv();
    return !!nv && nv.assigneeId === this.auth.nguoiDung()?.id && nv.statusCode === 'DANG_THUC_HIEN';
  }

  coTheDuyet(): boolean {
    const nv = this.nv();
    return (
      !!nv &&
      this.auth.coTheGiaoViec() &&
      nv.creatorId === this.auth.nguoiDung()?.id &&
      nv.statusCode === 'CHO_XAC_NHAN'
    );
  }

  // --- Hành động ---

  private xong(thongBao: string) {
    return {
      next: () => {
        this.thanhCong.set(thongBao);
        this.loi.set('');
        this.tai();
      },
      error: (e: any) => {
        this.thanhCong.set('');
        this.loi.set(e?.error?.detail ?? 'Thao tác không thực hiện được.');
      },
    };
  }

  giao(): void {
    if (this.nguoiNhanId) {
      this.api.giao(this.id, this.nguoiNhanId).subscribe(this.xong('Đã giao nhiệm vụ.'));
    }
  }

  tiepNhan(): void {
    this.api.tiepNhan(this.id).subscribe(this.xong('Đã tiếp nhận nhiệm vụ.'));
  }

  capNhatTienDo(): void {
    this.api
      .capNhatTienDo(this.id, this.phanTram, this.noiDungTienDo)
      .subscribe({ ...this.xong('Đã cập nhật tiến độ.'), });
    this.noiDungTienDo = '';
  }

  guiBaoCao(): void {
    if (!this.noiDungBaoCao.trim()) {
      this.loi.set('Nội dung báo cáo không được để trống.');
      return;
    }
    this.api.guiBaoCao(this.id, this.noiDungBaoCao).subscribe(this.xong('Đã gửi báo cáo.'));
    this.noiDungBaoCao = '';
  }

  duyet(xacNhan: boolean): void {
    const nv = this.nv();
    // Báo cáo đang chờ luôn nằm đầu danh sách vì backend sắp mới nhất lên trước.
    const bc = nv?.danhSachBaoCao.find((b) => b.reportStatus === 'CHO_XAC_NHAN');
    if (!bc) {
      this.loi.set('Không tìm thấy báo cáo đang chờ duyệt.');
      return;
    }
    if (!xacNhan && !this.yKien.trim()) {
      this.loi.set('Phải nêu lý do khi từ chối báo cáo.');
      return;
    }
    // Backend cho phép bỏ trống, nhưng xác nhận hoàn thành mà không chấm thì AI mất dữ liệu
    // hiệu suất của nhiệm vụ này — nên giao diện yêu cầu chấm ít nhất chất lượng.
    if (xacNhan && this.diemChatLuong === null) {
      this.loi.set('Chấm điểm chất lượng trước khi xác nhận hoàn thành.');
      return;
    }
    this.api
      .duyetBaoCao(bc.id, xacNhan, this.yKien, this.diemChatLuong, this.diemHoanThanh)
      .subscribe(this.xong(xacNhan ? 'Đã xác nhận hoàn thành.' : 'Đã yêu cầu bổ sung.'));
    this.yKien = '';
    this.diemChatLuong = null;
    this.diemHoanThanh = null;
  }

  quayLai(): void {
    this.router.navigate(['/nhiem-vu']);
  }
}
