import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../core/api.service';

@Component({
  selector: 'app-dang-nhap',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="khung-dang-nhap">
      <!-- Cột trái chỉ hiện trên màn rộng: nói đề tài là gì trước khi người ta đăng nhập. -->
      <aside class="gioi-thieu">
        <div class="dau">NV</div>
        <h2>Phân hệ Giao nhiệm vụ</h2>
        <p>Tích hợp AI hỗ trợ gợi ý người thực hiện phù hợp</p>
        <ul>
          <li><span>1</span> AI đoán nhiệm vụ thuộc phòng nào</li>
          <li><span>2</span> Xếp hạng ứng viên theo bảy tiêu chí</li>
          <li><span>3</span> Giải thích lý do từng gợi ý</li>
        </ul>
      </aside>

      <form class="the" (ngSubmit)="dangNhap()">
        <h1>Đăng nhập</h1>
        <p class="phu">Dùng tài khoản được cấp để vào hệ thống</p>

        @if (loi()) {
          <div class="bao-loi">{{ loi() }}</div>
        }

        <label>
          Tên đăng nhập
          <input name="username" [(ngModel)]="username" autocomplete="username" required />
        </label>

        <label>
          Mật khẩu
          <input
            name="password"
            type="password"
            [(ngModel)]="password"
            autocomplete="current-password"
            required
          />
        </label>

        <button type="submit" class="nut-chinh" [disabled]="dangGui()">
          {{ dangGui() ? 'Đang đăng nhập…' : 'Đăng nhập' }}
        </button>

        <div class="goi-y">
          <strong>Tài khoản demo</strong> — mật khẩu chung <code>123456</code>
          <div class="luoi">
            @for (tk of taiKhoanDemo; track tk.ten) {
              <button type="button" class="nho" (click)="dienNhanh(tk.ten)">
                <strong>{{ tk.ten }}</strong>
                <span>{{ tk.moTa }}</span>
              </button>
            }
          </div>
        </div>
      </form>
    </div>
  `,
  styles: [
    `
      /* Nền tối có hai quầng sáng mờ, để thẻ trắng nổi hẳn lên. Không dùng ảnh nên
         không tốn thêm request nào. */
      .khung-dang-nhap {
        min-height: 100vh;
        display: grid;
        grid-template-columns: minmax(0, 1fr) 400px;
        align-items: center;
        justify-content: center;
        gap: 72px;
        padding: 40px;
        background:
          radial-gradient(900px 500px at 12% 18%, rgb(37 99 235 / 22%), transparent 60%),
          radial-gradient(700px 500px at 88% 88%, rgb(8 145 178 / 18%), transparent 60%),
          linear-gradient(160deg, #0f172a 0%, #1e293b 100%);
      }

      .gioi-thieu {
        color: #e2e8f0;
        max-width: 460px;
        justify-self: end;
      }
      .gioi-thieu .dau {
        display: grid;
        place-items: center;
        width: 46px;
        height: 46px;
        border-radius: 13px;
        background: var(--chinh);
        color: #fff;
        font-weight: 700;
        font-size: 16px;
        box-shadow: 0 6px 20px rgb(37 99 235 / 50%);
        margin-bottom: 22px;
      }
      .gioi-thieu h2 {
        color: #fff;
        font-size: 30px;
        margin: 0 0 10px;
        letter-spacing: -0.025em;
      }
      .gioi-thieu > p {
        color: #94a3b8;
        font-size: 15px;
        margin: 0 0 28px;
      }
      .gioi-thieu ul {
        list-style: none;
        margin: 0;
        padding: 0;
        display: grid;
        gap: 13px;
      }
      .gioi-thieu li {
        display: flex;
        align-items: center;
        gap: 13px;
        font-size: 14px;
        color: #cbd5e1;
      }
      .gioi-thieu li span {
        display: grid;
        place-items: center;
        flex: none;
        width: 26px;
        height: 26px;
        border-radius: 50%;
        background: rgb(255 255 255 / 8%);
        border: 1px solid rgb(255 255 255 / 14%);
        font-size: 12px;
        font-weight: 600;
        color: #fff;
      }

      .the {
        padding: 34px;
        border-radius: var(--bo-lon);
        box-shadow: var(--bong-noi);
        border: 0;
        width: 100%;
        display: grid;
        gap: 15px;
      }
      h1 {
        margin: 0;
        font-size: 23px;
      }
      .phu {
        margin: -9px 0 6px;
        font-size: 13.5px;
      }
      label {
        display: grid;
        gap: 6px;
        font-size: 13px;
        font-weight: 550;
        color: var(--chu-vua);
      }
      button[type='submit'] {
        padding: 11px;
        font-size: 15px;
        margin-top: 4px;
      }

      .goi-y {
        border-top: 1px solid var(--vien);
        padding-top: 16px;
        font-size: 12.5px;
        color: var(--chu-nhat);
      }
      .luoi {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 8px;
        margin-top: 10px;
      }
      /* Nút điền nhanh: bấm là đổ sẵn tài khoản, đỡ phải gõ khi demo. */
      .nho {
        display: grid;
        gap: 1px;
        text-align: left;
        line-height: 1.35;
        padding: 8px 10px;
        background: var(--nen-diu);
        border-radius: var(--bo-nho);
      }
      .nho strong {
        color: var(--chu);
        font-size: 12.5px;
        font-weight: 600;
      }
      .nho span {
        color: var(--chu-nhat);
        font-size: 11.5px;
      }
      .nho:hover {
        background: var(--chinh-nhat);
        border-color: var(--chinh-vien);
      }

      @media (max-width: 980px) {
        .khung-dang-nhap {
          grid-template-columns: minmax(0, 400px);
          justify-content: center;
          gap: 32px;
          padding: 28px 18px;
        }
        .gioi-thieu {
          justify-self: stretch;
        }
        .gioi-thieu ul {
          display: none;
        }
        .gioi-thieu h2 {
          font-size: 24px;
        }
      }
    `,
  ],
})
export class DangNhapComponent {
  private auth = inject(AuthService);
  private router = inject(Router);

  username = '';
  password = '';
  readonly loi = signal('');
  readonly dangGui = signal(false);

  /** Một tài khoản mỗi cấp, đủ để thử hết các luồng: giao việc, nhận việc, phạm vi xem. */
  readonly taiKhoanDemo = [
    { ten: 'giamdoc', moTa: 'Giám đốc' },
    { ten: 'tp.phattrien', moTa: 'Trưởng phòng Phát triển' },
    { ten: 'leader.be', moTa: 'Trưởng nhóm Backend' },
    { ten: 'nv.cuong', moTa: 'Nhân viên Backend' },
  ];

  dienNhanh(ten: string): void {
    this.username = ten;
    this.password = '123456';
  }

  dangNhap(): void {
    if (!this.username || !this.password) {
      this.loi.set('Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.');
      return;
    }

    this.dangGui.set(true);
    this.loi.set('');

    this.auth.dangNhap({ username: this.username, password: this.password }).subscribe({
      next: () => this.router.navigate(['/nhiem-vu']),
      error: (e) => {
        this.dangGui.set(false);
        // Backend trả ProblemDetails, thông báo nằm ở trường detail.
        this.loi.set(e?.error?.detail ?? 'Không đăng nhập được. Kiểm tra lại tài khoản.');
      },
    });
  }
}
