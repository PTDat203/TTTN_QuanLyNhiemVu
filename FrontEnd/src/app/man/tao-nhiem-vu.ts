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
          <h2><span class="cham-ai"></span>AI gợi ý người thực hiện</h2>
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
        gap: 16px;
        margin-bottom: 20px;
      }
      h1 {
        margin: 0;
        font-size: 24px;
      }
      .hai-cot {
        display: grid;
        grid-template-columns: 1fr 420px;
        gap: 20px;
        align-items: start;
      }
      @media (max-width: 1040px) {
        .hai-cot {
          grid-template-columns: 1fr;
        }
      }
      .the {
        padding: 22px;
        display: grid;
        gap: 15px;
      }
      label {
        display: grid;
        gap: 6px;
        font-size: 13px;
        font-weight: 550;
        color: var(--chu-vua);
      }
      .hang {
        display: grid;
        grid-template-columns: repeat(3, 1fr);
        gap: 12px;
      }
      @media (max-width: 560px) {
        .hang {
          grid-template-columns: 1fr;
        }
      }
      .bat-buoc {
        color: var(--do);
      }
      small {
        color: var(--chu-nhat);
        font-size: 12px;
        font-weight: 400;
      }
      form .nut-chinh {
        padding: 11px;
        font-size: 15px;
        margin-top: 4px;
      }
      .bao-loi {
        margin-bottom: 15px;
      }

      /* ------------------------------------------------ cot goi y AI */

      /* Vien tren mau chinh de cot nay doc ra ngay la khu vuc cua AI, tach khoi form. */
      .goi-y {
        position: sticky;
        top: 88px;
        padding: 0;
        overflow: hidden;
        gap: 0;
        border-top: 3px solid var(--chinh);
        box-shadow: var(--bong);
        max-height: calc(100vh - 116px);
        overflow-y: auto;
      }
      .dau-goi-y {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 12px;
        padding: 16px 18px 12px;
      }
      .cham-ai {
        width: 8px;
        height: 8px;
        border-radius: 50%;
        background: var(--chinh);
        box-shadow: 0 0 0 3px rgb(37 99 235 / 18%);
        flex: none;
      }
      .dau-goi-y h2 {
        margin: 0;
        font-size: 15.5px;
        display: flex;
        align-items: center;
        gap: 9px;
      }
      .dau-goi-y button {
        background: var(--chinh);
        border-color: var(--chinh);
        color: #fff;
        font-weight: 600;
        padding: 7px 16px;
        font-size: 13px;
        box-shadow: var(--bong-chinh);
      }
      .dau-goi-y button:hover:not(:disabled) {
        background: var(--chinh-dam);
      }
      .tom-tat {
        font-size: 11.5px;
        color: var(--chu-mo);
        padding: 0 18px 14px;
        border-bottom: 1px solid var(--vien);
        line-height: 1.6;
      }

      .goi-y > .suy-luan,
      .goi-y > .ky-nang,
      .goi-y > .canh-bao,
      .goi-y > .ung-vien,
      .goi-y > .chu-thich,
      .goi-y > .mo {
        margin: 14px 18px;
      }

      /* ------------------------------------------------ tang 1: doan phong */

      .suy-luan {
        border: 1px solid var(--vien);
        border-left: 3px solid var(--xanh);
        border-radius: var(--bo-nho);
        background: var(--xanh-nhat);
        padding: 12px 14px;
        display: grid;
        gap: 7px;
        font-size: 12.5px;
        color: var(--chu-vua);
      }
      .suy-luan[data-ket-luan='LUONG_LU'] {
        border-left-color: var(--cam);
        background: var(--cam-nhat);
      }
      .suy-luan[data-ket-luan='KHONG_RO'] {
        border-left-color: var(--chu-mo);
        background: var(--nen-diu);
      }
      .nhan-ket-luan {
        font-weight: 700;
        margin-right: 5px;
        color: var(--chu);
      }
      .dong-phong {
        display: grid;
        grid-template-columns: 1fr 88px 32px;
        gap: 9px;
        align-items: center;
        font-size: 12px;
        color: var(--chu-nhat);
      }
      .dong-phong.duoc-chon {
        color: var(--chu);
        font-weight: 600;
      }
      .dong-phong .thanh {
        height: 6px;
        background: rgb(15 23 42 / 8%);
        border-radius: 99px;
        overflow: hidden;
      }
      .dong-phong .thanh i {
        display: block;
        height: 100%;
        border-radius: 99px;
        background: var(--chu-mo);
        transition: width 400ms cubic-bezier(0.4, 0, 0.2, 1);
      }
      .dong-phong.duoc-chon .thanh i {
        background: var(--xanh);
      }
      .so {
        text-align: right;
        font-variant-numeric: tabular-nums;
      }
      .ghi-chu {
        font-size: 11.5px;
        color: var(--chu-nhat);
      }

      .ky-nang {
        display: flex;
        flex-wrap: wrap;
        gap: 6px;
        align-items: center;
      }
      .chip {
        background: var(--chinh-nhat);
        border: 1px solid var(--chinh-vien);
        color: var(--chinh-dam);
        border-radius: 99px;
        padding: 3px 10px;
        font-size: 11.5px;
        font-weight: 550;
      }
      /* Vien dut = do AI tu trich, vien lien = nguoi nhap. Phan biet duoc bang mat. */
      .chip-ai {
        border-style: dashed;
      }
      .canh-bao {
        background: var(--cam-nhat);
        border: 1px solid #fde68a;
        color: #92400e;
        padding: 10px 12px;
        border-radius: var(--bo-nho);
        font-size: 12.5px;
        line-height: 1.55;
      }

      /* ------------------------------------------------ tang 2: ung vien */

      .ung-vien {
        border: 1px solid var(--vien);
        border-radius: var(--bo);
        padding: 14px;
        display: grid;
        gap: 10px;
        background: var(--mat);
        transition: border-color var(--chuyen), box-shadow var(--chuyen);
      }
      .ung-vien:hover {
        border-color: var(--vien-dam);
        box-shadow: var(--bong-nhe);
      }
      .ung-vien.duoc-chon {
        border-color: var(--chinh);
        background: var(--chinh-nhat);
        box-shadow: 0 0 0 3px rgb(37 99 235 / 10%);
      }
      .hang-ten {
        display: flex;
        align-items: center;
        gap: 10px;
      }
      /* Hang 1 doi mau de mat bat duoc ngay ai dung dau. */
      .hang-so {
        background: var(--chu);
        color: #fff;
        width: 23px;
        height: 23px;
        border-radius: 50%;
        display: grid;
        place-items: center;
        font-size: 11.5px;
        font-weight: 700;
        flex: none;
      }
      .ung-vien:first-of-type .hang-so {
        background: var(--chinh);
        box-shadow: 0 2px 8px rgb(37 99 235 / 40%);
      }
      .ten-khoi {
        display: grid;
        line-height: 1.35;
        min-width: 0;
      }
      .ten-khoi strong {
        font-size: 14px;
        color: var(--chu);
        font-weight: 600;
      }
      .ten-khoi small {
        font-size: 11.5px;
      }
      .nhan-moi {
        background: #f3e8ff;
        color: #6b21a8;
        border: 1px solid #e9d5ff;
        font-size: 10.5px;
        font-weight: 600;
        padding: 2px 8px;
        border-radius: 99px;
        flex: none;
      }
      .diem {
        margin-left: auto;
        font-size: 20px;
        font-weight: 700;
        color: var(--chu);
        letter-spacing: -0.03em;
        flex: none;
      }
      .thanh-diem {
        display: flex;
        height: 10px;
        border-radius: 99px;
        overflow: hidden;
        background: var(--nen);
      }
      .thanh-diem i {
        display: block;
        height: 100%;
        transition: width 400ms cubic-bezier(0.4, 0, 0.2, 1);
      }
      .ly-do {
        margin: 0;
        padding-left: 17px;
        font-size: 12.5px;
        color: var(--chu-nhat);
        line-height: 1.65;
      }
      .ly-do li::marker {
        color: var(--vien-dam);
      }
      .nut-chon {
        border-color: var(--chinh-vien);
        color: var(--chinh);
        background: var(--mat);
        padding: 7px;
        font-size: 13px;
        font-weight: 600;
      }
      .nut-chon:hover {
        background: var(--chinh-nhat);
        border-color: var(--chinh);
      }
      .ung-vien.duoc-chon .nut-chon {
        background: var(--chinh);
        border-color: var(--chinh);
        color: #fff;
      }

      .chu-thich {
        display: flex;
        gap: 11px;
        flex-wrap: wrap;
        font-size: 11px;
        color: var(--chu-nhat);
        border-top: 1px solid var(--vien);
        padding-top: 12px;
      }
      .chu-thich i {
        display: inline-block;
        width: 9px;
        height: 9px;
        border-radius: 3px;
        margin-right: 5px;
      }
      .mo {
        color: var(--chu-nhat);
        font-size: 13px;
        line-height: 1.7;
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
