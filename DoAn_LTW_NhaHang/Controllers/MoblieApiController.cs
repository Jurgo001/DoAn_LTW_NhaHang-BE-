using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DoAn_LTW_NhaHang.Models;

namespace DoAn_LTW_NhaHang.Controllers
{
    public class MobileApiController : Controller
    {
        // 1. Khai báo biến dùng chung cho tất cả các hàm bên dưới
        QL_NhaHangEntities db = new QL_NhaHangEntities();

        // ==========================================
        // CÁC HÀM GET (LẤY DỮ LIỆU)
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

            // 1. Lấy toàn bộ bình luận của món này (TỪ DATABASE LÊN RAM)
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

            // 2. Tính toán sao
            double trungBinhSao = danhSachCmt.Any() ? danhSachCmt.Average(l => (double)(l.SoSao ?? 5)) : 5.0;

            // 3. Lấy đúng 1 món ăn từ bảng tblMonAn (TỪ DATABASE LÊN RAM)
            var monDb = db.tblMonAns.Where(m => m.MaMon == id).FirstOrDefault();

            // Nếu không tìm thấy món thì kết thúc sớm
            if (monDb == null) return Json(null, JsonRequestBehavior.AllowGet);

            // 4. Lấy thêm danh sách ảnh phụ (nếu có)
            var hinhAnhs = db.tblHinhAnhs.Where(h => h.MaMon == id).Select(h => h.TenHinh).ToList();

            // 5. Gói tất cả vào một Object mới tinh để gửi về cho Flutter
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
                BinhLuans = danhSachCmt, // Gắn mảng bình luận vào đây rất an toàn
                HinhAnhs = hinhAnhs
            };

            return Json(result, JsonRequestBehavior.AllowGet);
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
                    HinhAnhBinhLuan = hinhAnhBase64, // Nhận chuỗi ảnh từ Flutter
                    ChuDe = "Binh luan MonAn ID: " + maMon,
                    NgayGui = DateTime.Now
                };
                db.tblLienHes.Add(bl);
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch { return Json(new { success = false }); }
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

        // ==========================================
        // CÁC HÀM POST (GỬI DỮ LIỆU)
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
        public JsonResult ToggleYeuThich(int maKH, int maMon)
        {
            // Logic xử lý yêu thích ở đây
            return Json(new { success = true });
        }

    } // Kết thúc Class MobileApiController

    // ==========================================
    // CÁC CLASS MODEL (PHẢI NẰM NGOÀI CONTROLLER)
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