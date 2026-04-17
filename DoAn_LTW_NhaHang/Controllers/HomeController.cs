using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DoAn_LTW_NhaHang.Controllers
{
    public class HomeController : Controller
    {
        QL_NhaHangEntities da = new QL_NhaHangEntities();

        //------------------------------------------------------------------------------------------------
        public ActionResult Index()
        {
            // 1. Lấy danh sách toàn bộ món (cho phần bên dưới)
            var allMonAn = da.tblMonAns.ToList();

            // 2. LOGIC TÍNH TOP 10 BÁN CHẠY (Copy từ TrangChu sang)
            var topMonAn = da.tblChiTietHoaDons
                .Where(ct => ct.tblHoaDon.TinhTrang == 4) // Đã thanh toán
                .GroupBy(ct => new {
                    ct.tblMonAn.MaMon,
                    ct.tblMonAn.TenMon,
                    ct.tblMonAn.AnhDaiDien,
                    ct.tblMonAn.DonGia,
                    ct.tblMonAn.MoTa,
                    ct.tblMonAn.MaLoai
                })
                .Select(g => new MonAnViewModel
                {
                    MaMon = g.Key.MaMon,
                    TenMon = g.Key.TenMon,
                    AnhDaiDien = g.Key.AnhDaiDien,
                    DonGia = g.Key.DonGia,
                    MoTa = g.Key.MoTa,
                    MaLoai = (int)g.Key.MaLoai,
                    TongSoLuongBan = g.Sum(x => x.SoLuong)
                })
                .OrderByDescending(x => x.TongSoLuongBan)
                .Take(10)
                .ToList();

            // 3. Gửi Top 10 sang View qua ViewBag
            ViewBag.TopMonAn = topMonAn;

            return View(allMonAn);
        }


        //------------------------------------------------------------------------------------------------
        public ActionResult _LoaiMon()
        {
            return PartialView(da.tblLoaiMons.ToList());
        }
        public ActionResult TimTheoMonAn(int id)
        {
            List<tblMonAn> list = da.tblMonAns.Where(x => x.MaLoai == id).ToList();
            return View("Index", list);
        }
        //------------------------------------------------------------------------------------------------
        public ActionResult TimKiem(string key)
        {
            var list = new List<tblMonAn>();

            if (!string.IsNullOrEmpty(key))
            {
                list = da.tblMonAns
                         .Where(x => x.TenMon.ToLower().Contains(key.ToLower()))
                         .ToList();
            }
            else
            {
                list = da.tblMonAns.ToList();
            }
            ViewBag.TuKhoa = key;
            return View("Index", list);
        }

        //------------------------------------------------------------------------------------------------
        public ActionResult Detail(int id)
        {
            tblMonAn sp = da.tblMonAns.FirstOrDefault(x => x.MaMon == id);
            List<tblMonAn> SPLQ = da.tblMonAns.Where(x => x.MaLoai == sp.MaLoai && x.MaMon != sp.MaMon).ToList();
            ViewBag.SPLQ = SPLQ;
            return View(sp);
        }
        [HttpPost]
        public ActionResult DatBan(string TenKhachHang, string Email, string ThoiGian, int SoNguoi, string GhiChu)
        {
            // Giả sử da là QL_NhaHangEntities đã được khai báo ở phạm vi class
            // QL_NhaHangEntities da = new QL_NhaHangEntities(); 

            try
            {
                // 1. Tạo đối tượng mới
                tblDatBan newBooking = new tblDatBan();
                newBooking.TenKhachHang = TenKhachHang;
                newBooking.Email = Email;
                newBooking.SoNguoi = SoNguoi;
                newBooking.GhiChu = GhiChu;
                newBooking.TrangThai = 0; // Mặc định là mới đặt

                // Xử lý ngày giờ
                if (DateTime.TryParse(ThoiGian, out DateTime date))
                {
                    newBooking.NgayDat = date;
                }
                else
                {
                    // Báo lỗi cụ thể nếu không parse được thời gian
                    TempData["Error"] = "Lỗi định dạng ngày/giờ. Vui lòng kiểm tra lại!";
                    return RedirectToAction("TrangChu");
                }

                // 2. Kiểm tra ràng buộc dữ liệu (Optional)
                if (string.IsNullOrEmpty(newBooking.TenKhachHang) || newBooking.SoNguoi <= 0)
                {
                    TempData["Error"] = "Vui lòng điền đủ thông tin Tên và Số khách.";
                    return RedirectToAction("TrangChu");
                }

                // 3. Lưu vào CSDL
                da.tblDatBans.Add(newBooking);
                da.SaveChanges();

                // 4. Thông báo thành công 
                TempData["Message"] = "Đặt bàn thành công! Chúng tôi đã lưu đơn và sẽ liên hệ sớm.";
            }
            catch (Exception ex) // Bắt lỗi tổng quát hơn để xem lỗi gì
            {
                // Lỗi này có thể là lỗi kết nối CSDL, lỗi khóa ngoại, hoặc lỗi model rỗng
                // In ra lỗi cụ thể hơn để dễ debug
                TempData["Error"] = "Lỗi hệ thống: Không thể lưu đơn. Chi tiết: " + ex.Message;
            }

            // Quay lại trang chủ
            return RedirectToAction("TrangChu");
        }
        public class MonAnViewModel
        {
            public int MaMon { get; set; }
            public int MaLoai { get; set; }
            public string TenMon { get; set; }
            public string AnhDaiDien { get; set; }
            public decimal? DonGia { get; set; }
            public string MoTa
            { get; set; }
            public int? TongSoLuongBan { get; set; }
        }

        public ActionResult TrangChu()
        {
            // Lấy danh sách Loại món để tạo Tab
            ViewBag.DanhSachLoai = da.tblLoaiMons.ToList();

            // Lấy TOP 10 MÓN ĂN BÁN CHẠY NHẤT
            var topMonAn = da.tblChiTietHoaDons
                // Lọc theo đơn hàng Đã thanh toán (TinhTrang == 4, theo logic CartController)
                .Where(ct => ct.tblHoaDon.TinhTrang == 4)
                // Gom nhóm theo Món ăn, tên, ảnh, giá, mô tả, và loại món
                .GroupBy(ct => new { ct.tblMonAn.MaMon, ct.tblMonAn.TenMon, ct.tblMonAn.AnhDaiDien, ct.tblMonAn.DonGia, ct.tblMonAn.MoTa, ct.tblMonAn.MaLoai })
                .Select(g => new MonAnViewModel // Sử dụng ViewModel đã định nghĩa
                {
                    MaMon = g.Key.MaMon,
                    TenMon = g.Key.TenMon,
                    AnhDaiDien = g.Key.AnhDaiDien,
                    DonGia = g.Key.DonGia,
                    MoTa= g.Key.MoTa,
                    MaLoai = (int)g.Key.MaLoai,
                    TongSoLuongBan = g.Sum(x => x.SoLuong)
                })
                .OrderByDescending(x => x.TongSoLuongBan)
                .Take(10)
                .ToList();

            // Truyền dữ liệu Top Món ăn sang View bằng ViewBag
            ViewBag.TopMonAn = topMonAn;

            return View(); // Trả về TrangChu.cshtml
        }
        // -------------------------------------------------------------------------
        // CHỨC NĂNG LIÊN HỆ
        // -------------------------------------------------------------------------

        // 1. Hiển thị trang Liên Hệ (GET)
        public ActionResult LienHe()
        {
            return View();
        }

        // 2. Xử lý gửi tin nhắn liên hệ (POST - Gọi Ajax)
        [HttpPost]
        public ActionResult GuiLienHe(string hoTen, string email, string sdt, string chuDe, string noiDung)
        {
            try
            {
                // -- LƯU VÀO DATABASE --
                // Tạo đối tượng Liên Hệ mới (Đảm bảo bạn đã có class tblLienHe trong Model)
                tblLienHe lh = new tblLienHe();
                lh.TenNguoiGui = hoTen;
                lh.Email = email;
                lh.SoDienThoai = sdt;
                lh.ChuDe = chuDe;
                lh.NoiDung = noiDung;
                lh.NgayGui = DateTime.Now;
                lh.TrangThai = false; // Mặc định là chưa xem/chưa xử lý

                da.tblLienHes.Add(lh);
                da.SaveChanges();

                // Trả về JSON để Ajax bên View xử lý hiển thị thông báo
                return Json(new { success = true, msg = "Cảm ơn bạn! Chúng tôi đã nhận được tin nhắn và sẽ phản hồi sớm nhất." });
            }
            catch (Exception ex)
            {
                // Nếu lỗi, trả về thông báo lỗi
                return Json(new { success = false, msg = "Có lỗi xảy ra khi gửi tin: " + ex.Message });
            }
        }
    }
}