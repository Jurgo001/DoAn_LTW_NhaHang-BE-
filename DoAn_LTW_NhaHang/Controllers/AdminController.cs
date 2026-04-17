using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoAn_LTW_NhaHang.Models; // Nhớ using Models

namespace DoAn_LTW_NhaHang.Controllers
{
    public class AdminController : Controller
    {
        QL_NhaHangEntities db = new QL_NhaHangEntities();

        // Hàm kiểm tra xem đã đăng nhập Admin chưa
        public bool CheckAdmin()
        {
            if (Session["Admin"] == null) return false;
            return true;
        }
        // --- CHỨC NĂNG SỬA LOẠI MÓN ---

        // 1. Hiển thị form sửa (GET)
        [HttpGet]
        public ActionResult SuaLoai(int id)
        {
            if (!CheckAdmin()) return RedirectToAction("Login", "User");

            // Tìm loại món theo ID
            var loai = db.tblLoaiMons.Find(id);
            if (loai == null)
            {
                TempData["Error"] = "Không tìm thấy loại món này!";
                return RedirectToAction("QuanLyLoai");
            }

            return View(loai);
        }

        // 2. Lưu thông tin sửa (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaLoai(tblLoaiMon loai)
        {
            if (!CheckAdmin()) return RedirectToAction("Login", "User");

            if (ModelState.IsValid)
            {
                // Tìm đối tượng cũ trong DB
                var loaiCu = db.tblLoaiMons.Find(loai.MaLoai);
                if (loaiCu != null)
                {
                    // Cập nhật thông tin mới
                    loaiCu.TenLoai = loai.TenLoai;

                    // Kiểm tra xem Model của bạn có thuộc tính MoTa không (dựa trên View bạn gửi là có)
                    loaiCu.MoTa = loai.MoTa;

                    db.SaveChanges();
                    TempData["Message"] = "Cập nhật loại món thành công!";
                    return RedirectToAction("QuanLyLoai");
                }
                else
                {
                    ModelState.AddModelError("", "Loại món không tồn tại.");
                }
            }
            return View(loai);
        }
        // 1. Danh sách món ăn (Trang chủ Admin)
        public ActionResult Index(int? maLoai)
        {
            if (!CheckAdmin()) return RedirectToAction("Login", "User");

            // Lấy danh sách loại món để đổ vào Dropdown lọc
            ViewBag.ListLoai = db.tblLoaiMons.ToList();
            ViewBag.MaLoaiHienTai = maLoai; // Để giữ lại giá trị đang chọn trên View

            // Truy vấn cơ bản
            var query = db.tblMonAns.AsQueryable();

            // Nếu có chọn loại (maLoai khác null) thì lọc
            if (maLoai.HasValue)
            {
                query = query.Where(n => n.MaLoai == maLoai.Value);
            }

            // Sắp xếp món mới nhất lên đầu và lấy dữ liệu
            var listMon = query.OrderByDescending(n => n.MaMon).ToList();

            return View(listMon);
        }

        // ==========================================
        // HÀM MỚI: XÓA NHIỀU MÓN CÙNG LÚC
        // ==========================================
        [HttpPost]
        public ActionResult DeleteMultiple(int[] selectedIds)
        {
            if (!CheckAdmin()) return RedirectToAction("Login", "User");

            if (selectedIds != null && selectedIds.Length > 0)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        foreach (var id in selectedIds)
                        {
                            var mon = db.tblMonAns.Find(id);
                            if (mon != null)
                            {
                                // 1. Xóa hình ảnh liên quan trước
                                var hinhAnhs = db.tblHinhAnhs.Where(h => h.MaMon == id).ToList();
                                db.tblHinhAnhs.RemoveRange(hinhAnhs);

                                // 2. Xóa chi tiết hóa đơn liên quan (Nếu muốn xóa triệt để - Cẩn thận bước này)
                                // var chiTietHD = db.tblChiTietHoaDons.Where(c => c.MaMon == id).ToList();
                                // db.tblChiTietHoaDons.RemoveRange(chiTietHD);

                                // 3. Xóa món ăn
                                db.tblMonAns.Remove(mon);
                            }
                        }
                        db.SaveChanges();
                        transaction.Commit();
                        TempData["Message"] = $"Đã xóa thành công {selectedIds.Length} món ăn.";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        TempData["Error"] = "Lỗi khi xóa: Một số món đang có trong đơn hàng cũ, không thể xóa!";
                    }
                }
            }
            else
            {
                TempData["Error"] = "Bạn chưa chọn món nào để xóa!";
            }

            return RedirectToAction("Index");
        }

        // 2. Thêm món ăn mới (GET - Hiển thị form)
        [HttpGet]
        public ActionResult Create()
        {
            if (!CheckAdmin()) return RedirectToAction("Login", "User");

            // Tạo Dropdown cho Loại món và Nhà cung cấp
            ViewBag.MaLoai = new SelectList(db.tblLoaiMons, "MaLoai", "TenLoai");
            ViewBag.MaNCC = new SelectList(db.tblNCCs, "MaNCC", "TenNCC");
            return View();
        }

        // 2. Thêm món ăn mới (POST - Xử lý dữ liệu)
        [HttpPost]
        [ValidateInput(false)] // Cho phép nhập HTML trong mô tả nếu cần
        public ActionResult Create(tblMonAn mon, HttpPostedFileBase fileUpload)
        {
            if (!CheckAdmin()) return RedirectToAction("Login", "User");

            // Xử lý ảnh đại diện
            if (fileUpload == null)
            {
                ViewBag.ThongBao = "Vui lòng chọn ảnh bìa";
                ViewBag.MaLoai = new SelectList(db.tblLoaiMons, "MaLoai", "TenLoai");
                ViewBag.MaNCC = new SelectList(db.tblNCCs, "MaNCC", "TenNCC");
                return View();
            }
            else
            {
                if (ModelState.IsValid)
                {
                    // Lưu tên file
                    var fileName = Path.GetFileName(fileUpload.FileName);
                    // Lưu đường dẫn
                    var path = Path.Combine(Server.MapPath("~/Content/Images"), fileName);

                    // Kiểm tra ảnh đã tồn tại chưa
                    if (System.IO.File.Exists(path))
                    {
                        ViewBag.ThongBao = "Hình ảnh đã tồn tại";
                    }
                    else
                    {
                        fileUpload.SaveAs(path); // Lưu ảnh vào folder
                    }

                    mon.AnhDaiDien = fileName; // Lưu tên ảnh vào CSDL
                    db.tblMonAns.Add(mon);
                    db.SaveChanges();
                }
            }
            return RedirectToAction("Index");
        }

        // 3. Chỉnh sửa món ăn (GET - Hiển thị form cũ)
        [HttpGet]
        public ActionResult Edit(int id)
        {
            if (!CheckAdmin()) return RedirectToAction("Login", "User");

            var mon = db.tblMonAns.SingleOrDefault(n => n.MaMon == id);
            if (mon == null) return HttpNotFound();

            ViewBag.MaLoai = new SelectList(db.tblLoaiMons, "MaLoai", "TenLoai", mon.MaLoai);
            ViewBag.MaNCC = new SelectList(db.tblNCCs, "MaNCC", "TenNCC", mon.MaNCC);
            return View(mon);
        }

        // 3. Chỉnh sửa món ăn (POST - Lưu thay đổi)
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(tblMonAn mon, HttpPostedFileBase fileUpload)
        {
            if (!CheckAdmin()) return RedirectToAction("Login", "User");

            var monUpdate = db.tblMonAns.SingleOrDefault(n => n.MaMon == mon.MaMon);
            if (monUpdate != null)
            {
                // Cập nhật thông tin cơ bản
                monUpdate.TenMon = mon.TenMon;
                monUpdate.DonGia = mon.DonGia; // Chỉnh giá ở đây
                monUpdate.MoTa = mon.MoTa;
                monUpdate.MaLoai = mon.MaLoai;
                monUpdate.MaNCC = mon.MaNCC;

                // Nếu có chọn ảnh mới thì cập nhật, không thì giữ nguyên ảnh cũ
                if (fileUpload != null)
                {
                    var fileName = Path.GetFileName(fileUpload.FileName);
                    var path = Path.Combine(Server.MapPath("~/Content/Images"), fileName);
                    fileUpload.SaveAs(path);
                    monUpdate.AnhDaiDien = fileName;
                }

                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // 4. Xóa món ăn
        public ActionResult Delete(int id)
        {
            if (!CheckAdmin()) return RedirectToAction("Login", "User");

            var mon = db.tblMonAns.SingleOrDefault(n => n.MaMon == id);
            if (mon == null) return HttpNotFound();

            // Xóa ảnh liên quan trong bảng tblHinhAnh trước (nếu có) để tránh lỗi khóa ngoại
            var hinhAnhs = db.tblHinhAnhs.Where(h => h.MaMon == id).ToList();

            // 2. Xóa từng hình (Cách thủ công an toàn)
            foreach (var hinh in hinhAnhs)
            {
                db.tblHinhAnhs.Remove(hinh);
            }

            // Sau đó mới xóa món ăn
            db.tblMonAns.Remove(mon);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        public ActionResult QuanLyLoai()
        {
            if (!CheckAdmin()) return RedirectToAction("Login", "User");
            return View(db.tblLoaiMons.ToList());
        }

        [HttpGet]
        public ActionResult ThemMoiLoai()
        {
            if (!CheckAdmin()) return RedirectToAction("Login", "User");
            return View();
        }

        [HttpPost]
        public ActionResult ThemMoiLoai(tblLoaiMon loai)
        {
            if (!CheckAdmin()) return RedirectToAction("Login", "User");
            if (ModelState.IsValid)
            {
                db.tblLoaiMons.Add(loai);
                db.SaveChanges();
                return RedirectToAction("QuanLyLoai");
            }
            return View(loai);
        }

        public ActionResult XoaLoai(int id)
        {
            if (!CheckAdmin()) return RedirectToAction("Login", "User");
            var loai = db.tblLoaiMons.Find(id);
            if (loai != null)
            {
                // Kiểm tra xem loại này có món ăn nào chưa, nếu có thì không cho xóa để tránh lỗi
                if (db.tblMonAns.Any(x => x.MaLoai == id))
                {
                    TempData["Error"] = "Không thể xóa loại này vì đang có món ăn thuộc loại này!";
                }
                else
                {
                    db.tblLoaiMons.Remove(loai);
                    db.SaveChanges();
                }
            }
            return RedirectToAction("QuanLyLoai");
        }

        // ==========================================
        // 2. QUẢN LÝ ĐƠN HÀNG
        // ==========================================
        // Trong AdminController.cs

        // --- 2. QUẢN LÝ ĐƠN HÀNG (CÓ LỌC & TÌM KIẾM) ---
        [HttpGet]
        public ActionResult QuanLyDonHang(int? status, string keyword)
        {
            if (!CheckAdmin()) return RedirectToAction("Login", "User");

            // 1. Khởi tạo truy vấn
            var query = db.tblHoaDons.AsQueryable();

            // 2. Lọc theo trạng thái (status) nếu có chọn
            // status = 1 (Chờ xác nhận), 2 (Đang làm), 3 (Đang giao), 4 (Hoàn thành), 5 (Hủy)
            if (status.HasValue)
            {
                query = query.Where(n => n.TinhTrang == status.Value);
            }

            // 3. Tìm kiếm theo tên khách hoặc mã đơn (keyword)
            if (!string.IsNullOrEmpty(keyword))
            {
                // Chuyển keyword về chữ thường để tìm không phân biệt hoa thường
                string k = keyword.ToLower();

                // Tìm trong Tên Khách Hàng HOẶC Mã Hóa Đơn (ép kiểu int để so sánh)
                query = query.Where(n => n.tblKhachHang.TenKH.ToLower().Contains(k)
                                      || n.MaHD.ToString().Contains(k));
            }

            // 4. Sắp xếp: Đơn mới nhất lên đầu
            var dsDonHang = query.OrderByDescending(n => n.NgayLap).ToList();

            // 5. Lưu lại giá trị đã lọc để hiển thị lại trên View (giữ trạng thái nút active)
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentKeyword = keyword;

            return View(dsDonHang);
        }

        // Xem chi tiết để duyệt đơn
        public ActionResult DuyetDonHang(int id)
        {
            if (!CheckAdmin()) return RedirectToAction("Login", "User");
            var donHang = db.tblHoaDons.Find(id);
            if (donHang == null) return HttpNotFound();

            // Lấy danh sách trạng thái để hiển thị Dropdown hoặc Radio
            ViewBag.TrangThaiList = db.tblTinhTrangs.ToList();
            return View(donHang);
        }

        [HttpPost]
        public ActionResult CapNhatTrangThai(int MaHD, int MaTinhTrang)
        {
            if (!CheckAdmin()) return RedirectToAction("Login", "User");
            var donHang = db.tblHoaDons.Find(MaHD);
            if (donHang != null)
            {
                donHang.TinhTrang = MaTinhTrang;

                // Nếu trạng thái là "Đã thanh toán" hoặc "Hoàn thành" (ví dụ ID 4)
                if (MaTinhTrang == 4)
                {
                    donHang.DaThanhToan = true;
                }

                db.SaveChanges();
            }
            return RedirectToAction("QuanLyDonHang");
        }

        // ==========================================
        // 3. THỐNG KÊ DOANH THU
        // ==========================================
        [HttpGet]
        public ActionResult ThongKe(string fromDate, string toDate)
        {
            // 1. Lấy thông tin user từ Session
            var user = Session["Admin"] as tblNhanVien;

            // 2. Kiểm tra đăng nhập (Chỉ cần có Session là được, không phân biệt Boss hay Nhân viên)
            if (user == null)
            {
                return RedirectToAction("Login", "User");
            }

            // Bỏ qua đoạn kiểm tra VaiTro == 11 ở đây (nếu có).
            // Vì Boss (11) cũng cần xem thống kê.

            // --- PHẦN LOGIC THỐNG KÊ GIỮ NGUYÊN ---
            decimal tongDoanhThu = db.tblHoaDons
                .Where(n => n.DaThanhToan == true)
                .Sum(n => n.TongTien) ?? 0;

            int donMoi = db.tblHoaDons.Count(n => n.TinhTrang == 1);

            ViewBag.TongDoanhThu = tongDoanhThu;
            ViewBag.DonMoi = donMoi;

            DateTime start, end;
            if (string.IsNullOrEmpty(fromDate) || string.IsNullOrEmpty(toDate))
            {
                start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                end = DateTime.Now;
            }
            else
            {
                start = DateTime.Parse(fromDate);
                end = DateTime.Parse(toDate).AddDays(1).AddSeconds(-1);
            }

            ViewBag.Start = start.ToString("yyyy-MM-dd");
            ViewBag.End = end.ToString("yyyy-MM-dd");

            ThongKeViewModel model = new ThongKeViewModel();

            model.DoanhThuTheoNgay = db.tblHoaDons
                .Where(x => x.NgayLap >= start && x.NgayLap <= end && x.DaThanhToan == true)
                .GroupBy(x => System.Data.Entity.DbFunctions.TruncateTime(x.NgayLap))
                .Select(g => new DoanhThuNgay
                {
                    Ngay = g.Key.Value,
                    DoanhThu = g.Sum(x => x.TongTien) ?? 0,
                    SoDonHang = g.Count()
                })
                .OrderBy(x => x.Ngay).ToList();

            model.DoanhThuTheoLoai = db.tblChiTietHoaDons
                .Where(ct => ct.tblHoaDon.NgayLap >= start
                            && ct.tblHoaDon.NgayLap <= end
                            && ct.tblHoaDon.DaThanhToan == true)
                .GroupBy(ct => ct.tblMonAn.tblLoaiMon.TenLoai)
                .Select(g => new ThongKeLoai
                {
                    TenLoai = g.Key,
                    SoLuongBan = g.Sum(x => x.SoLuong) ?? 0,
                    TongTien = g.Sum(x => x.SoLuong * x.DonGia) ?? 0
                }).ToList();

            model.TopMonAn = db.tblChiTietHoaDons
                .Where(ct => ct.tblHoaDon.NgayLap >= start
                            && ct.tblHoaDon.NgayLap <= end
                            && ct.tblHoaDon.DaThanhToan == true)
                .GroupBy(ct => new { ct.tblMonAn.TenMon, ct.tblMonAn.AnhDaiDien })
                .Select(g => new MonAnBanChay
                {
                    TenMon = g.Key.TenMon,
                    HinhAnh = g.Key.AnhDaiDien,
                    SoLuongBan = g.Sum(x => x.SoLuong) ?? 0,
                    TongTien = g.Sum(x => x.SoLuong * x.DonGia) ?? 0 
                })
                .OrderByDescending(x => x.SoLuongBan)
                .Take(10).ToList();

            return View(model);
        }

    }
}