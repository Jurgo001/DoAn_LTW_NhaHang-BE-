using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoAn_LTW_NhaHang.Models;

namespace DoAn_LTW_NhaHang.Controllers
{
    public class NhanVienController : Controller
    {
        QL_NhaHangEntities db = new QL_NhaHangEntities();

        // --- 1. KIỂM TRA QUYỀN "CHỦ NHÀ HÀNG" ---
        public bool CheckQuyenChu()
        {
            // LƯU Ý QUAN TRỌNG: Phải dùng chuỗi "Admin" cho khớp với UserController
            var user = Session["Admin"] as tblNhanVien;

            if (user != null && user.VaiTro == 11)
            {
                return true;
            }
            return false;
        }

        // --- 2. DANH SÁCH NHÂN VIÊN ---
        public ActionResult Index()
        {
            if (!CheckQuyenChu())
            {
                TempData["Error"] = "Bạn không có quyền truy cập mục này!";
                return RedirectToAction("Index", "Admin");
            }

            // Load danh sách kèm tên chức vụ
            var listNV = db.tblNhanViens.Include(n => n.tblVaiTro).OrderByDescending(n => n.TenNV).ToList();
            return View(listNV);
        }

        // --- 3. XÓA NHIỀU NHÂN VIÊN (Action nhận từ Form Index) ---
        [HttpPost]
        public ActionResult DeleteMultiple(int[] selectedIds)
        {
            if (!CheckQuyenChu()) return RedirectToAction("Index", "Admin");

            if (selectedIds != null && selectedIds.Length > 0)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var currentUser = Session["Admin"] as tblNhanVien;
                        foreach (var id in selectedIds)
                        {
                            // Không cho phép tự xóa chính mình
                            if (currentUser != null && id == currentUser.MaNV) continue;

                            var nv = db.tblNhanViens.Find(id);
                            if (nv != null) db.tblNhanViens.Remove(nv);
                        }
                        db.SaveChanges();
                        transaction.Commit();
                        TempData["Message"] = $"Đã xóa {selectedIds.Length} nhân viên.";
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        TempData["Error"] = "Lỗi: Không thể xóa nhân viên đang có dữ liệu liên quan (Hóa đơn/Nhập kho)!";
                    }
                }
            }
            else
            {
                TempData["Error"] = "Vui lòng chọn ít nhất 1 nhân viên để xóa!";
            }
            return RedirectToAction("Index");
        }

        // --- 4. THÊM NHÂN VIÊN ---
        [HttpGet]
        public ActionResult Create()
        {
            if (!CheckQuyenChu()) return RedirectToAction("Index", "Admin");
            // Dùng MaVaiTro làm giá trị (Value)
            ViewBag.VaiTro = new SelectList(db.tblVaiTroes, "IDVaiTro", "TenVaiTro");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(tblNhanVien nv)
        {
            if (!CheckQuyenChu()) return RedirectToAction("Index", "Admin");

            if (ModelState.IsValid)
            {
                if (db.tblNhanViens.Any(x => x.Email == nv.Email))
                {
                    ModelState.AddModelError("", "Email này đã tồn tại!");
                }
                else
                {
                    db.tblNhanViens.Add(nv);
                    db.SaveChanges();
                    TempData["Message"] = "Thêm nhân viên thành công!";
                    return RedirectToAction("Index");
                }
            }
            ViewBag.VaiTro = new SelectList(db.tblVaiTroes, "IDVaiTro", "TenVaiTro", nv.VaiTro);
            return View(nv);
        }

        // --- 5. SỬA NHÂN VIÊN ---
        // GET: Admin/NhanVien/Edit/5
        [HttpGet]
        public ActionResult Edit(int id)
        {
            if (!CheckQuyenChu()) return RedirectToAction("Index", "Admin");

            var nv = db.tblNhanViens.Find(id);
            if (nv == null) return HttpNotFound();

            // --- BẮT BUỘC PHẢI CÓ DÒNG NÀY ĐỂ TRÁNH LỖI ---
            // Lấy danh sách chức vụ đưa vào ViewBag để DropDownList bên View hiển thị được
            ViewBag.VaiTro = new SelectList(db.tblVaiTroes, "IDVaiTro", "TenVaiTro", nv.VaiTro);
            // ----------------------------------------------

            return View(nv);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(tblNhanVien nv)
        {
            if (!CheckQuyenChu()) return RedirectToAction("Index", "Admin");

            if (ModelState.IsValid)
            {
                var nvGoc = db.tblNhanViens.Find(nv.MaNV);
                if (nvGoc != null)
                {
                    nvGoc.TenNV = nv.TenNV;
                    nvGoc.GioiTinh = nv.GioiTinh;
                    nvGoc.NamSinh = nv.NamSinh;
                    nvGoc.Email = nv.Email;
                    nvGoc.VaiTro = nv.VaiTro;

                    // Chỉ đổi mật khẩu nếu người dùng nhập mới
                    if (!string.IsNullOrEmpty(nv.MatKhau))
                    {
                        nvGoc.MatKhau = nv.MatKhau;
                    }

                    db.SaveChanges();
                    TempData["Message"] = "Cập nhật thành công!";
                    return RedirectToAction("Index");
                }
            }
            ViewBag.VaiTro = new SelectList(db.tblVaiTroes, "IDVaiTro", "TenVaiTro", nv.VaiTro);
            return View(nv);
        }
        // ==========================================
        // 6. QUẢN LÝ VOUCHER (CHỈ BOSS MỚI ĐƯỢC DÙNG)
        // ==========================================

        // A. Danh sách Voucher
        public ActionResult QuanLyVoucher()
        {
            // Kiểm tra quyền Boss
            if (!CheckQuyenChu()) return RedirectToAction("Index", "Admin");

            var listVoucher = db.tblVouchers.OrderByDescending(v => v.NgayHetHan).ToList();
            return View(listVoucher);
        }

        // B. Thêm Voucher (GET)
        [HttpGet]
        public ActionResult ThemVoucher()
        {
            if (!CheckQuyenChu()) return RedirectToAction("Index", "Admin");
            return View();
        }

        // B. Thêm Voucher (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemVoucher(tblVoucher voucher)
        {
            if (!CheckQuyenChu()) return RedirectToAction("Index", "Admin");

            if (ModelState.IsValid)
            {
                // Kiểm tra logic: Ngày hết hạn phải lớn hơn ngày hiện tại
                if (voucher.NgayHetHan <= DateTime.Now)
                {
                    ModelState.AddModelError("NgayHetHan", "Ngày hết hạn phải lớn hơn ngày hiện tại!");
                    return View(voucher);
                }

                db.tblVouchers.Add(voucher);
                db.SaveChanges();
                TempData["Message"] = "Thêm Voucher thành công!";
                return RedirectToAction("QuanLyVoucher");
            }
            return View(voucher);
        }

        // C. Sửa Voucher (GET)
        [HttpGet]
        public ActionResult SuaVoucher(int id)
        {
            if (!CheckQuyenChu()) return RedirectToAction("Index", "Admin");

            var voucher = db.tblVouchers.Find(id);
            if (voucher == null) return HttpNotFound();

            return View(voucher);
        }

        // C. Sửa Voucher (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaVoucher(tblVoucher voucher)
        {
            if (!CheckQuyenChu()) return RedirectToAction("Index", "Admin");

            if (ModelState.IsValid)
            {
                var voucherCu = db.tblVouchers.Find(voucher.MaVoucher);
                if (voucherCu != null)
                {
                    voucherCu.TenVoucher = voucher.TenVoucher;
                    voucherCu.GiaTri = voucher.GiaTri;
                    voucherCu.DiemDoi = voucher.DiemDoi;
                    voucherCu.SoLuong = voucher.SoLuong;
                    voucherCu.NgayHetHan = voucher.NgayHetHan;

                    // Nếu có cột Mô tả hoặc Hình ảnh thì cập nhật thêm ở đây
                    // voucherCu.MoTa = voucher.MoTa;

                    db.SaveChanges();
                    TempData["Message"] = "Cập nhật Voucher thành công!";
                    return RedirectToAction("QuanLyVoucher");
                }
            }
            return View(voucher);
        }

        // D. Xóa Voucher
        public ActionResult XoaVoucher(int id)
        {
            if (!CheckQuyenChu()) return RedirectToAction("Index", "Admin");

            var voucher = db.tblVouchers.Find(id);
            if (voucher != null)
            {
                // Kiểm tra xem đã có ai đổi voucher này chưa để tránh lỗi khóa ngoại
                bool daDung = db.tblLichSuDoiDiems.Any(ls => ls.MaVoucher == id);

                if (daDung)
                {
                    // Nếu đã có người đổi -> Chỉ ẩn đi hoặc thông báo lỗi (Ở đây chọn thông báo lỗi)
                    TempData["Error"] = "Không thể xóa Voucher này vì đã có khách hàng đổi!";
                }
                else
                {
                    db.tblVouchers.Remove(voucher);
                    db.SaveChanges();
                    TempData["Message"] = "Đã xóa Voucher thành công!";
                }
            }
            return RedirectToAction("QuanLyVoucher");
        }
        // ==========================================
        // 7. QUẢN LÝ KHÁCH HÀNG (THÊM, XÓA, SỬA)
        // ==========================================

        // A. Danh sách khách hàng
        public ActionResult QuanLyKhachHang()
        {
            if (!CheckQuyenChu()) return RedirectToAction("Index", "Admin");

            var listKH = db.tblKhachHangs.OrderByDescending(k => k.MaKH).ToList();
            return View(listKH);
        }

        // B. Thêm Khách Hàng (GET)
        [HttpGet]
        public ActionResult ThemKhachHang()
        {
            if (!CheckQuyenChu()) return RedirectToAction("Index", "Admin");
            return View();
        }

        // B. Thêm Khách Hàng (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemKhachHang(tblKhachHang kh)
        {
            if (!CheckQuyenChu()) return RedirectToAction("Index", "Admin");

            if (ModelState.IsValid)
            {
                // Kiểm tra Email trùng
                if (db.tblKhachHangs.Any(x => x.Email == kh.Email))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng!");
                    return View(kh);
                }

                // Gán ảnh mặc định nếu không chọn
                if (string.IsNullOrEmpty(kh.Avarta))
                {
                    kh.Avarta = "default_user.jpg";
                }

                db.tblKhachHangs.Add(kh);
                db.SaveChanges();
                TempData["Message"] = "Thêm khách hàng thành công!";
                return RedirectToAction("QuanLyKhachHang");
            }
            return View(kh);
        }

        // C. Sửa Khách Hàng (GET)
        [HttpGet]
        public ActionResult SuaKhachHang(int id)
        {
            if (!CheckQuyenChu()) return RedirectToAction("Index", "Admin");

            var kh = db.tblKhachHangs.Find(id);
            if (kh == null) return HttpNotFound();

            return View(kh);
        }

        // C. Sửa Khách Hàng (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaKhachHang(tblKhachHang kh)
        {
            if (!CheckQuyenChu()) return RedirectToAction("Index", "Admin");

            if (ModelState.IsValid)
            {
                var khCu = db.tblKhachHangs.Find(kh.MaKH);
                if (khCu != null)
                {
                    // Kiểm tra Email trùng (trừ chính nó ra)
                    if (db.tblKhachHangs.Any(x => x.Email == kh.Email && x.MaKH != kh.MaKH))
                    {
                        ModelState.AddModelError("Email", "Email này đã thuộc về người khác!");
                        return View(kh);
                    }

                    khCu.TenKH = kh.TenKH;
                    khCu.DienThoai = kh.DienThoai;
                    khCu.DiaChi = kh.DiaChi;
                    khCu.Email = kh.Email;

                    // Chỉ đổi mật khẩu nếu admin nhập vào ô mật khẩu mới
                    if (!string.IsNullOrEmpty(kh.MatKhau))
                    {
                        khCu.MatKhau = kh.MatKhau;
                    }

                    db.SaveChanges();
                    TempData["Message"] = "Cập nhật thông tin khách hàng thành công!";
                    return RedirectToAction("QuanLyKhachHang");
                }
            }
            return View(kh);
        }

        // D. Xóa Khách Hàng
        public ActionResult XoaKhachHang(int id)
        {
            if (!CheckQuyenChu()) return RedirectToAction("Index", "Admin");

            var kh = db.tblKhachHangs.Find(id);
            if (kh != null)
            {
                // Kiểm tra xem khách hàng có đơn hàng không
                bool coDonHang = db.tblHoaDons.Any(hd => hd.MaKH == id);

                if (coDonHang)
                {
                    TempData["Error"] = "Không thể xóa! Khách hàng này đã có lịch sử đặt hàng.";
                }
                else
                {
                    // Kiểm tra xem khách có lịch sử đổi điểm/voucher không
                    var lsDoiDiem = db.tblLichSuDoiDiems.Where(ls => ls.MaKH == id).ToList();
                    if (lsDoiDiem.Count > 0)
                    {
                        db.tblLichSuDoiDiems.RemoveRange(lsDoiDiem); // Xóa lịch sử voucher trước
                    }

                    db.tblKhachHangs.Remove(kh);
                    db.SaveChanges();
                    TempData["Message"] = "Đã xóa khách hàng thành công!";
                }
            }
            return RedirectToAction("QuanLyKhachHang");
        }
        // E. Xóa NHIỀU Khách Hàng (Action nhận từ Form Index)
        [HttpPost]
        public ActionResult XoaNhieuKhachHang(int[] selectedIds)
        {
            if (!CheckQuyenChu()) return RedirectToAction("Index", "Admin");

            if (selectedIds != null && selectedIds.Length > 0)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        int countSuccess = 0;
                        int countFail = 0;

                        foreach (var id in selectedIds)
                        {
                            var kh = db.tblKhachHangs.Find(id);
                            if (kh != null)
                            {
                                // 1. Kiểm tra xem khách có đơn hàng không
                                bool coDonHang = db.tblHoaDons.Any(hd => hd.MaKH == id);

                                if (coDonHang)
                                {
                                    // Nếu có đơn hàng -> Không được xóa -> Bỏ qua
                                    countFail++;
                                }
                                else
                                {
                                    // 2. Xóa lịch sử đổi điểm/voucher trước (nếu có)
                                    var lsDoiDiem = db.tblLichSuDoiDiems.Where(ls => ls.MaKH == id).ToList();
                                    if (lsDoiDiem.Count > 0)
                                    {
                                        db.tblLichSuDoiDiems.RemoveRange(lsDoiDiem);
                                    }

                                    // 3. Xóa khách hàng
                                    db.tblKhachHangs.Remove(kh);
                                    countSuccess++;
                                }
                            }
                        }

                        db.SaveChanges();
                        transaction.Commit();

                        if (countFail > 0)
                        {
                            TempData["Message"] = $"Đã xóa {countSuccess} khách hàng. Có {countFail} khách hàng không thể xóa do đã có đơn hàng.";
                        }
                        else
                        {
                            TempData["Message"] = $"Đã xóa thành công {countSuccess} khách hàng.";
                        }
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        TempData["Error"] = "Đã xảy ra lỗi trong quá trình xử lý dữ liệu!";
                    }
                }
            }
            else
            {
                TempData["Error"] = "Vui lòng chọn ít nhất 1 khách hàng để xóa!";
            }
            return RedirectToAction("QuanLyKhachHang");
        }
    }
}