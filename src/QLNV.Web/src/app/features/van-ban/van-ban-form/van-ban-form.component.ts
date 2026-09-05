import { Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { NbDialogRef } from '@nebular/theme';

import {
  DoKhan,
  Guid,
  LinhVuc,
  LuuVanBanRequest,
  NHAN_DO_KHAN,
  TepDinhKem,
  TuDien,
  VanBan
} from '../../../core/models';
import {
  DanhMucService,
  FileService,
  ThongBaoService,
  VanBanService
} from '../../../core/services';

/** Tao nhanh mot FormControl chuoi khong nullable (tranh sai suy dien kieu cua FormBuilder). */
function o(giaTri = '', kiemTra: ValidatorFn[] = []): FormControl<string> {
  return new FormControl<string>(giaTri, { nonNullable: true, validators: kiemTra });
}

/** Chuoi rong -> null (BE coi rong va null nhu nhau, nhung null gon hon). */
function rongThanhNull(gt: string | null | undefined): string | null {
  const s = (gt ?? '').trim();
  return s.length > 0 ? s : null;
}

/**
 * M04 - Tao / sua van ban chi dao (dialog mo tu M03).
 *
 * §3.2 M04 - 8 truong: so ky hieu, ngay ban hanh, loai van ban, co quan ban hanh,
 * trich yeu (BAT BUOC), do khan (BAT BUOC), linh vuc, tep dinh kem.
 * Bam `dm-nhiemvu-crud` cua he goc nhung BO do mat / ma hoa noi dung (§1.4).
 *
 * MO RONG CO CHU DICH: them 2 o tuy chon `nguoitheodoi` (lanh dao phu trach) va
 * `thoigianchidao` - header cua M05 (§3.2) yeu cau hien hai gia tri nay; neu M04
 * khong cho nhap thi chung vinh vien rong.
 *
 * API dung: B2 (nap khi sua), B3 (tao), B4 (sua), G1 (tai tep).
 * Dong dialog voi ban ghi vua luu khi thanh cong => M03 nap lai luoi.
 */
@Component({
  selector: 'qlnv-van-ban-form',
  templateUrl: './van-ban-form.component.html',
  styleUrls: ['./van-ban-form.component.scss']
})
export class VanBanFormComponent implements OnInit {
  /** Id van ban khi SUA. Bo trong = tao moi. */
  @Input() id: Guid | null = null;

  /** Ban ghi da co san tu luoi M03 - dung de hien ngay, khong doi B2. */
  @Input() banGhi: VanBan | null = null;

  readonly form = new FormGroup({
    sokyhieu: o('', [Validators.maxLength(100)]),
    ngaybanhanh: o(),
    loaivb: o(),
    coquanbanhanh: o('', [Validators.maxLength(255)]),
    // §3.2 M04 - trich yeu BAT BUOC.
    trichyeu: o('', [Validators.required, Validators.maxLength(2000)]),
    // §7.4 muc 4 - do khan BAT BUOC: TRONGTAM | THUONGXUYEN | DOTXUAT.
    dokhan: o(DoKhan.ThuongXuyen, [Validators.required]),
    linhvuc: o(),
    nguoitheodoi: o('', [Validators.maxLength(255)]),
    thoigianchidao: o()
  });

  /** §7.4 muc 4 - danh muc do khan co dinh, khong doi may chu. */
  readonly dsDoKhan: ReadonlyArray<{ ma: string; nhan: string }> = [
    { ma: DoKhan.TrongTam, nhan: NHAN_DO_KHAN[DoKhan.TrongTam] },
    { ma: DoKhan.ThuongXuyen, nhan: NHAN_DO_KHAN[DoKhan.ThuongXuyen] },
    { ma: DoKhan.DotXuat, nhan: NHAN_DO_KHAN[DoKhan.DotXuat] }
  ];

  dsLoaiVb: TuDien[] = [];
  dsLinhVuc: LinhVuc[] = [];

  /** Tep dinh kem hien co (ban ghi cu) + tep vua tai len trong phien nay. */
  dsTep: TepDinhKem[] = [];

  dangNap = false;
  dangTaiTep = false;
  dangLuu = false;

  constructor(
    private readonly dialogRef: NbDialogRef<VanBanFormComponent>,
    private readonly vanBan: VanBanService,
    private readonly danhMuc: DanhMucService,
    private readonly tep: FileService,
    private readonly tb: ThongBaoService
  ) {}

  ngOnInit(): void {
    this.napDanhMuc();

    if (this.banGhi) {
      this.dat(this.banGhi);
    } else if (this.id) {
      this.napChiTiet(this.id);
    }
  }

  get laSua(): boolean {
    return !!this.id;
  }

  get tieuDe(): string {
    return this.laSua ? 'Sửa văn bản chỉ đạo' : 'Thêm văn bản chỉ đạo';
  }

  /** Dung cho lop `.sai` tren o nhap. */
  sai(ten: 'sokyhieu' | 'coquanbanhanh' | 'trichyeu' | 'dokhan' | 'nguoitheodoi'): boolean {
    const c = this.form.controls[ten];
    return c.invalid && (c.touched || c.dirty);
  }

  get soKyTuTrichYeu(): number {
    return this.form.controls.trichyeu.value.length;
  }

  // ---------------------------------------------------------------
  // Tep dinh kem (G1)
  // ---------------------------------------------------------------

  chonTep(su: Event): void {
    const input = su.target as HTMLInputElement;
    const ds: File[] = input.files ? Array.from(input.files) : [];
    // Xoa gia tri de chon lai dung tep do van kich hoat su kien change.
    input.value = '';
    if (ds.length === 0) return;

    for (const t of ds) {
      const loi = this.tep.kiemTraTep(t);
      if (loi) {
        this.tb.canhBao(loi);
        return;
      }
    }

    this.dangTaiTep = true;
    this.tep.taiLen(ds).subscribe({
      next: (kq) => {
        this.dangTaiTep = false;
        this.dsTep = [...this.dsTep, ...kq];
        this.tb.thanhCong('Đã tải lên ' + kq.length + ' tệp.');
      },
      error: (e: unknown) => {
        this.dangTaiTep = false;
        this.tb.loi(e, 'Không tải được tệp');
      }
    });
  }

  boTep(t: TepDinhKem): void {
    this.dsTep = this.dsTep.filter((x) => x.id !== t.id);
  }

  kichThuoc(t: TepDinhKem): string {
    return FileService.dinhDangKichThuoc(t.size);
  }

  // ---------------------------------------------------------------
  // Luu (B3 / B4)
  // ---------------------------------------------------------------

  luu(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.tb.canhBao('Vui lòng nhập đầy đủ Trích yếu và Độ khẩn.');
      return;
    }

    const v = this.form.getRawValue();
    const yeuCau: LuuVanBanRequest = {
      sokyhieu: rongThanhNull(v.sokyhieu),
      trichyeu: v.trichyeu.trim(),
      loaivb: rongThanhNull(v.loaivb),
      ngaybanhanh: rongThanhNull(v.ngaybanhanh),
      coquanbanhanh: rongThanhNull(v.coquanbanhanh),
      dokhan: v.dokhan,
      linhvuc: rongThanhNull(v.linhvuc),
      // <input type="datetime-local"> tra "yyyy-MM-ddTHH:mm"; BE nhan kieu DateTime.
      thoigianchidao: v.thoigianchidao ? v.thoigianchidao + ':00' : null,
      nguonnv: null,
      nguoitheodoi: rongThanhNull(v.nguoitheodoi),
      fileIds: this.dsTep.map((t) => t.id)
    };

    this.dangLuu = true;
    const goi = this.id ? this.vanBan.sua(this.id, yeuCau) : this.vanBan.tao(yeuCau);

    goi.subscribe({
      next: (kq) => {
        this.dangLuu = false;
        this.tb.thanhCong(this.laSua ? 'Đã cập nhật văn bản chỉ đạo.' : 'Đã thêm văn bản chỉ đạo.');
        this.dialogRef.close(kq);
      },
      error: (e: unknown) => {
        this.dangLuu = false;
        this.tb.loi(e, 'Không lưu được văn bản');
      }
    });
  }

  huy(): void {
    this.dialogRef.close(null);
  }

  // ---------------------------------------------------------------
  // Noi bo
  // ---------------------------------------------------------------

  private napDanhMuc(): void {
    this.danhMuc.loaiVanBan().subscribe({
      next: (ds) => (this.dsLoaiVb = ds),
      // Loai van ban khong bat buoc - hong danh muc khong duoc chan viec nhap lieu.
      error: () => (this.dsLoaiVb = [])
    });

    this.danhMuc.linhVucPhang().subscribe({
      next: (ds) => (this.dsLinhVuc = ds),
      error: () => (this.dsLinhVuc = [])
    });
  }

  private napChiTiet(id: Guid): void {
    this.dangNap = true;
    this.vanBan.chiTiet(id).subscribe({
      next: (vb) => {
        this.dangNap = false;
        this.dat(vb);
      },
      error: (e: unknown) => {
        this.dangNap = false;
        this.tb.loi(e, 'Không tải được văn bản');
      }
    });
  }

  private dat(vb: VanBan): void {
    this.form.setValue({
      sokyhieu: vb.sokyhieu ?? '',
      ngaybanhanh: (vb.ngaybanhanh ?? '').substring(0, 10),
      loaivb: vb.loaivb ?? '',
      coquanbanhanh: vb.coquanbanhanh ?? '',
      trichyeu: vb.trichyeu ?? '',
      dokhan: vb.dokhan || DoKhan.ThuongXuyen,
      linhvuc: vb.linhvuc ?? '',
      nguoitheodoi: vb.nguoitheodoi ?? '',
      thoigianchidao: (vb.thoigianchidao ?? '').substring(0, 16)
    });
    this.dsTep = vb.files ?? [];
  }
}
