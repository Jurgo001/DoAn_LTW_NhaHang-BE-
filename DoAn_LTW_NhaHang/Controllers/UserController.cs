using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoAn_LTW_NhaHang.Models;
using System.Net;
using System.Net.Mail;

namespace DoAn_LTW_NhaHang.Controllers
{
    public class UserController : Controller
    {
        QL_NhaHangEntities da = new QL_NhaHangEntities();

        // GET: User/Login
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult LoginOnSubMit(FormCollection collect)
        {
            var email = collect["Email"];
            var pass = collect["MatKhau"];

            // 1. Kiểm tra Khách hàng
            tblKhachHang kh = da.tblKhachHangs.FirstOrDefault(x => x.Email == email && x.MatKhau == pass);

            if (kh != null)
            {
                // Lưu session đăng nhập
                Session["User"] = kh;
                Session["HoTen"] = kh.TenKH;

                // --- [PHẦN SỬA ĐỔI QUAN TRỌNG] ---
                // Kiểm tra xem có món ăn nào đang chờ được thêm vào giỏ không
                if (Session["idSanPhamCanThem"] != null)
                {
                    // Lấy lại ID món và URL cũ từ Session
                    int idMon = (int)Session["idSanPhamCanThem"];
                    string urlCu = Session["urlTraVe"] as string;

                    // Xóa Session chờ để tránh lặp lại lần sau
                    Session["idSanPhamCanThem"] = null;
                    Session["urlTraVe"] = null;

                    // Chuyển hướng sang CartController để thực hiện hành động Thêm Giỏ Hàng
                    return RedirectToAction("AddToCart", "Cart", new { id = idMon, strURL = urlCu });
                }
                // -----------------------------------

                return RedirectToAction("Index", "Home");
            }

            // 2. Kiểm tra Nhân viên (Admin)
            tblNhanVien nv = da.tblNhanViens.FirstOrDefault(x => x.Email == email && x.MatKhau == pass);
            if (nv != null)
            {
                Session["Admin"] = nv; // Bạn đang dùng tên Session là "Admin" -> ĐÚNG
                Session["HoTen"] = nv.TenNV;
                Session["VaiTro"] = nv.VaiTro;

                // --- PHÂN QUYỀN CHUYỂN HƯỚNG ---
                // Giả sử mã 11 là Ông Chủ (dựa theo code NhanVienController cũ của bạn)
                if (nv.VaiTro == 11)
                {
                    // Nếu là Boss -> Chuyển sang trang Quản lý Nhân Viên (Nơi có Layout Boss)
                    return RedirectToAction("Index", "NhanVien");
                }
                else
                {
                    // Nếu là Nhân viên thường -> Chuyển sang trang Quản lý Món Ăn
                    return RedirectToAction("Index", "Admin");
                }
            }

            // 3. Đăng nhập thất bại
            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng!";
            return View("Login");
        }

        // Đăng xuất
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login", "User");
        }

        // Đăng ký (GET)
        public ActionResult Register()
        {
            return View();
        }

        // Đăng ký (POST)
        [HttpPost]
        public ActionResult RegisterUser(tblKhachHang _user, FormCollection collection)
        {
            var matKhauXacNhan = collection["MatKhauXacNhan"];
            var email = _user.Email;

            // Kiểm tra Email tồn tại
            var checkEmail = da.tblKhachHangs.FirstOrDefault(x => x.Email == email);
            if (checkEmail != null)
            {
                ViewBag.Error = "Email này đã được đăng ký! Vui lòng dùng email khác.";
                return View("Register");
            }

            // Kiểm tra mật khẩu xác nhận
            if (_user.MatKhau != matKhauXacNhan)
            {
                ViewBag.Error = "Mật khẩu xác nhận không trùng khớp!";
                return View("Register");
            }

            // Gán dữ liệu mặc định và lưu
            _user.Avarta = "default_user.jpg";
            da.tblKhachHangs.Add(_user);
            da.SaveChanges();

            TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }
        // 1. Xem danh sách các đơn hàng đã đặt
        public ActionResult LichSuDonHang()
        {
            // Kiểm tra đăng nhập
            if (Session["User"] == null) return RedirectToAction("Login");

            // Lấy thông tin khách hàng từ Session
            tblKhachHang kh = (tblKhachHang)Session["User"];

            // Lấy danh sách hóa đơn của khách đó, sắp xếp ngày mới nhất lên đầu
            var listHD = da.tblHoaDons
                            .Where(n => n.MaKH == kh.MaKH)
                            .OrderByDescending(n => n.NgayLap)
                            .ToList();

            return View(listHD);
        }

        // 2. Xem chi tiết từng món trong một đơn hàng cụ thể
        public ActionResult ChiTietDonHang(int id)
        {
            if (Session["User"] == null) return RedirectToAction("Login");

            // Tìm đơn hàng theo ID
            var donHang = da.tblHoaDons.FirstOrDefault(n => n.MaHD == id);

            // Kiểm tra bảo mật: Khách hàng chỉ được xem đơn của chính mình
            tblKhachHang kh = (tblKhachHang)Session["User"];
            if (donHang == null || donHang.MaKH != kh.MaKH)
            {
                return RedirectToAction("LichSuDonHang");
            }

            return View(donHang);
        }
        [HttpGet]
        public ActionResult HoSoCaNhan()
        {
            if (Session["User"] == null) return RedirectToAction("Login");

            var sessionUser = (tblKhachHang)Session["User"];
            var user = da.tblKhachHangs.Find(sessionUser.MaKH);

            // Tính số đơn hàng
            int soDonHang = da.tblHoaDons.Count(n => n.MaKH == user.MaKH);

            // --- SỬA ĐỔI: GỌI HÀM TÍNH ĐIỂM MỚI ---
            int diemKhaDung = TinhDiemKhaDung(user.MaKH);

            // Lấy danh sách Voucher khách đang sở hữu
            var myVouchers = da.tblLichSuDoiDiems
                               .Where(v => v.MaKH == user.MaKH)
                               .OrderByDescending(v => v.NgayDoi)
                               .ToList();

            ViewBag.SoDonHang = soDonHang;
            ViewBag.DiemTichLuy = diemKhaDung; // Hiển thị điểm thật
            ViewBag.MyVouchers = myVouchers;   // Truyền danh sách voucher đã đổi sang View

            return View(user);
        }

        // 2. Cập nhật thông tin (Nếu bạn muốn cho khách sửa)
        [HttpPost]
        // Thêm tham số HttpPostedFileBase ImageUpload để nhận file từ View
        public ActionResult CapNhatHoSo(tblKhachHang formData, HttpPostedFileBase ImageUpload)
        {
            if (Session["User"] == null) return RedirectToAction("Login");

            // Lấy user hiện tại từ Database
            var user = da.tblKhachHangs.Find(formData.MaKH);
            if (user != null)
            {
                // 1. Cập nhật thông tin cơ bản
                user.TenKH = formData.TenKH;
                user.DienThoai = formData.DienThoai;
                user.DiaChi = formData.DiaChi;
                user.Email = formData.Email;

                // 2. Xử lý lưu ảnh (NẾU người dùng có chọn ảnh mới)
                if (ImageUpload != null && ImageUpload.ContentLength > 0)
                {
                    try
                    {
                        // Lấy tên file
                        string fileName = System.IO.Path.GetFileName(ImageUpload.FileName);

                        // Tạo tên file duy nhất (để tránh trùng lặp) bằng cách thêm thời gian vào trước
                        string uniqueFileName = DateTime.Now.Ticks.ToString() + "_" + fileName;

                        // Tạo đường dẫn lưu file vào thư mục ~/Content/Images/
                        string path = Server.MapPath("~/Content/Images/" + uniqueFileName);

                        // Lưu file vật lý lên Server
                        ImageUpload.SaveAs(path);

                        // Cập nhật tên file vào cột Avarta trong Database
                        user.Avarta = uniqueFileName;
                    }
                    catch (Exception)
                    {
                        // Nếu lỗi lưu ảnh thì bỏ qua hoặc ghi log, không làm crash web
                    }
                }
                // Nếu ImageUpload == null thì giữ nguyên ảnh cũ (không làm gì cả)

                // 3. Lưu thay đổi xuống Database
                da.SaveChanges();

                // 4. Cập nhật lại Session để hiển thị ngay lập tức trên giao diện
                Session["HoTen"] = user.TenKH;
                Session["User"] = user; // Quan trọng: Cập nhật lại toàn bộ object User trong session

                TempData["Message"] = "Cập nhật hồ sơ thành công!";
            }

            return RedirectToAction("HoSoCaNhan");
        }
        // UserController.cs
        // 3. Hủy đơn hàng (XÓA VĨNH VIỄ KHỎI DATABASE)
        [HttpGet]
        public ActionResult HuyDonHang(int id)
        {
            // 1. Kiểm tra đăng nhập
            if (Session["User"] == null) return RedirectToAction("Login");

            tblKhachHang kh = (tblKhachHang)Session["User"];

            // Tìm đơn hàng theo ID
            tblHoaDon hoaDon = da.tblHoaDons.SingleOrDefault(n => n.MaHD == id);

            // Kiểm tra đơn hàng có tồn tại và thuộc về khách hàng
            if (hoaDon == null || hoaDon.MaKH != kh.MaKH)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập.";
                // Chuyển hướng về Lịch sử để xem lại
                return RedirectToAction("LichSuDonHang");
            }

            // 2. Kiểm tra trạng thái đơn hàng (Chỉ xóa nếu đang là ID 1: 'Chờ xác nhận')
            // Nếu trạng thái đã là 2, 3, 4 (Đang chế biến, Đang phục vụ, Đã thanh toán) thì không cho xóa
            if (hoaDon.TinhTrang == 1)
            {
                try
                {
                    // BƯỚC 1: XÓA CÁC BẢN GHI CHI TIẾT HÓA ĐƠN LIÊN QUAN
                    var chiTietHD = da.tblChiTietHoaDons.Where(ct => ct.MaHD == id);
                    da.tblChiTietHoaDons.RemoveRange(chiTietHD);

                    // BƯỚC 2: XÓA BẢN GHI HÓA ĐƠN CHÍNH
                    da.tblHoaDons.Remove(hoaDon);

                    // LƯU THAY ĐỔI
                    da.SaveChanges();

                    TempData["Message"] = "Đơn hàng #" + id + " đã được xóa khỏi hệ thống thành công.";
                }
                catch (Exception)
                {
                    TempData["Error"] = "Lỗi database khi xóa đơn hàng. Hãy kiểm tra khóa ngoại!";
                }
            }
            // else { ... } // BỎ TẠM KHỐI ELSE NÀY ĐI

            // CHUYỂN HƯỚNG
            return RedirectToAction("LichSuDonHang");
        }
        // Hàm trả về số điểm hiện tại mà khách hàng có thể dùng
        public int TinhDiemKhaDung(int maKH)
        {
            // 1. Tổng điểm kiếm được từ hóa đơn (Công thức cũ của bạn)
            decimal tongTienDaThanhToan = da.tblHoaDons
                .Where(n => n.MaKH == maKH && n.TinhTrang == 4) // 4 = Đã thanh toán
                .Sum(n => (decimal?)n.TongTien) ?? 0;

            int diemKiemDuoc = (int)Math.Floor(tongTienDaThanhToan / 10000m) * 10;

            // 2. Tổng điểm đã tiêu xài (đổi voucher)
            int diemDaDung = da.tblLichSuDoiDiems
                .Where(n => n.MaKH == maKH)
                .Sum(n => (int?)n.DiemDaTru) ?? 0;

            // 3. Điểm còn lại
            return diemKiemDuoc - diemDaDung;
        }
        // Hiển thị danh sách các Voucher có thể đổi
        public ActionResult DoiDiemVoucher()
        {
            if (Session["User"] == null) return RedirectToAction("Login");

            var sessionUser = (tblKhachHang)Session["User"];

            // Lấy điểm hiện tại để hiển thị
            ViewBag.DiemHienTai = TinhDiemKhaDung(sessionUser.MaKH);

            // Lấy danh sách voucher còn hạn và còn số lượng
            var listVoucher = da.tblVouchers
                .Where(v => v.SoLuong > 0 && v.NgayHetHan >= DateTime.Now)
                .ToList();

            return View(listVoucher);
        }

        // Xử lý logic khi bấm nút "Đổi ngay"
        [HttpPost]
        public ActionResult ThucHienDoiVoucher(int maVoucher)
        {
            if (Session["User"] == null) return Json(new { success = false, msg = "Vui lòng đăng nhập!" });

            var sessionUser = (tblKhachHang)Session["User"];
            int maKH = sessionUser.MaKH;

            using (var transaction = da.Database.BeginTransaction())
            {
                try
                {
                    // 1. Kiểm tra Voucher tồn tại
                    var voucher = da.tblVouchers.Find(maVoucher);
                    if (voucher == null || voucher.SoLuong <= 0)
                    {
                        return Json(new { success = false, msg = "Voucher này đã hết hoặc không tồn tại!" });
                    }

                    // 2. Kiểm tra điểm người dùng có đủ không
                    int diemHienCo = TinhDiemKhaDung(maKH);
                    if (diemHienCo < voucher.DiemDoi)
                    {
                        return Json(new { success = false, msg = "Bạn không đủ điểm để đổi Voucher này!" });
                    }

                    // 3. Trừ số lượng kho Voucher
                    voucher.SoLuong--;

                    // 4. Tạo lịch sử đổi điểm (Trừ điểm)
                    tblLichSuDoiDiem ls = new tblLichSuDoiDiem();
                    ls.MaKH = maKH;
                    ls.MaVoucher = maVoucher;
                    ls.NgayDoi = DateTime.Now;
                    ls.DiemDaTru = voucher.DiemDoi; // Quan trọng: Lưu số điểm bị trừ
                    ls.TrangThai = false; // Chưa sử dụng

                    // Tạo mã code ngẫu nhiên (Ví dụ: VOUCHER_12345)
                    ls.MaCode = "VOUCHER_" + DateTime.Now.Ticks.ToString().Substring(10, 6);

                    da.tblLichSuDoiDiems.Add(ls);
                    da.SaveChanges();

                    transaction.Commit();
                    return Json(new { success = true, msg = "Đổi thành công! Kiểm tra trong Hồ sơ cá nhân." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, msg = "Lỗi hệ thống: " + ex.Message });
                }
            }
        }

        // =========================================================================
        // CHỨC NĂNG QUÊN MẬT KHẨU (Khớp với DB QL_NhaHangg)
        // =========================================================================

        // 1. Hiển thị View nhập Email (GET)
        [HttpGet]
        public ActionResult QuenMatKhau()
        {
            return View();
        }

        // 2. Xử lý logic gửi mail (POST)
        [HttpPost]
        public ActionResult QuenMatKhau(string email)
        {
            // Kiểm tra đầu vào
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Vui lòng nhập email!";
                return View();
            }

            // Tìm khách hàng trong DB theo Email
            // Lưu ý: db context ở đây tôi gọi là 'da', giống code cũ của bạn
            var kh = da.tblKhachHangs.FirstOrDefault(x => x.Email == email);

            if (kh != null)
            {
                // Tạo mật khẩu mới ngẫu nhiên (8 ký tự)
                string matKhauMoi = Guid.NewGuid().ToString().Substring(0, 8);

                // Cập nhật mật khẩu mới vào CSDL
                // Cột trong DB là: MatKhau (NVARCHAR 100) -> Khớp
                kh.MatKhau = matKhauMoi;

                // Lưu thay đổi
                da.SaveChanges();

                // Gọi hàm gửi email
                bool ketQua = GuiEmailMatKhau(kh.Email, matKhauMoi, kh.TenKH);

                if (ketQua)
                {
                    TempData["Success"] = "Mật khẩu mới đã được gửi tới " + email + ". Vui lòng kiểm tra hộp thư (cả mục Spam).";
                    return RedirectToAction("Login", "User");
                }
                else
                {
                    ViewBag.Error = "Lỗi gửi mail! Vui lòng kiểm tra lại đường truyền hoặc cấu hình.";
                }
            }
            else
            {
                ViewBag.Error = "Email này chưa được đăng ký trong hệ thống!";
            }

            return View();
        }

        // 3. Hàm phụ trợ gửi Email qua Gmail
        private bool GuiEmailMatKhau(string emailNguoiNhan, string matKhauMoi, string tenKhachHang)
        {
            try
            {
                // --- CẤU HÌNH GỬI MAIL ---
                // Bạn PHẢI dùng "Mật khẩu ứng dụng" (App Password) của Gmail, không phải mật khẩu đăng nhập thường.
                string fromEmail = "Khah06764@gmail.com"; // <--- ĐIỀN EMAIL CỦA BẠN
                string password = "deda hfpz ionj iaok"; // <--- ĐIỀN APP PASSWORD

                // Nội dung Email
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(fromEmail);
                mail.To.Add(emailNguoiNhan);
                mail.Subject = "[NHÀ HÀNG] - Cấp lại mật khẩu mới";

                string noiDung = "Chào " + tenKhachHang + ",\n\n"
                               + "Hệ thống đã nhận được yêu cầu cấp lại mật khẩu của bạn.\n"
                               + "Mật khẩu mới của bạn là: " + matKhauMoi + "\n\n"
                               + "Vui lòng đăng nhập và đổi lại mật khẩu ngay để bảo mật.\n"
                               + "Cảm ơn bạn đã sử dụng dịch vụ!";

                mail.Body = noiDung;
                mail.IsBodyHtml = false; // Gửi text thường cho đơn giản

                // Cấu hình SMTP Google
                SmtpClient smtp = new SmtpClient("smtp.gmail.com");
                smtp.Port = 587;
                smtp.EnableSsl = true;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(fromEmail, password);

                smtp.Send(mail);
                return true; // Gửi thành công
            }
            catch (Exception)
            {
                return false; // Gửi thất bại
            }
        }
    }
}