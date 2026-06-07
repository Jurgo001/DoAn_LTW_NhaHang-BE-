using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DoAn_LTW_NhaHang.Models;

namespace DoAn_LTW_NhaHang.Controllers
{
    public class MobileApiController : Controller
    {
        // 1. Đã thêm chữ 'readonly' để diệt cảnh báo vàng
        readonly QL_NhaHangEntities db = new QL_NhaHangEntities();

        // ==========================================
        // CÁC HÀM GET (LẤY DỮ LIỆU VỀ APP)
        // ==========================================

        [HttpGet]
        public JsonResult GetTatCaMonAn()
        {
            db.Configuration.ProxyCreationEnabled = false;
            var danhSach = db.tblMonAns.Where(m => m.TrangThai == true).Select(m => new {
                m.MaMon,
                m.TenMon,
                m.MoTa,
                m.DonGia,
                m.AnhDaiDien,
                m.MaLoai
            }).ToList();
            return Json(danhSach, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetLoaiMon()
        {
            db.Configuration.ProxyCreationEnabled = false;
            var danhMuc = db.tblLoaiMons.Select(l => new { l.MaLoai, l.TenLoai }).ToList();
            return Json(danhMuc, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetChiTietMon(int id)
        {
            db.Configuration.ProxyCreationEnabled = false;

            // Lấy bình luận từ tblLienHe (Đã cập nhật SoSao và HinhAnhBinhLuan)
            var danhSachCmt = db.tblLienHes
                .Where(l => l.ChuDe.Contains("ID: " + id))
                .OrderByDescending(l => l.NgayGui)
                .Select(l => new {
                    l.TenNguoiGui,
                    l.NoiDung,
                    l.SoSao,
                    l.HinhAnhBinhLuan,
                    l.NgayGui
                }).ToList();

            double trungBinhSao = danhSachCmt.Any() ? danhSachCmt.Average(l => (double)(l.SoSao ?? 5)) : 5.0;

            var monDb = db.tblMonAns.Where(m => m.MaMon == id).FirstOrDefault();
            if (monDb == null) return Json(null, JsonRequestBehavior.AllowGet);

            var hinhAnhs = db.tblHinhAnhs.Where(h => h.MaMon == id).Select(h => h.TenHinh).ToList();

            var result = new
            {
                monDb.MaMon,
                monDb.TenMon,
                monDb.MoTa,
                monDb.DonGia,
                monDb.AnhDaiDien,
                monDb.MaLoai,
                DiemTrungBinh = trungBinhSao,
                TongDanhGia = danhSachCmt.Count,
                BinhLuans = danhSachCmt,
                HinhAnhs = hinhAnhs
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetMonCungLoai(int maLoai)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var ds = db.tblMonAns.Where(m => m.MaLoai == maLoai).Take(5).Select(m => new {
                m.MaMon,
                m.TenMon,
                m.MoTa,
                m.DonGia,
                m.AnhDaiDien,
                m.MaLoai
            }).ToList();
            return Json(ds, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetThongTinKhachHang(int maKH)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var khachHang = db.tblKhachHangs
                .Where(k => k.MaKH == maKH)
                .Select(k => new {
                    k.MaKH,
                    k.TenKH,
                    k.DienThoai, 
                    k.Email
                }).FirstOrDefault();

            if (khachHang == null)
                return Json(new { success = false, message = "Không tìm thấy khách hàng" }, JsonRequestBehavior.AllowGet);

            return Json(new { success = true, data = khachHang }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetDanhSachVoucher()
        {
            db.Configuration.ProxyCreationEnabled = false;

            var vouchers = db.tblVouchers
                .Select(v => new {
                    v.MaVoucher,
                    v.TenVoucher,
                    v.GiaTri,   
                    v.DiemDoi,
                    v.SoLuong,
                    v.NgayHetHan
                }).ToList();

            return Json(vouchers, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetOrderHistory(int maKH)
        {
            try
            {
                // Tắt ProxyCreation để tránh lỗi vòng lặp khi convert sang JSON
                db.Configuration.ProxyCreationEnabled = false;

                // Lấy danh sách hóa đơn theo mã khách hàng, sắp xếp mới nhất lên đầu
                var danhSachHoaDon = db.tblHoaDons
                    .Where(hd => hd.MaKH == maKH)
                    .OrderByDescending(hd => hd.NgayLap)
                    .Select(hd => new
                    {
                        hd.MaHD,
                        hd.NgayLap,
                        hd.TongTien,
                        hd.TinhTrang,
                        hd.DaThanhToan,
                        hd.GhiChu,
                        // Lấy luôn danh sách các món ăn bên trong hóa đơn này để App dễ hiển thị
                        ChiTietMon = hd.tblChiTietHoaDons.Select(ct => new {
                            ct.MaMon,
                            TenMon = ct.tblMonAn.TenMon,
                            AnhDaiDien = ct.tblMonAn.AnhDaiDien,
                            ct.SoLuong,
                            ct.DonGia,
                            ThanhTien = ct.SoLuong * ct.DonGia
                        }).ToList()
                    }).ToList();

                // Xử lý lại định dạng ngày tháng để Mobile App (Flutter/Android/iOS) dễ đọc hơn
                // Mặc định MVC trả về ngày dạng /Date(123123123)/ rất khó parse trên App
                var result = danhSachHoaDon.Select(hd => new {
                    hd.MaHD,
                    NgayLap = hd.NgayLap.HasValue ? hd.NgayLap.Value.ToString("dd/MM/yyyy HH:mm") : "",
                    hd.TongTien,
                    hd.TinhTrang,
                    hd.DaThanhToan,
                    hd.GhiChu,
                    hd.ChiTietMon
                }).ToList();

                if (result.Count == 0)
                {
                    return Json(new { success = true, message = "Bạn chưa có đơn hàng nào.", data = result }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { success = true, data = result }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi lấy lịch sử đơn hàng: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }

        }
        [HttpGet]
        public JsonResult GetDanhSachDiaChi(int maKH)
        {
            try
            {
                db.Configuration.ProxyCreationEnabled = false;

                var dsDiaChi = db.tblDiaChiGiaoHangs
                                 .Where(d => d.MaKH == maKH)
                                 .OrderByDescending(d => d.LaMacDinh) // Đưa địa chỉ mặc định lên đầu
                                 .Select(d => new {
                                     d.MaDiaChi,
                                     d.TenNguoiNhan,
                                     d.SoDienThoai,
                                     d.DiaChiChiTiet,
                                     d.LaMacDinh
                                 }).ToList();

                return Json(new { success = true, data = dsDiaChi }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // ==========================================
        // CÁC HÀM POST (GỬI DỮ LIỆU TỪ APP LÊN)
        // ==========================================
        [HttpPost]
        public JsonResult DangNhap(LoginRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return Json(new { success = false, message = "Vui lòng nhập đầy đủ Email và Mật khẩu!" });
                }

                db.Configuration.ProxyCreationEnabled = false;

                // Lưu ý: Nếu cột mật khẩu trong Database của bạn tên khác (VD: Password, Pass), thì sửa chữ MatKhau lại nha
                var khachHang = db.tblKhachHangs
                                  .Where(k => k.Email == request.Email && k.MatKhau == request.Password)
                                  .Select(k => new {
                                      k.MaKH,
                                      k.TenKH,
                                      k.Email
                                  }).FirstOrDefault();

                if (khachHang != null)
                {
                    return Json(new { success = true, data = khachHang });
                }
                else
                {
                    return Json(new { success = false, message = "Sai tài khoản hoặc mật khẩu!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống C#: " + ex.Message });
            }
        }



        [HttpPost]
        public JsonResult DatHang(OrderRequest request)
        {
            try
            {
                if (request == null || request.Items == null || request.Items.Count == 0)
                    return Json(new { success = false, message = "Giỏ hàng trống!" });

                tblHoaDon hd = new tblHoaDon
                {
                    MaKH = request.MaKH,
                    NgayLap = DateTime.Now,
                    // 👉 SỬA DÒNG NÀY: Lấy số tiền từ Mobile gửi lên thay vì tự tính
                    TongTien = request.TongTienThanhToan,
                    TinhTrang = 1,
                    DaThanhToan = false,
                    GhiChu = request.GhiChu ?? "Đặt hàng từ Mobile App"
                };

                db.tblHoaDons.Add(hd);
                db.SaveChanges();

                foreach (var item in request.Items)
                {
                    db.tblChiTietHoaDons.Add(new tblChiTietHoaDon
                    {
                        MaHD = hd.MaHD,
                        MaMon = item.MaMon,
                        SoLuong = item.SoLuong,
                        DonGia = item.DonGia
                    });
                }
                db.SaveChanges();
                return Json(new { success = true, message = "Đặt món thành công! Mã đơn: " + hd.MaHD });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult GuiBinhLuan(int maMon, string tenKH, string noiDung, int soSao, string hinhAnhBase64)
        {
            try
            {
                tblLienHe bl = new tblLienHe
                {
                    TenNguoiGui = tenKH,
                    NoiDung = noiDung,
                    SoSao = soSao,
                    HinhAnhBinhLuan = hinhAnhBase64,
                    ChuDe = "Binh luan MonAn ID: " + maMon,
                    NgayGui = DateTime.Now
                };
                db.tblLienHes.Add(bl);
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch { return Json(new { success = false }); }
        }

        [HttpPost]
        public JsonResult GuiGhiChuGopY(int maKH, string hoTen, string sdt, string noiDungGhiChu)
        {
            try
            {
                tblLienHe lienHe = new tblLienHe
                {
                    TenNguoiGui = hoTen,
                    NoiDung = noiDungGhiChu,
                    ChuDe = "Gop y tu Khach Hang ID: " + maKH + " - SDT: " + sdt,
                    NgayGui = DateTime.Now
                };

                db.tblLienHes.Add(lienHe);
                db.SaveChanges();
                return Json(new { success = true, message = "Cảm ơn bạn đã gửi góp ý!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
       
        public JsonResult ToggleYeuThich()
        {
            
            return Json(new { success = true });
        }
        [HttpPost]
        public JsonResult DangKy(RegisterRequest request)
        {
            try
            {
                // 1. Kiểm tra dữ liệu đầu vào có bị rỗng không
                if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return Json(new { success = false, message = "Vui lòng nhập đầy đủ Email và Mật khẩu!" });
                }

                // 2. Kiểm tra xem Email này đã có ai dùng chưa
                var emailTonTai = db.tblKhachHangs.FirstOrDefault(k => k.Email == request.Email);
                if (emailTonTai != null)
                {
                    return Json(new { success = false, message = "Email này đã được đăng ký! Vui lòng dùng email khác." });
                }

                // 3. Tạo object khách hàng mới
                tblKhachHang khMoi = new tblKhachHang
                {
                    TenKH = request.TenKH ?? "Khách hàng mới", // Nếu không truyền tên thì để mặc định
                    Email = request.Email,
                    MatKhau = request.Password,
                    DienThoai = request.DienThoai,
                    Avarta = "default_user.jpg" // Set avatar mặc định giống hệt cách bạn làm bên Web
                };

                // 4. Lưu vào Database
                db.tblKhachHangs.Add(khMoi);
                db.SaveChanges();

                // 5. Trả về thành công kèm theo thông tin cơ bản (Không trả về mật khẩu)
                return Json(new
                {
                    success = true,
                    message = "Đăng ký tài khoản thành công!",
                    data = new
                    {
                        khMoi.MaKH,
                        khMoi.TenKH,
                        khMoi.Email,
                        khMoi.DienThoai,
                        khMoi.Avarta
                    }
                });
            }
            catch (Exception ex)
            {
                // Bắt lỗi hệ thống (như rớt mạng DB, sai kiểu dữ liệu...)
                return Json(new { success = false, message = "Lỗi hệ thống C#: " + ex.Message });
            }
        }
        [HttpPost]
        public JsonResult ThemDiaChiGiaoHang(AddressRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.DiaChiChiTiet))
                {
                    return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin địa chỉ!" });
                }

                // Nếu user set địa chỉ này làm mặc định, phải gỡ mặc định của các địa chỉ cũ
                if (request.LaMacDinh)
                {
                    var cacDiaChiCu = db.tblDiaChiGiaoHangs.Where(d => d.MaKH == request.MaKH && d.LaMacDinh == true).ToList();
                    foreach (var dc in cacDiaChiCu)
                    {
                        dc.LaMacDinh = false;
                    }
                }

                // Nếu khách hàng chưa có địa chỉ nào, tự động ép địa chỉ đầu tiên thành mặc định
                var kiemTraTonTai = db.tblDiaChiGiaoHangs.Any(d => d.MaKH == request.MaKH);
                if (!kiemTraTonTai)
                {
                    request.LaMacDinh = true;
                }

                tblDiaChiGiaoHang diaChiMoi = new tblDiaChiGiaoHang
                {
                    MaKH = request.MaKH,
                    TenNguoiNhan = request.TenNguoiNhan,
                    SoDienThoai = request.SoDienThoai,
                    DiaChiChiTiet = request.DiaChiChiTiet,
                    LaMacDinh = request.LaMacDinh
                };

                db.tblDiaChiGiaoHangs.Add(diaChiMoi);
                db.SaveChanges();

                return Json(new { success = true, message = "Thêm địa chỉ thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

    } 
        

    // ==========================================
    // CÁC CLASS MODEL DỮ LIỆU
    // ==========================================
    public class OrderRequest
    {
        public int MaKH { get; set; }
        public string GhiChu { get; set; }
        public decimal TongTienThanhToan { get; set; } // 👉 THÊM DÒNG NÀY
        public List<CartItemRequest> Items { get; set; }
    }

    public class CartItemRequest
    {
        public int MaMon { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
    public class RegisterRequest
    {
        public string TenKH { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string DienThoai { get; set; }
        public string DiaChi { get; set; }
    }
    public class AddressRequest
    {
        public int MaKH { get; set; }
        public string TenNguoiNhan { get; set; }
        public string SoDienThoai { get; set; }
        public string DiaChiChiTiet { get; set; }
        public bool LaMacDinh { get; set; }
    }
}