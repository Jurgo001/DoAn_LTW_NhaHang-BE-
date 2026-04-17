using DoAn_LTW_NhaHang.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity; // Cần thiết cho Include nếu bạn dùng ở nơi khác

namespace DoAn_LTW_NhaHang.Controllers
{
    public class CartController : Controller
    {
        QL_NhaHangEntities db = new QL_NhaHangEntities();

        // 1. LẤY GIỎ HÀNG (Helper)
        public Cart GetCart()
        {
            Cart cart = Session["Cart"] as Cart;
            if (cart == null || Session["Cart"] == null)
            {
                cart = new Cart();
                Session["Cart"] = cart;
            }
            return cart;
        }

        // 2. HIỂN THỊ GIỎ HÀNG
        public ActionResult Index()
        {
            Cart cart = GetCart();
            return View(cart);
        }

        // 3. CÁC HÀM XỬ LÝ GIỎ HÀNG (Thêm, Sửa, Xóa...)
        public ActionResult AddToCart(int id, string strURL)
        {
            if (Session["User"] == null)
            {
                Session["idSanPhamCanThem"] = id;
                Session["urlTraVe"] = strURL;
                return RedirectToAction("Login", "User");
            }
            Cart cart = GetCart();
            int result = cart.Tang(id);
            if (result == -1) ViewBag.Error = "Lỗi thêm giỏ hàng";
            else Session["Cart"] = cart;
            if (!string.IsNullOrEmpty(strURL)) return Redirect(strURL);
            return RedirectToAction("Index", "Home");
        }

        public ActionResult UpDateSL(int id, int type)
        {
            Cart cart = GetCart();
            if (type == 0) cart.Tang(id); else cart.Giam(id);
            return RedirectToAction("Index", "Cart");
        }

        public ActionResult ReMove(int id)
        {
            Cart cart = GetCart();
            cart.Xoa(id);
            return RedirectToAction("Index", "Cart");
        }

        // 4. AJAX: ÁP DỤNG VOUCHER
        [HttpPost]
        public ActionResult ApDungVoucher(string maCode)
        {
            if (Session["User"] == null) return Json(new { success = false, msg = "Vui lòng đăng nhập!" });

            var user = (tblKhachHang)Session["User"];

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // 1. Tìm lịch sử đổi voucher của khách hàng này
                    var voucherHistory = db.tblLichSuDoiDiems.FirstOrDefault(x => x.MaCode == maCode && x.MaKH == user.MaKH);

                    if (voucherHistory == null) return Json(new { success = false, msg = "Mã Voucher không tồn tại hoặc không thuộc sở hữu của bạn!" });
                    if (voucherHistory.TrangThai == true) return Json(new { success = false, msg = "Voucher đã được sử dụng!" });

                    // 2. Tìm thông tin Voucher gốc
                    var voucherGoc = db.tblVouchers.Find(voucherHistory.MaVoucher);

                    if (voucherGoc == null) return Json(new { success = false, msg = "Lỗi hệ thống: Không tìm thấy Voucher gốc." });
                    if (voucherGoc.NgayHetHan < DateTime.Now) return Json(new { success = false, msg = "Voucher đã hết hạn!" });

                    // --- THỰC HIỆN LOGIC XÓA VOUCHER KHỎI DB NGAY LẬP TỨC ---

                    // XÓA voucher khỏi tblLichSuDoiDiems (theo yêu cầu)
                    db.tblLichSuDoiDiems.Remove(voucherHistory);

                    // Cập nhật lại số lượng Voucher trong kho (Tăng số lượng lên 1)
                    // LƯU Ý: Đây là logic đảo ngược nếu bạn xóa khỏi tblLichSuDoiDiems
                    if (voucherGoc.SoLuong != null)
                    {
                        voucherGoc.SoLuong++;
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    // Lưu giá trị giảm giá vào Session sau khi xóa thành công khỏi DB
                    Session["VoucherCode"] = maCode;
                    Session["GiamGia"] = voucherGoc.GiaTri;

                    return Json(new { success = true, msg = "Áp dụng thành công!", discount = voucherGoc.GiaTri });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    // Trả về lỗi server nếu không thể xóa/cập nhật DB
                    return Json(new { success = false, msg = "Lỗi xử lý DB khi áp dụng: " + ex.Message });
                }
            }
        }

        // ============================================================
        // 5. XỬ LÝ THANH TOÁN (Lưu Đơn hàng)
        // ============================================================
        [HttpPost]
        public ActionResult DatHang(int paymentMethod)
        {
            if (Session["User"] == null) return RedirectToAction("Login", "User");

            Cart cart = GetCart();
            if (cart == null || cart.list.Count == 0) return RedirectToAction("Index", "Home");

            List<CartItem> gioHang = cart.list;
            tblKhachHang kh = (tblKhachHang)Session["User"];

            decimal tongTien = cart.TongThanhTien();
            decimal giamGia = 0;
            if (Session["GiamGia"] != null) giamGia = (decimal)Session["GiamGia"];

            decimal tongTienSauGiam = tongTien - giamGia;
            if (tongTienSauGiam < 0) tongTienSauGiam = 0;

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // A. Lưu Hóa Đơn
                    tblHoaDon hd = new tblHoaDon();
                    hd.MaKH = kh.MaKH;
                    hd.NgayLap = DateTime.Now;
                    hd.TinhTrang = 1; // 1: Mới đặt
                    hd.DaThanhToan = false;
                    hd.TongTien = tongTienSauGiam;
                    hd.GhiChu = (paymentMethod == 1) ? "Thanh toán QR" : "Thanh toán tiền mặt";

                    db.tblHoaDons.Add(hd);
                    db.SaveChanges();

                    // B. Lưu Chi Tiết
                    foreach (var item in gioHang)
                    {
                        tblChiTietHoaDon ct = new tblChiTietHoaDon();
                        ct.MaHD = hd.MaHD;
                        ct.MaMon = item.MaSP;
                        ct.SoLuong = item.SoLuong;
                        ct.DonGia = item.DonGia;
                        db.tblChiTietHoaDons.Add(ct);
                    }

                    // C. KHÓA VOUCHER (Đánh dấu đã dùng)
                    if (Session["VoucherCode"] != null)
                    {
                        string code = Session["VoucherCode"].ToString();
                        var voucherUsed = db.tblLichSuDoiDiems.FirstOrDefault(x => x.MaCode == code && x.MaKH == kh.MaKH);
                        if (voucherUsed != null)
                        {
                            voucherUsed.TrangThai = true; // Cập nhật trạng thái
                        }
                    }

                    db.SaveChanges(); // Lưu thay đổi Chi tiết & Voucher
                    transaction.Commit(); // Hoàn tất giao dịch

                    // D. Xóa Session
                    Session["Cart"] = null;
                    Session["GiamGia"] = null;
                    Session["VoucherCode"] = null;

                    // E. CHUYỂN HƯỚNG SANG TRANG KẾT QUẢ
                    TempData["MaDonHang"] = hd.MaHD;
                    TempData["TongTien"] = tongTienSauGiam;
                    TempData["HinhThucTT"] = paymentMethod;

                    // Chuyển hướng đến Action PaymentSuccess
                    return RedirectToAction("PaymentSuccess");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Content("Lỗi thanh toán: " + ex.Message);
                }
            }
        }

        // ============================================================
        // 6. TRANG HIỂN THỊ KẾT QUẢ VÀ MÃ QR
        // ============================================================
        public ActionResult PaymentSuccess()
        {
            if (TempData["MaDonHang"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            int maHD = (int)TempData["MaDonHang"];
            decimal tongTien = (decimal)TempData["TongTien"];
            int hinhThuc = (int)TempData["HinhThucTT"];

            ViewBag.MaDonHang = maHD;
            ViewBag.TongTien = tongTien;
            ViewBag.HinhThucThanhToan = hinhThuc;

            var user = (tblKhachHang)Session["User"];
            ViewBag.TenKhach = user.TenKH;

            // TẠO LINK VIETQR (Đã sửa template)
            if (hinhThuc == 1)
            {
                string bankId = "MB";
                string accountNo = "0333820793";
                // Sửa lại thành compact2
                string template = "compact2";
                string content = "THANHTOAN " + maHD;

                string qrUrl = $"https://img.vietqr.io/image/{bankId}-{accountNo}-{template}.png?amount={tongTien}&addInfo={content}";

                ViewBag.LinkQR = qrUrl;
            }

            // Trả về View PayMent.cshtml
            return View("~/Views/Cart/PayMent.cshtml");
        }

        // 7. XÁC NHẬN THANH TOÁN QR (Đã sửa TinhTrang = 4 và thêm redirectUrl)
        [HttpPost]
        public ActionResult XacNhanThanhToan(int maHD)
        {
            if (Session["User"] == null)
            {
                return Json(new { success = false, msg = "Vui lòng đăng nhập lại!", redirectUrl = Url.Action("Login", "User") });
            }

            var hoaDon = db.tblHoaDons.Find(maHD);
            var user = (tblKhachHang)Session["User"];

            if (hoaDon == null || hoaDon.MaKH != user.MaKH)
            {
                return Json(new { success = false, msg = "Lỗi xác thực hóa đơn." });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    if (hoaDon.DaThanhToan == false)
                    {
                        hoaDon.DaThanhToan = true;
                        // Đã thay đổi thành TinhTrang = 4 (Phù hợp với công thức tính điểm của bạn)
                        hoaDon.TinhTrang = 4;

                        db.SaveChanges();
                        transaction.Commit();

                        return Json(new
                        {
                            success = true,
                            msg = "Xác nhận thanh toán thành công!",
                            redirectUrl = Url.Action("LichSuDonHang", "User")
                        });
                    }
                    else
                    {
                        // Giữ nguyên logic, nhưng trả về success = false nếu voucher đã dùng
                        return Json(new { success = false, msg = "Hóa đơn này đã được xác nhận thanh toán trước đó." });
                    }
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    return Json(new { success = false, msg = "Lỗi server khi cập nhật trạng thái." });
                }
            }
        }
    }
    
}