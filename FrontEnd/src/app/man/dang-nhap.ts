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
      <form class="the" (ngSubmit)="dangNhap()">
        <h1>Quản lý nhiệm vụ</h1>
        <p class="phu">Đăng nhập để tiếp tục</p>

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

        <button type="submit" [disabled]="dangGui()">
          {{ dangGui() ? 'Đang đăng nhập…' : 'Đăng nhập' }}
        </button>

        <div class="goi-y">
          <strong>Tài khoản demo</strong> — mật khẩu chung <code>123456</code>
          <div class="hang">
            <button type="button" class="nho" (click)="dienNhanh('manager1')">
              manager1 · Người giao
            </button>
            <button type="button" class="nho" (click)="dienNhanh('nv.an')">
              nv.an · Người thực hiện
            </button>
          </div>
        </div>
      </form>
    </div>
  `,
  styles: [
    `
      .khung-dang-nhap {
        min-height: 100vh;
        display: grid;
        place-items: center;
        background: #f1f5f9;
        padding: 16px;
      }
      .the {
        background: #fff;
        padding: 32px;
        border-radius: 12px;
        box-shadow: 0 4px 24px rgb(0 0 0 / 8%);
        width: 100%;
        max-width: 380px;
        display: grid;
        gap: 14px;
      }
      h1 {
        margin: 0;
        font-size: 22px;
        color: #0f172a;
      }
      .phu {
        margin: -8px 0 4px;
        color: #64748b;
        font-size: 14px;
      }
      label {
        display: grid;
        gap: 6px;
        font-size: 14px;
        color: #334155;
      }
      input {
        padding: 10px 12px;
        border: 1px solid #cbd5e1;
        border-radius: 8px;
        font-size: 15px;
      }
      input:focus {
        outline: 2px solid #2563eb;
        border-color: transparent;
      }
      button[type='submit'] {
        padding: 11px;
        background: #2563eb;
        color: #fff;
        border: 0;
        border-radius: 8px;
        font-size: 15px;
        cursor: pointer;
      }
      button[type='submit']:disabled {
        background: #94a3b8;
        cursor: default;
      }
      .bao-loi {
        background: #fef2f2;
        color: #b91c1c;
        padding: 10px 12px;
        border-radius: 8px;
        font-size: 14px;
      }
      .goi-y {
        border-top: 1px solid #e2e8f0;
        padding-top: 14px;
        font-size: 13px;
        color: #64748b;
      }
      .hang {
        display: flex;
        gap: 8px;
        margin-top: 8px;
      }
      .nho {
        flex: 1;
        padding: 7px;
        font-size: 12px;
        border: 1px solid #cbd5e1;
        background: #f8fafc;
        border-radius: 6px;
        cursor: pointer;
      }
      code {
        background: #f1f5f9;
        padding: 1px 5px;
        border-radius: 4px;
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
