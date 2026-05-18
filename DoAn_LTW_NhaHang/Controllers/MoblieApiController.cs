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
                    k.Email,
                    k.DiaChi
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


        // ==========================================
        // CÁC HÀM POST (GỬI DỮ LIỆU TỪ APP LÊN)
        // ==========================================

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
                    TongTien = request.Items.Sum(x => x.SoLuong * x.DonGia),
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

    } 

    // ==========================================
    // CÁC CLASS MODEL DỮ LIỆU
    // ==========================================
    public class OrderRequest
    {
        public int MaKH { get; set; }
        public string GhiChu { get; set; }
        public List<CartItemRequest> Items { get; set; }
    }

    public class CartItemRequest
    {
        public int MaMon { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
    }
}