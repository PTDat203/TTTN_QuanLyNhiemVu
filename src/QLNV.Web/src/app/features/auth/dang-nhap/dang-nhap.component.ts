import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';

import { NHAN_VAI_TRO, VaiTro } from '../../../core/models';
import { AuthService } from '../../../core/services/auth.service';
import { ThongBaoService } from '../../../core/services/thong-bao.service';

/** Mot tai khoan mau duoc goi y san tren man dang nhap (du lieu seed cua BE). */
export interface TaiKhoanDemo {
  username: string;
  hoTen: string;
  /** QUAN_TRI | NGUOI_GIAO | NGUOI_THUC_HIEN. */
  vaiTro: string;
  /** Mot cau mo ta nguoi dung nay dung de thu chuc nang nao. */
  moTa: string;
}

/**
 * M01 - Dang nhap (§3.1).
 *
 * Form username/password, goi A1 `POST /api/v1/auth/login`; `AuthService`
 * luu access/refresh token vao localStorage.
 *
 * §3.1: LUOC BO SSO WSO2/Keycloak cua he goc - app nho chi dung JWT tu phat
 * hanh. §10.11: KHONG co bat ky secret nao o tang FE, ke ca client-secret SSO.
 *
 * Man nay nam NGOAI `MainLayoutComponent` nen khong co sidebar.
 */
@Component({
  selector: 'qlnv-dang-nhap',
  templateUrl: './dang-nhap.component.html',
  styleUrls: ['./dang-nhap.component.scss']
})
export class DangNhapComponent implements OnInit {
  /** A1 - hai truong bat buoc. */
  readonly form = new FormGroup({
    username: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(100)]
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)]
    })
  });

  /** Dang goi API - khoa nut de khong gui trung. */
  dangGui = false;

  /** Loi hien ngay tren form (tieng Viet co dau). */
  loi: string | null = null;

  /** Hien / an mat khau dang go. */
  hienMatKhau = false;

  /** Duong dan quay lai sau khi dang nhap (do `authGuard` gan vao query). */
  private quayLai = '/';

  /**
   * Mat khau cua MOI tai khoan mau, do `DanhMucMau.MatKhauDemo` cua backend seed.
   * Day la ung dung DEMO cho bao cao thuc tap - khong phai he thong that.
   */
  readonly matKhauDemo = '123456';

  /**
   * Ba tai khoan dai dien cho ba cot cua bang phan quyen §6.2.
   * Bam mot the la dien san username + mat khau demo vao form.
   */
  readonly dsTaiKhoanDemo: readonly TaiKhoanDemo[] = [
    {
      username: 'admin',
      hoTen: 'Nguyễn Quốc Hưng',
      vaiTro: VaiTro.QuanTri,
      moTa: 'Xem được cả ba nhóm màn, kể cả Quản trị: người dùng, danh mục, nhật ký gợi ý AI.'
    },
    {
      username: 'lehongphuc',
      hoTen: 'Lê Hồng Phúc',
      vaiTro: VaiTro.NguoiGiao,
      moTa: 'Tạo văn bản chỉ đạo, phân công nhiệm vụ, dùng popup AI gợi ý và nghiệm thu kết quả.'
    },
    {
      username: 'nguyenvanan',
      hoTen: 'Nguyễn Văn An',
      vaiTro: VaiTro.NguoiThucHien,
      moTa: 'Tiếp nhận nhiệm vụ, cập nhật tiến độ, gửi báo cáo kết quả và xin gia hạn.'
    }
  ];

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
    private readonly route: ActivatedRoute,
    private readonly thongBao: ThongBaoService
  ) {}

  ngOnInit(): void {
    this.quayLai = this.route.snapshot.queryParamMap.get('quayLai') ?? '/';

    // Da co phien hop le thi khong bat dang nhap lai.
    if (this.auth.daDangNhap) {
      void this.router.navigateByUrl(this.quayLai);
    }
  }

  /** Nhan tieng Viet cua vai tro (§6.1 - mot truc vai tro duy nhat). */
  nhanVaiTro(ma: string): string {
    return NHAN_VAI_TRO[ma] ?? ma;
  }

  /** Bam vao mot the tai khoan mau - dien san thong tin dang nhap. */
  chonTaiKhoanDemo(tk: TaiKhoanDemo): void {
    this.form.setValue({ username: tk.username, password: this.matKhauDemo });
    this.loi = null;
  }

  /** A1 - POST /api/v1/auth/login. */
  guiForm(): void {
    if (this.dangGui) {
      return;
    }

    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.loi = 'Vui lòng nhập tên đăng nhập và mật khẩu.';
      return;
    }

    const giaTri = this.form.getRawValue();
    this.loi = null;
    this.dangGui = true;

    this.auth
      .dangNhap({ username: giaTri.username.trim(), password: giaTri.password })
      .pipe(finalize(() => (this.dangGui = false)))
      .subscribe({
        next: (kq) => {
          this.thongBao.thanhCong(`Xin chào ${kq.nguoiDung.fullname}.`, 'Đăng nhập thành công');
          void this.router.navigateByUrl(this.quayLai);
        },
        error: (nguon: unknown) => {
          // Khong nuot loi: hien nguyen thong bao tieng Viet cua backend.
          this.loi = this.thongBao.docThongBao(nguon);
        }
      });
  }

  /** Kiem tra mot truong da cham vao va dang sai. */
  sai(ten: 'username' | 'password'): boolean {
    const dk = this.form.controls[ten];
    return dk.invalid && (dk.touched || dk.dirty);
  }
}
