import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { GoiYService, NhiemVuService } from '../core/api.service';
import {
  ChiTietDiem, GoiYResponse, KetLuanPhongBan, MucUuTien, NguoiDung, THANH_PHAN_DIEM, UngVien,
} from '../core/models';

@Component({
  selector: 'app-tao-nhiem-vu',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="dau-trang">
      <h1>Tạo nhiệm vụ</h1>
      <button class="phu-nhat" (click)="quayLai()">← Quay lại</button>
    </div>

    @if (loi()) {
      <div class="bao-loi">{{ loi() }}</div>
    }

    <div class="hai-cot">
      <!-- ------------------------------------------------ form -->
      <form class="the" (ngSubmit)="luu()">
        <label>
          Tiêu đề <span class="bat-buoc">*</span>
          <input name="title" [(ngModel)]="title" maxlength="200" required />
        </label>

        <label>
          Mô tả
          <textarea name="description" [(ngModel)]="description" rows="5"></textarea>
          <small>Mô tả càng rõ thì AI đoán phòng và gợi ý người càng chính xác.</small>
        </label>

        <div class="hang">
          <label>
            Mức ưu tiên
            <select name="priority" [(ngModel)]="priority">
              <option value="LOW">Thấp</option>
              <option value="MEDIUM">Trung bình</option>
              <option value="HIGH">Cao</option>
            </select>
          </label>

          <label>
            Ngày bắt đầu
            <input type="date" name="startDate" [(ngModel)]="startDate" />
          </label>

          <label>
            Hạn hoàn thành
            <input type="date" name="dueDate" [(ngModel)]="dueDate" />
          </label>
        </div>

        <label>
          Người thực hiện
          <select name="assigneeId" [(ngModel)]="assigneeId">
            <option [ngValue]="null">— Chưa giao, để AI đoán phòng rồi giao sau —</option>
            @for (nv of nhanVien(); track nv.id) {
              <option [ngValue]="nv.id">{{ moTaNguoi(nv) }}</option>
            }
          </select>
          <small>Chỉ hiện những người trong phạm vi bạn được giao việc.</small>
        </label>

        <button type="submit" class="nut-chinh" [disabled]="dangLuu()">
          {{ dangLuu() ? 'Đang lưu…' : 'Lưu nhiệm vụ' }}
        </button>
      </form>

      <!-- ------------------------------------------------ AI gợi ý -->
      <aside class="the goi-y">
        <div class="dau-goi-y">
          <h2>AI gợi ý người thực hiện</h2>
          <button type="button" (click)="xinGoiY()" [disabled]="dangGoiY() || !title.trim()">
            {{ dangGoiY() ? 'Đang tính…' : 'Gợi ý' }}
          </button>
        </div>

        @if (ketQuaGoiY(); as kq) {
          <div class="tom-tat">
            {{ kq.phuongPhap }} · xét {{ kq.soUngVienDaXet }}/{{ kq.soUngVienTrongPhamVi }} người
            · {{ kq.thoiGianMs }}ms · tham số {{ kq.phienBanTrongSo }}
          </div>

          <!-- Tầng 1: AI đoán nhiệm vụ thuộc phòng nào -->
          <div class="suy-luan" [attr.data-ket-luan]="kq.suyLuanPhongBan.ketLuan">
            <div class="dong-dau">
              <span class="nhan-ket-luan">{{ nhanKetLuan(kq.suyLuanPhongBan.ketLuan) }}</span>
              {{ kq.suyLuanPhongBan.moTa }}
            </div>
            @for (p of kq.suyLuanPhongBan.cacPhong; track p.id) {
              <div class="dong-phong" [class.duoc-chon]="kq.suyLuanPhongBan.phongDaChon.includes(p.id)">
                <span class="ten-phong">{{ p.ten }}</span>
                <span class="thanh"><i [style.width.%]="p.diem * 100"></i></span>
                <span class="so">{{ p.diem.toFixed(2) }}</span>
              </div>
            }
            @if (kq.suyLuanPhongBan.nhom; as nhom) {
              <div class="ghi-chu">Nhóm phù hợp nhất: <strong>{{ nhom.ten }}</strong></div>
            }
          </div>

          @if (kq.kyNangYeuCau.length) {
            <div class="ky-nang">
              <span class="ghi-chu">Kỹ năng cần:</span>
              @for (k of kq.kyNangYeuCau; track k.skillId) {
                <span class="chip" [class.chip-ai]="k.nguon === 'AI'"
                      [title]="k.nguon === 'AI' ? 'AI trích từ nội dung, độ khớp ' + (k.doKhop ?? 0).toFixed(2) : 'Người giao nhập'">
                  {{ k.ten }}{{ k.mucYeuCau ? ' ≥ ' + k.mucYeuCau : '' }}
                </span>
              }
            </div>
          }

          @for (c of kq.canhBao; track c) {
            <div class="canh-bao">{{ c }}</div>
          }

          <!-- Tầng 2: xếp hạng trong tập đã lọc -->
          @for (uv of kq.ungVien; track uv.userId) {
            <div class="ung-vien" [class.duoc-chon]="assigneeId === uv.userId">
              <div class="hang-ten">
                <span class="hang-so">{{ uv.thuHang }}</span>
                <span class="ten-khoi">
                  <strong>{{ uv.fullName }}</strong>
                  <small>{{ uv.chucDanh }}{{ uv.tenNhom ? ' · ' + uv.tenNhom : '' }}</small>
                </span>
                @if (uv.soLieu.chuaCoLichSu) {
                  <span class="nhan-moi" title="Chưa có lịch sử — hiệu suất và đúng hạn dùng giá trị mặc định">Mới</span>
                }
                <span class="diem">{{ (uv.diem * 100).toFixed(1) }}</span>
              </div>

              <!-- Thanh phân rã điểm: mỗi đoạn là một thành phần, bề rộng đúng bằng phần đóng
                   góp của nó. Nhìn là thấy điểm tổng do đâu mà có. -->
              <div class="thanh-diem" [title]="moTaDiem(uv)">
                @for (tp of thanhPhan; track tp.khoa) {
                  <i [style.width.%]="uv.chiTietDiem[tp.khoa].dongGop * 100" [style.background]="tp.mau"></i>
                }
              </div>

              <ul class="ly-do">
                @for (l of uv.lyDo; track l) {
                  <li>{{ l }}</li>
                }
              </ul>

              <button type="button" class="nut-chon" (click)="chon(uv)">
                {{ assigneeId === uv.userId ? '✓ Đã chọn' : 'Chọn người này' }}
              </button>
            </div>
          }

          <div class="chu-thich">
            @for (tp of thanhPhan; track tp.khoa) {
              <span><i [style.background]="tp.mau"></i>{{ tp.ten }} {{ trongSo(kq, tp.khoa) }}</span>
            }
          </div>
          <p class="mo nho">AI chỉ đề xuất. Quyết định giao việc vẫn thuộc về bạn.</p>
        } @else {
          <p class="mo">
            Nhập tiêu đề và mô tả nhiệm vụ rồi bấm <strong>Gợi ý</strong>. AI đoán nhiệm vụ thuộc
            phòng nào, cần kỹ năng gì, rồi xếp hạng những người bạn giao được theo bảy tiêu chí:
            ngữ nghĩa, mức kỹ năng, hiệu suất, việc tương tự, đúng hạn, khối lượng việc đang gánh và thâm niên.
          </p>
        }
      </aside>
    </div>
  `,
  styles: [
    `
      .dau-trang {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 18px;
      }
      h1 {
        margin: 0;
        font-size: 22px;
      }
      .hai-cot {
        display: grid;
        grid-template-columns: 1fr 400px;
        gap: 18px;
        align-items: start;
      }
      @media (max-width: 980px) {
        .hai-cot {
          grid-template-columns: 1fr;
        }
      }
      .the {
        background: #fff;
        padding: 20px;
        border-radius: 10px;
        box-shadow: 0 1px 3px rgb(0 0 0 / 8%);
        display: grid;
        gap: 14px;
      }
      label {
        display: grid;
        gap: 5px;
        font-size: 14px;
        color: #334155;
      }
      input,
      select,
      textarea {
        padding: 9px 11px;
        border: 1px solid #cbd5e1;
        border-radius: 7px;
        font-size: 14px;
        font-family: inherit;
      }
      .hang {
        display: grid;
        grid-template-columns: repeat(3, 1fr);
        gap: 10px;
      }
      .bat-buoc {
        color: #dc2626;
      }
      small {
        color: #64748b;
        font-size: 12px;
      }
      .nut-chinh {
        background: #2563eb;
        color: #fff;
        border: 0;
        padding: 11px;
        border-radius: 8px;
        font-size: 15px;
        cursor: pointer;
      }
      .nut-chinh:disabled {
        background: #94a3b8;
      }
      .phu-nhat {
        border: 1px solid #cbd5e1;
        background: #fff;
        padding: 8px 14px;
        border-radius: 7px;
        cursor: pointer;
      }
      .bao-loi {
        background: #fef2f2;
        color: #b91c1c;
        padding: 11px 14px;
        border-radius: 8px;
        margin-bottom: 14px;
        font-size: 14px;
      }
      .dau-goi-y {
        display: flex;
        justify-content: space-between;
        align-items: center;
      }
      .dau-goi-y h2 {
        margin: 0;
        font-size: 16px;
      }
      .dau-goi-y button {
        background: #0891b2;
        color: #fff;
        border: 0;
        padding: 7px 14px;
        border-radius: 7px;
        cursor: pointer;
        font-size: 13px;
      }
      .dau-goi-y button:disabled {
        background: #94a3b8;
      }
      .tom-tat {
        font-size: 12px;
        color: #64748b;
        border-bottom: 1px solid #e2e8f0;
        padding-bottom: 8px;
      }
      .suy-luan {
        border: 1px solid #e2e8f0;
        border-left: 4px solid #16a34a;
        border-radius: 8px;
        padding: 10px 12px;
        display: grid;
        gap: 6px;
        font-size: 13px;
        color: #334155;
      }
      .suy-luan[data-ket-luan='LUONG_LU'] {
        border-left-color: #d97706;
      }
      .suy-luan[data-ket-luan='KHONG_RO'] {
        border-left-color: #94a3b8;
      }
      .nhan-ket-luan {
        font-weight: 600;
        margin-right: 4px;
      }
      .dong-phong {
        display: grid;
        grid-template-columns: 1fr 90px 34px;
        gap: 8px;
        align-items: center;
        font-size: 12.5px;
        color: #94a3b8;
      }
      .dong-phong.duoc-chon {
        color: #0f172a;
        font-weight: 500;
      }
      .dong-phong .thanh {
        height: 6px;
        background: #f1f5f9;
        border-radius: 3px;
        overflow: hidden;
      }
      .dong-phong .thanh i {
        display: block;
        height: 100%;
        background: #94a3b8;
      }
      .dong-phong.duoc-chon .thanh i {
        background: #16a34a;
      }
      .so {
        text-align: right;
        font-variant-numeric: tabular-nums;
      }
      .ghi-chu {
        font-size: 12px;
        color: #64748b;
      }
      .ky-nang {
        display: flex;
        flex-wrap: wrap;
        gap: 6px;
        align-items: center;
      }
      .chip {
        background: #f1f5f9;
        border: 1px solid #e2e8f0;
        border-radius: 12px;
        padding: 2px 9px;
        font-size: 12px;
        color: #334155;
      }
      .chip-ai {
        border-style: dashed;
      }
      .canh-bao {
        background: #fffbeb;
        color: #92400e;
        padding: 9px 11px;
        border-radius: 7px;
        font-size: 13px;
      }
      .ung-vien {
        border: 1px solid #e2e8f0;
        border-radius: 8px;
        padding: 12px;
        display: grid;
        gap: 8px;
      }
      .ung-vien.duoc-chon {
        border-color: #2563eb;
        background: #eff6ff;
      }
      .hang-ten {
        display: flex;
        align-items: center;
        gap: 8px;
      }
      .hang-so {
        background: #1e293b;
        color: #fff;
        width: 20px;
        height: 20px;
        border-radius: 50%;
        display: grid;
        place-items: center;
        font-size: 11px;
        flex-shrink: 0;
      }
      .ten-khoi {
        display: grid;
        line-height: 1.3;
      }
      .ten-khoi small {
        font-size: 11.5px;
      }
      .nhan-moi {
        background: #ede9fe;
        color: #5b21b6;
        font-size: 11px;
        padding: 1px 7px;
        border-radius: 10px;
      }
      .diem {
        margin-left: auto;
        font-size: 17px;
        font-weight: 600;
        color: #0f172a;
      }
      .thanh-diem {
        display: flex;
        height: 9px;
        border-radius: 5px;
        overflow: hidden;
        background: #f1f5f9;
      }
      .thanh-diem i {
        display: block;
        height: 100%;
      }
      .ly-do {
        margin: 0;
        padding-left: 16px;
        font-size: 12.5px;
        color: #475569;
        line-height: 1.6;
      }
      .nut-chon {
        border: 1px solid #2563eb;
        color: #2563eb;
        background: #fff;
        padding: 6px;
        border-radius: 6px;
        cursor: pointer;
        font-size: 13px;
      }
      .chu-thich {
        display: flex;
        gap: 10px;
        flex-wrap: wrap;
        font-size: 11.5px;
        color: #64748b;
        border-top: 1px solid #e2e8f0;
        padding-top: 10px;
      }
      .chu-thich i {
        display: inline-block;
        width: 9px;
        height: 9px;
        border-radius: 2px;
        margin-right: 4px;
      }
      .mo {
        color: #64748b;
        font-size: 13px;
        line-height: 1.6;
        margin: 0;
      }
      .nho {
        font-size: 11.5px;
        font-style: italic;
      }
    `,
  ],
})
export class TaoNhiemVuComponent {
  private api = inject(NhiemVuService);
  private goiYApi = inject(GoiYService);
  private http = inject(HttpClient);
  private router = inject(Router);

  readonly thanhPhan = THANH_PHAN_DIEM;

  title = '';
  description = '';
  priority: MucUuTien = 'MEDIUM';
  startDate = '';
  dueDate = '';
  assigneeId: number | null = null;

  readonly nhanVien = signal<NguoiDung[]>([]);
  readonly ketQuaGoiY = signal<GoiYResponse | null>(null);
  readonly dangGoiY = signal(false);
  readonly dangLuu = signal(false);
  readonly loi = signal('');

  constructor() {
    this.http
      .get<NguoiDung[]>('/api/nguoi-dung/nhan-vien')
      .subscribe({ next: (ds) => this.nhanVien.set(ds), error: () => undefined });
  }

  xinGoiY(): void {
    this.dangGoiY.set(true);
    this.loi.set('');

    // Gửi thẳng nội dung đang gõ, không cần lưu nhiệm vụ trước —
    // người giao xem được gợi ý ngay khi còn đang soạn.
    this.goiYApi
      .goiY({ title: this.title, description: this.description, soLuong: 5 })
      .subscribe({
        next: (kq) => {
          this.ketQuaGoiY.set(kq);
          this.dangGoiY.set(false);
        },
        error: (e) => {
          this.dangGoiY.set(false);
          this.loi.set(e?.error?.detail ?? 'Không lấy được gợi ý.');
        },
      });
  }

  chon(uv: UngVien): void {
    this.assigneeId = uv.userId;
  }

  moTaNguoi(n: NguoiDung): string {
    const viTri = [n.jobTitle, n.tenNhom ?? n.tenPhongBan].filter((x) => !!x).join(' · ');
    return viTri ? `${n.fullName} — ${viTri}` : n.fullName;
  }

  nhanKetLuan(k: KetLuanPhongBan): string {
    return k === 'CHAC_CHAN' ? 'Chắc chắn.' : k === 'LUONG_LU' ? 'Lưỡng lự.' : 'Không rõ.';
  }

  trongSo(kq: GoiYResponse, khoa: keyof ChiTietDiem): string {
    const w = kq.ungVien[0]?.chiTietDiem[khoa].trongSo;
    return w === undefined ? '' : `${Math.round(w * 100)}%`;
  }

  moTaDiem(uv: UngVien): string {
    const c = uv.chiTietDiem;
    const dong = this.thanhPhan.map(
      (tp) => `${tp.ten} ${c[tp.khoa].diem.toFixed(2)} × ${c[tp.khoa].trongSo} = ${c[tp.khoa].dongGop.toFixed(3)}`,
    );
    return [...dong, `Tổng = ${uv.diem.toFixed(4)}`].join('\n');
  }

  luu(): void {
    if (!this.title.trim()) {
      this.loi.set('Tiêu đề nhiệm vụ không được để trống.');
      return;
    }

    this.dangLuu.set(true);
    this.loi.set('');

    this.api
      .tao({
        title: this.title.trim(),
        description: this.description.trim() || null,
        priority: this.priority,
        // Ô ngày trống trả chuỗi rỗng; phải đổi thành null để backend hiểu là "không đặt".
        startDate: this.startDate || null,
        dueDate: this.dueDate || null,
        assigneeId: this.assigneeId,
      })
      .subscribe({
        next: (nv) => this.router.navigate(['/nhiem-vu', nv.id]),
        error: (e) => {
          this.dangLuu.set(false);
          this.loi.set(e?.error?.detail ?? 'Không lưu được nhiệm vụ.');
        },
      });
  }

  quayLai(): void {
    this.router.navigate(['/nhiem-vu']);
  }
}
