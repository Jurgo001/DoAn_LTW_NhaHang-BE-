using System;
using System.Collections.Generic;

namespace DoAn_LTW_NhaHang.Models
{
    // ViewModel tổng chứa dữ liệu đẩy ra View Thống kê
    public class ThongKeViewModel
    {
        public List<DoanhThuNgay> DoanhThuTheoNgay { get; set; }
        public List<ThongKeLoai> DoanhThuTheoLoai { get; set; }
        public List<MonAnBanChay> TopMonAn { get; set; }
    }

    // Class con hỗ trợ từng biểu đồ
    public class DoanhThuNgay
    {
        public DateTime Ngay { get; set; }
        public decimal DoanhThu { get; set; }
        public int SoDonHang { get; set; }
    }

    public class ThongKeLoai
    {
        public string TenLoai { get; set; }
        public int SoLuongBan { get; set; }
        public decimal TongTien { get; set; }
    }

    public class MonAnBanChay
    {
        public string TenMon { get; set; }
        public string HinhAnh { get; set; }
        public int SoLuongBan { get; set; }
        public decimal TongTien { get; set; }
    }
}