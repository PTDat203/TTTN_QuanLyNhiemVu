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

            <!-- Đã giao nhưng người nhận chưa bấm Tiếp nhận: backend vẫn cho đổi người.
                 Phải nói rõ đây là ĐỔI người, không thì người giao tưởng thao tác lúc nãy
                 chưa ăn và bấm giao lại lần nữa. -->
            @if (coTheDoiNguoi()) {
              <p class="mo">
                Đã giao cho <strong>{{ nv()!.tenNguoiThucHien }}</strong> — chưa tiếp nhận.
                Còn đổi được người cho tới khi họ bấm Tiếp nhận.
              </p>
              <div class="hanh-dong">
                <select [(ngModel)]="nguoiNhanId">
                  <option [ngValue]="null">— Giữ nguyên người hiện tại —</option>
                  @for (n of nguoiCoTheDoiSang(); track n.id) {
                    <option [ngValue]="n.id">
                      {{ n.fullName }}{{ n.jobTitle ? ' — ' + n.jobTitle : '' }}
                    </option>
                  }
                </select>
                <button class="phu-nhat" (click)="giao()" [disabled]="!nguoiNhanId">
                  Đổi người thực hiện
                </button>
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

            @if (!coTheGiao() && !coTheDoiNguoi() && !coTheTiepNhan() && !coTheCapNhatTienDo()
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
        margin-bottom: 20px;
        gap: 16px;
      }
      h1 {
        margin: 9px 0 0;
        font-size: 23px;
      }
      h2 {
        margin: 0 0 13px;
        font-size: 13px;
        font-weight: 600;
        letter-spacing: 0.03em;
        text-transform: uppercase;
        color: var(--chu-nhat);
      }
      .the-trang-thai {
        display: inline-flex;
        align-items: center;
        padding: 4px 12px;
        border-radius: 99px;
        font-size: 12px;
        font-weight: 600;
        color: #fff;
      }
      .hai-cot {
        display: grid;
        grid-template-columns: 1fr 400px;
        gap: 20px;
        align-items: start;
      }
      @media (max-width: 1040px) {
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
        padding: 20px;
      }
      .mo-ta {
        white-space: pre-wrap;
        line-height: 1.7;
        color: var(--chu-vua);
        margin: 0 0 18px;
      }

      /* Danh sach thuoc tinh: nhan xam nhat ben trai, gia tri dam ben phai. */
      dl {
        display: grid;
        grid-template-columns: 150px 1fr;
        gap: 10px 16px;
        margin: 0;
        font-size: 13.5px;
      }
      dt {
        color: var(--chu-nhat);
      }
      dd {
        margin: 0;
        color: var(--chu);
        font-weight: 500;
      }
      .qua-han {
        color: var(--do);
        font-weight: 600;
      }

      .hanh-dong {
        display: flex;
        gap: 10px;
        align-items: center;
        flex-wrap: wrap;
      }
      .hanh-dong.doc {
        display: grid;
        gap: 11px;
      }
      .hang {
        display: flex;
        gap: 10px;
        align-items: flex-end;
      }
      .hang button {
        flex: 1;
      }
      input[type='range'] {
        width: 100%;
        accent-color: var(--chinh);
        cursor: pointer;
      }
      label {
        display: grid;
        gap: 7px;
        font-size: 13px;
        font-weight: 550;
        color: var(--chu-vua);
      }
      .cham-diem {
        display: flex;
        gap: 12px;
      }
      .cham-diem label {
        flex: 1;
      }
      .nut-dat {
        background: var(--xanh);
        border-color: var(--xanh);
        color: #fff;
        font-weight: 600;
      }
      .nut-dat:hover:not(:disabled) {
        background: #15803d;
        border-color: #15803d;
      }
      .nut-tu-choi {
        background: var(--mat);
        border-color: #fca5a5;
        color: var(--do);
        font-weight: 600;
      }
      .nut-tu-choi:hover:not(:disabled) {
        background: var(--do-nhat);
        border-color: var(--do);
      }

      /* Dong thoi gian: moi muc mot vach doc ben trai. */
      .muc {
        border-top: 1px solid var(--vien);
        padding: 13px 0 0 14px;
        margin-top: 13px;
        position: relative;
      }
      .muc::before {
        content: '';
        position: absolute;
        left: 0;
        top: 15px;
        bottom: 2px;
        width: 2px;
        border-radius: 99px;
        background: var(--vien);
      }
      .muc:first-of-type {
        border-top: 0;
        padding-top: 0;
        margin-top: 0;
      }
      .muc:first-of-type::before {
        top: 3px;
      }
      .hang-muc {
        display: flex;
        align-items: center;
        gap: 9px;
        flex-wrap: wrap;
        margin-bottom: 5px;
      }
      .hang-muc strong {
        color: var(--chinh);
        font-size: 14px;
        font-weight: 700;
      }
      .muc p {
        margin: 0;
        font-size: 13px;
        color: var(--chu-vua);
        line-height: 1.65;
      }
      .diem-bc {
        margin-top: 7px !important;
        font-size: 12.5px !important;
        color: var(--chu-nhat) !important;
        background: var(--nen-diu);
        border: 1px solid var(--vien);
        border-radius: var(--bo-nho);
        padding: 7px 11px;
      }
      .y-kien {
        margin-top: 7px !important;
        font-size: 12.5px !important;
        background: var(--chinh-nhat);
        border-left: 3px solid var(--chinh-vien);
        border-radius: 0 var(--bo-nho) var(--bo-nho) 0;
        padding: 8px 11px;
      }

      .nhan {
        background: var(--cam-nhat);
        color: #92400e;
        border: 1px solid #fde68a;
      }
      .nhan.dat {
        background: var(--xanh-nhat);
        color: #166534;
        border-color: #bbf7d0;
      }
      .nhan.tu-choi {
        background: var(--do-nhat);
        color: #b91c1c;
        border-color: #fecaca;
      }
      .mo {
        color: var(--chu-nhat);
        font-size: 13px;
      }
      .nho {
        font-size: 11.5px;
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

  /** Giao lần đầu — nhiệm vụ còn chưa có người thực hiện. */
  coTheGiao(): boolean {
    const nv = this.nv();
    return (
      !!nv &&
      this.auth.coTheGiaoViec() &&
      nv.creatorId === this.auth.nguoiDung()?.id &&
      nv.statusCode === 'MOI_TAO'
    );
  }

  /**
   * Đổi người khi đã giao nhưng người nhận chưa tiếp nhận.
   *
   * Backend cho phép (NhiemVuService.GiaoAsync xử lý riêng ca DA_GIAO) vì lúc này chưa ai
   * bắt tay vào việc. Tách khỏi {@link coTheGiao} để giao diện gọi đúng tên thao tác.
   */
  coTheDoiNguoi(): boolean {
    const nv = this.nv();
    return (
      !!nv &&
      this.auth.coTheGiaoViec() &&
      nv.creatorId === this.auth.nguoiDung()?.id &&
      nv.statusCode === 'DA_GIAO'
    );
  }

  /** Danh sách để đổi sang — bỏ người đang giữ việc, chọn lại chính họ thì vô nghĩa. */
  nguoiCoTheDoiSang(): NguoiDung[] {
    const dangGiu = this.nv()?.assigneeId;
    return this.nhanVien().filter((n) => n.id !== dangGiu);
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
        // Trả ô chọn về rỗng. Để nguyên người vừa chọn thì màn hình trông như thao tác
        // chưa gửi đi, và người giao dễ bấm thêm lần nữa.
        this.nguoiNhanId = null;
        this.tai();
      },
      error: (e: any) => {
        this.thanhCong.set('');
        this.loi.set(e?.error?.detail ?? 'Thao tác không thực hiện được.');
      },
    };
  }

  giao(): void {
    if (!this.nguoiNhanId) return;
    const doiNguoi = this.coTheDoiNguoi();
    this.api
      .giao(this.id, this.nguoiNhanId)
      .subscribe(this.xong(doiNguoi ? 'Đã đổi người thực hiện.' : 'Đã giao nhiệm vụ.'));
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
