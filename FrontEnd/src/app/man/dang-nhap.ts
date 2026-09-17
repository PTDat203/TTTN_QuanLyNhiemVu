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
      <div class="hat"></div>

      <form class="cua-so" (ngSubmit)="dangNhap()">
        <!-- Dấu hiệu nhận diện vẽ bằng SVG: ba vạch dài ngắn khác nhau, vạch đầu
             được tô đậm — gợi đúng ý "danh sách nhiệm vụ có ưu tiên". Chữ viết tắt
             nhét trong ô vuông trông như bản nháp. -->
        <svg class="dau" viewBox="0 0 32 32" aria-hidden="true">
          <rect width="32" height="32" rx="9" fill="url(#g)" />
          <rect x="8" y="10" width="16" height="2.6" rx="1.3" fill="#fff" />
          <rect x="8" y="15" width="11" height="2.6" rx="1.3" fill="#fff" opacity=".62" />
          <rect x="8" y="20" width="7" height="2.6" rx="1.3" fill="#fff" opacity=".34" />
          <defs>
            <linearGradient id="g" x1="0" y1="0" x2="0" y2="32" gradientUnits="userSpaceOnUse">
              <stop stop-color="#3b82f6" />
              <stop offset="1" stop-color="#1d4ed8" />
            </linearGradient>
          </defs>
        </svg>

        <div class="tieu-de">
          <h1>Quản lý nhiệm vụ</h1>
          <p>Đăng nhập để tiếp tục</p>
        </div>

        @if (loi()) {
          <div class="bao-loi">{{ loi() }}</div>
        }

        <label>
          <span>Tên đăng nhập</span>
          <input name="username" [(ngModel)]="username" autocomplete="username" required />
        </label>

        <label>
          <span>Mật khẩu</span>
          <input
            name="password"
            type="password"
            [(ngModel)]="password"
            autocomplete="current-password"
            required
          />
        </label>

        <button type="submit" class="nut-vao" [disabled]="dangGui()">
          {{ dangGui() ? 'Đang đăng nhập…' : 'Đăng nhập' }}
        </button>

        <!-- Tài khoản demo thu về một hàng chip nhỏ. Bốn nút to như trước chiếm mất
             nửa thẻ và kéo sự chú ý khỏi hai ô nhập. -->
        <div class="demo">
          <span class="nhan-demo">Demo</span>
          @for (tk of taiKhoanDemo; track tk.ten) {
            <button type="button" [title]="tk.moTa" (click)="dienNhanh(tk.ten)">
              {{ tk.ten }}
            </button>
          }
        </div>
      </form>
    </div>
  `,
  styles: [
    `
      /* Nền: một nguồn sáng duy nhất hắt từ trên trái xuống nền xanh rất tối. Bản
         trước dùng hai quầng màu xanh lam và lục lam chọi nhau — đúng kiểu mẫu có
         sẵn, nhìn rẻ. Một nguồn sáng thì giống ánh sáng thật hơn. */
      .khung-dang-nhap {
        position: relative;
        min-height: 100vh;
        display: grid;
        place-items: center;
        padding: 24px;
        overflow: hidden;
        background:
          radial-gradient(1200px 760px at 18% -10%, #1e3a8a 0%, transparent 58%),
          linear-gradient(168deg, #0b1220 0%, #0f172a 52%, #0b1220 100%);
      }

      /* Lớp hạt nhiễu rất mờ phủ lên trên. Đây là chi tiết tạo khác biệt lớn nhất:
         gradient phẳng luôn bị kẻ sọc trên màn 8 bit và trông như đồ hoạ máy tính,
         thêm hạt vào là hết sọc và có cảm giác vật liệu. */
      .hat {
        position: absolute;
        inset: 0;
        pointer-events: none;
        opacity: 0.4;
        background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='180' height='180'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.9' numOctaves='4' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='180' height='180' filter='url(%23n)' opacity='0.09'/%3E%3C/svg%3E");
      }

      .cua-so {
        position: relative;
        width: 100%;
        max-width: 372px;
        background: var(--mat);
        border-radius: 18px;
        padding: 36px 34px 30px;
        display: grid;
        gap: 14px;
        /* Bốn lớp: viền tóc sáng ở mép trên cho cảm giác được chiếu sáng từ trên,
           một bóng sát để tạo mép, hai bóng loe rộng dần cho độ sâu. */
        box-shadow:
          inset 0 1px 0 rgb(255 255 255 / 90%),
          0 1px 2px rgb(2 6 23 / 30%),
          0 12px 28px rgb(2 6 23 / 26%),
          0 40px 80px rgb(2 6 23 / 34%);
      }

      .dau {
        width: 32px;
        height: 32px;
        filter: drop-shadow(0 3px 8px rgb(37 99 235 / 40%));
      }

      .tieu-de {
        display: grid;
        gap: 4px;
        margin-bottom: 4px;
      }
      h1 {
        margin: 0;
        font-size: 21px;
        font-weight: 650;
        letter-spacing: -0.025em;
      }
      .tieu-de p {
        margin: 0;
        font-size: 13px;
        color: var(--chu-nhat);
      }

      label {
        display: grid;
        gap: 7px;
      }
      label span {
        font-size: 12px;
        font-weight: 600;
        letter-spacing: 0.01em;
        color: var(--chu-vua);
      }
      /* Ô nhập hơi ngả xám khi nghỉ, trắng hẳn khi đang gõ — người dùng biết ngay
         con trỏ đang ở đâu mà không cần viền đậm. */
      .cua-so input {
        height: 42px;
        padding: 0 13px;
        border: 1px solid var(--vien);
        border-radius: 10px;
        background: #fafbfc;
        font-size: 14px;
      }
      .cua-so input:hover:not(:focus) {
        background: var(--mat);
        border-color: var(--vien-dam);
      }
      .cua-so input:focus {
        background: var(--mat);
      }

      .nut-vao {
        height: 42px;
        margin-top: 6px;
        border: 0;
        border-radius: 10px;
        color: #fff;
        font-size: 14px;
        font-weight: 600;
        letter-spacing: 0.01em;
        background: linear-gradient(180deg, #3b82f6 0%, #2563eb 100%);
        box-shadow:
          inset 0 1px 0 rgb(255 255 255 / 26%),
          0 1px 2px rgb(29 78 216 / 40%),
          0 6px 16px rgb(37 99 235 / 32%);
      }
      .nut-vao:hover:not(:disabled) {
        background: linear-gradient(180deg, #2563eb 0%, #1d4ed8 100%);
      }
      .nut-vao:disabled {
        background: var(--vien-dam);
        box-shadow: none;
        opacity: 1;
      }

      /* Hàng chip demo: chữ nhỏ, độ tương phản thấp, nằm yên cho tới khi rê chuột. */
      .demo {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 6px;
        border-top: 1px solid var(--vien);
        margin-top: 4px;
        padding-top: 14px;
      }
      .nhan-demo {
        font-size: 10.5px;
        font-weight: 700;
        letter-spacing: 0.07em;
        text-transform: uppercase;
        color: var(--chu-mo);
        margin-right: 2px;
      }
      .demo button {
        padding: 3px 9px;
        border-radius: 99px;
        border: 1px solid var(--vien);
        background: var(--nen-diu);
        color: var(--chu-nhat);
        font-size: 11.5px;
        font-weight: 500;
        font-family: 'JetBrains Mono', Consolas, monospace;
      }
      .demo button:hover {
        background: var(--chinh-nhat);
        border-color: var(--chinh-vien);
        color: var(--chinh-dam);
      }

      .bao-loi {
        font-size: 12.5px;
      }

      @media (max-width: 420px) {
        .cua-so {
          padding: 28px 22px 24px;
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
